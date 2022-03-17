using System.CommandLine;
using System.Net;
using Autofac;
using Autofac.Core;
using CSharpFunctionalExtensions;
using FileSystem;

namespace Core;

public class Shell
{
    public static async Task Execute(string[] args)
    {
        var rootCommand = CreateRootCommand();
        await rootCommand.InvokeAsync(args);
    }

    private static Command CreateRootCommand()
    {
        var sourceArg = new Argument<string>("source", () => ".");
        var destinationArg = new Argument<string>("destination");
        var hostOption = new Option<string>("--host", "Hostname") { IsRequired = true };
        var portOption = new Option<int>("--port", () => 22, "Port");
        var usernameOption = new Option<string>("--username") { IsRequired = true };
        var optionalUsernameOption = new Option<string>("--username");
        var passwordOption = new Option<string>("--password") { IsRequired = true };

        var loginCommand = new Command("login")
        {
            hostOption,
            passwordOption,
            usernameOption
        };

        var copyCommand = new Command("sync")
        {
            sourceArg,
            destinationArg,
            hostOption,
            portOption,
            optionalUsernameOption
        };

        loginCommand.SetHandler(
            async (string host, string username, string password) =>
            {
                var qualifiedUser = new MachineUser(new Host(host), new Username(username));
                var container = CompositionRoot.CreateContainer(new RemoteLoginOptions(Maybe<Username>.From(""), null));

                using (var scope = container.BeginLifetimeScope())
                {
                    var loginStore = scope.Resolve<LoginStore>();
                    var result = await loginStore.AddOrReplace(new Login(qualifiedUser, password));
                    result.Match(() => Console.WriteLine("Success"), e => Console.Error.WriteLine(e));
                }
            }, hostOption,
            usernameOption, passwordOption);

        copyCommand.SetHandler<string, string, string, int, string>(
            async (source, destination, host, port, usernameString) =>
            {
                var dnsEndPoint = new DnsEndPoint(host, port);
                var username = Maybe<string>.From(usernameString).Map(s => new Username(s));
                var container = CompositionRoot.CreateContainer(new RemoteLoginOptions(username, dnsEndPoint));

                var parameters = new Parameter[]
                {
                    new NamedParameter("source", source),
                    new NamedParameter("destination", destination),
                };

                using (var scope = container.BeginLifetimeScope())
                {
                    var command = scope.Resolve<CopyCommand>(parameters);
                    var result = await command.Execute();
                    result.Match(() => Console.WriteLine("Success"), e => Console.Error.WriteLine(e));
                }
            }, sourceArg, destinationArg, hostOption, portOption, usernameOption);


        var rootCommand = new RootCommand
        {
            loginCommand,
            copyCommand
        };

        return rootCommand;
    }
}