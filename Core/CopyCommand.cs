using CSharpFunctionalExtensions;
using FileSystem;

namespace Core;

public class CopyCommand
{
    private readonly string source;
    private readonly string destination;
    private readonly IFileSystemRoot fileSystemRoot;
    private readonly ICopier copier;

    public CopyCommand(string source, string destination, IFileSystemRoot fileSystemRoot, ICopier copier)
    {
        this.source = source;
        this.destination = destination;
        this.fileSystemRoot = fileSystemRoot;
        this.copier = copier;
    }

    public async Task<Result> Execute()
    {
        var originSession = await fileSystemRoot.GetFileSystem("local")();
        var destinationSession = await fileSystemRoot.GetFileSystem("remote")();

        return await originSession.Using(ss => destinationSession.Using(async ds =>
        {
            var origin = ss.GetDirectory(source);
            var result = ds.GetDirectory(destination);
            return await copier.Copy(origin, result);
        }));
    }
}