using CSharpFunctionalExtensions;
using FileSystem;

namespace Core;

public class CopyCommand
{
    private readonly string source;
    private readonly string destination;
    private readonly IFileSystemRoot fileSystemRoot;
    private readonly ISyncer syncer;

    public CopyCommand(string source, string destination, IFileSystemRoot fileSystemRoot, ISyncer syncer)
    {
        this.source = source;
        this.destination = destination;
        this.fileSystemRoot = fileSystemRoot;
        this.syncer = syncer;
    }

    public async Task<Result> Execute()
    {
        var originSession = await fileSystemRoot.GetFileSystem("local")();
        var destinationSession = await fileSystemRoot.GetFileSystem("remote")();

        return await originSession.Using(ss => destinationSession.Using(async ds =>
        {
            var origin = ss.GetDirectory(source);
            var result = ds.GetDirectory(destination);
            return await syncer.Copy(origin, result);
        }));
    }
}