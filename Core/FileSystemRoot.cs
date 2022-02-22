using CSharpFunctionalExtensions;

namespace Core;

class FileSystemRoot : IFileSystemRoot
{
    private readonly IDictionary<string, Func<Task<Result<IFileSystemSession>>>> getSession;

    public FileSystemRoot(IDictionary<string, Func<Task<Result<IFileSystemSession>>>> getSession)
    {
        this.getSession = getSession;
    }

    public Func<Task<Result<IFileSystemSession>>> GetFileSystem(string identifier)
    {
        return getSession[identifier];
    }
}