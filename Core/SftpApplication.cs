using System.Net;
using CSharpFunctionalExtensions;
using SftpFileSystem;

namespace Core;

internal class SftpApplication
{
    private readonly LoginStore loginStore;

    public SftpApplication(LoginStore loginStore)
    {
        this.loginStore = loginStore;
    }

    public Task<Result> AddOrReplaceLogin(Login login)
    {
        return loginStore.AddOrReplace(login);
    }

    public Task<Result> Copy(string destination, string source, int port, string host)
    {
        return loginStore.GetPassword(new Host(host))
            .Bind(credentials => Copy(host, port, credentials, source, destination));
    }

    public Task<Result> Copy(string destination, string source, int port, string host, string username)
    {
        return loginStore.GetPassword(new Host(host), new Username(username))
            .Bind(credentials => Copy(host, port, credentials, source, destination));
    }

    private static async Task<Result> Copy(string host, int port, MachineCredentials machineCredentials, string source,
        string destination)
    {
        var endpoint = new DnsEndPoint(host, port);
        var sftpCredentials = new Credentials(machineCredentials.Username.Value, machineCredentials.Password);
        var sftp = new Sftp(endpoint, sftpCredentials);
        return await sftp.CopyDirectory(source, destination);
    }
}