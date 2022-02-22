using System.Net;
using CSharpFunctionalExtensions;
using FileSystem;
using SftpFileSystem;

namespace Core;

class RemoteFileSystemSession : IFileSystemSession
{
    private readonly SftpFileSystem.FileSystem fs;
    private readonly IZafiroFileSystem inner;

    private RemoteFileSystemSession(SftpFileSystem.FileSystem fs)
    {
        this.fs = fs;
        inner = new ZafiroFileSystem(fs);
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
    
    public static Task<Result<IFileSystemSession>> Create(DnsEndPoint endPoint, Credentials creds)
    {
        var host = endPoint.Host;
        var port = endPoint.Port;

        var result = SftpFileSystem.FileSystem
            .Connect(host, port, creds)
            .Map(fs => (IFileSystemSession) new RemoteFileSystemSession(fs));

        return Task.FromResult(result);
    }
}