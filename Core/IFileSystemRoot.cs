using CSharpFunctionalExtensions;

namespace Core;

public interface IFileSystemRoot
{
    public Func<Task<Result<IFileSystemSession>>> GetFileSystem(string identifier);
}