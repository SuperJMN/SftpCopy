using System.CommandLine;
using CSharpFunctionalExtensions;

namespace Core;

public class Shell
{
    public async Task Execute(string[] args)
    {
        await CreateRootCommand().InvokeAsync(args);
    }

    private static Command CreateRootCommand()
    {
        var sourceArg = new Argument<string>("source", () => ".");
        var destinationArg = new Argument<string>("destination");
        var hostOption = new Option<string>("--host", "Hostname") {IsRequired = true};
        var portOption = new Option<int>("--port", () => 22, "Port");
        var usernameOption = new Option<string>("--username") {IsRequired = true};
        var optionalUsernameOption = new Option<string>("--username");
        var passwordOption = new Option<string>("--password") {IsRequired = true};

        var loginCommand = new Command("login")
        {
            hostOption,
            passwordOption,
            usernameOption
        };

        var copyCommand = new Command("copy")
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
                await Login(new Login(qualifiedUser, password));
            }, hostOption,
            usernameOption, passwordOption);

        copyCommand.SetHandler<string, string, string, int, string>(
            async (source, destination, host, port, usernameString) =>
            {
                await Copy(usernameString, destination, source, port, host);
            }, sourceArg, destinationArg, hostOption, portOption, usernameOption);


        var rootCommand = new RootCommand
        {
            loginCommand,
            copyCommand
        };

        return rootCommand;
    }

    private static async Task Copy(string usernameString, string destination, string source, int port, string host)
    {
        var app = CreateApp();
        var username = Maybe.From(usernameString);

        await Result.Success()
            .Bind(() => username.Match(
                user => app.Copy(destination, source, port, host, user),
                () => app.Copy(destination, source, port, host)))
            .Match(() => Console.WriteLine("Copy successful"),
                s => Console.WriteLine($"Copy failed. Reason: {s}"));
    }

    private static SftpApplication CreateApp()
    {
        return new SftpApplication(new LoginStore(new System.IO.Abstractions.FileSystem()));
    }

    private static async Task Login(Login login)
    {
        var app = new SftpApplication(new LoginStore(new System.IO.Abstractions.FileSystem()));
        await Result.Success()
            .Bind(() => app.AddOrReplaceLogin(login))
            .Match(() => Console.WriteLine("Credentials added/updated successfully"),
                s => Console.WriteLine($"Could not add/update credentials. Reason: {s}"));
    }
}