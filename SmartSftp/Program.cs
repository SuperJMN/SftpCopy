using System.CommandLine;
using Cli;
using CSharpFunctionalExtensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .CreateLogger();

var sourceArg = new Argument<string>("source", () => ".");
var destinationArg = new Argument<string>("destination");
var hostOption = new Option<string>("--host", "Hostname") {IsRequired = true};
var portOption = new Option<int>("--port", () => 22, "Port");
var usernameOption = new Option<string>("--username");
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
    usernameOption
};

var rootCommand = new RootCommand
{
    loginCommand,
    copyCommand
};

loginCommand.SetHandler(async (string host, string username, string password) =>
{
    var client = new Client(new LoginStore(new System.IO.Abstractions.FileSystem()));
    await Result.Success()
        .Bind(() => client.AddOrReplaceLogin(host, username, password))
        .Match(() => Console.WriteLine("Credentials added/updated successfully"),
            s => Console.WriteLine($"Could not add/update credentials. Reason: {s}"));
}, hostOption, usernameOption, passwordOption);

copyCommand.SetHandler<string, string, string, int, string>(async (source, destination, host, port, usernameString) =>
{
    var client = new Client(new LoginStore(new System.IO.Abstractions.FileSystem()));
    var username = Maybe.From(usernameString);

    await Result.Success()
        .Bind(() => username.Match(
            user => client.Copy(destination, source, port, host, user),
            () => client.Copy(destination, source, port, host)))
        .Match(() => Console.WriteLine("Copy successful"), s => Console.WriteLine($"Copy failed. Reason: {s}"));
}, sourceArg, destinationArg, hostOption, portOption, usernameOption);

await rootCommand.InvokeAsync(args);