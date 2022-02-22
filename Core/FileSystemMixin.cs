using System.IO.Abstractions;
using FileSystem;

namespace Core;

public static class FileSystemMixin
{
    public static ZafiroPath MakeZafiroPathFrom(this IFileSystem self, string path)
    {
        return new ZafiroPath(path.Replace(self.Path.DirectorySeparatorChar, ZafiroPath.ChuckSeparator));
    }
}