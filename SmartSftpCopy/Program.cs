using System.CommandLine;
using System.Net;
using CSharpFunctionalExtensions;
using Serilog;
using SftpFileSystem;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .CreateLogger();

var source = new Argument<string>("source", () => ".");
var destination = new Argument<string>("destination");
var host = new Option<string>("--host", "Hostname") {IsRequired = true};
var port = new Option<int>("--port", () => 22, "Port");
var username = new Option<string>("--username") {IsRequired = true};
var password = new Option<string>("--password") {IsRequired = true};

var rootCommand = new RootCommand
{
    source,
    destination,
    host,
    port,
    username,
    password
};

rootCommand.SetHandler<string, string, string, int, string, string>(async (s, d, h, po, us, p) =>
{
    var endpoint = new DnsEndPoint(h, po);
    var credentials = new Credentials(us, p);
    var sftp = new Sftp(endpoint, credentials);
    var result = await sftp.CopyDirectory(s, d);
    result.Match(() => Console.WriteLine("Copy successful"), s => Console.WriteLine($"Copy failed. Reason: {s}"));
}, source, destination, host, port, username, password);

await rootCommand.InvokeAsync(args);