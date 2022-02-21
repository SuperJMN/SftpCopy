using System.IO.Abstractions;

public static class FileMixin
{
    public static async Task<byte[]> ReadAllBytes(this IFileInfo file)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var sourceStream = file.OpenRead())
            {
                await sourceStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}