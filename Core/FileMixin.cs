using System.IO.Abstractions;

namespace Core;

public static class FileMixin
{
    public static async Task<byte[]> ReadAllBytes(this IFileInfo file)
    {
        await using var memoryStream = new MemoryStream();
        await using var sourceStream = file.OpenRead();
        await sourceStream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}