using System.Net;
using CSharpFunctionalExtensions;
using FileSystem;
using Serilog;
using SftpFileSystem;

namespace Core;

class RemoteFileSystemSession : IFileSystemSession
{
    private readonly SftpFileSystem.FileSystem fs;
    private readonly IZafiroFileSystem inner;

    private RemoteFileSystemSession(SftpFileSystem.FileSystem fs, Maybe<ILogger> logger)
    {
        this.fs = fs;
        inner = new ZafiroFileSystem(fs, logger);
    }

    public void Dispose()
    {
        fs.Dispose();
    }

    public Result<IZafiroFile> GetFile(ZafiroPath path)
    {
        return inner.GetFile(path);
    }

    public Result<IZafiroDirectory> GetDirectory(ZafiroPath path)
    {
        return inner.GetDirectory(path);
    }

    public Maybe<ILogger> Logger => inner.Logger;

    public static Task<Result<IFileSystemSession>> Create(DnsEndPoint endPoint, Credentials creds, Maybe<ILogger> logger)
    {
        var host = endPoint.Host;
        var port = endPoint.Port;

        var result = SftpFileSystem.FileSystem
            .Connect(host, port, creds)
            .Map(fs => (IFileSystemSession) new RemoteFileSystemSession(fs, logger));

        return Task.FromResult(result);
    }
}