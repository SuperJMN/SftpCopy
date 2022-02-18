using System.Net;
using Cli;
using CSharpFunctionalExtensions;
using SftpFileSystem;
using Credentials = Cli.Credentials;

public class Client
{
    private readonly LoginStore loginStore;

    public Client(LoginStore loginStore)
    {
        this.loginStore = loginStore;
    }

    public Task<Result> AddOrReplaceLogin(string host, string username, string password)
    {
        return loginStore.AddOrReplace(host, username, password);
    }

    public Task<Result> Copy(string destination, string source, int port, string host)
    {
        return loginStore.GetPassword(host)
            .Bind(credentials => Copy(host, port, credentials, source, destination));
    }

    public Task<Result> Copy(string destination, string source, int port, string host, string username)
    {
        return loginStore.GetPassword(host, username)
            .Bind(credentials => Copy(host, port, credentials, source, destination));
    }

    private static async Task<Result> Copy(string host, int port, Credentials credentials, string source,
        string destination)
    {
        var endpoint = new DnsEndPoint(host, port);
        var sftpCredentials = new SftpFileSystem.Credentials(credentials.Username, credentials.Password);
        var sftp = new Sftp(endpoint, sftpCredentials);
        return await sftp.CopyDirectory(source, destination);
    }
}