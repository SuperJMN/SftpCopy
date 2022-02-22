using System.Xml;
using ExtendedXmlSerializer;
using ExtendedXmlSerializer.Configuration;
using FileSystem;
using FileSystem.Smart;

namespace Core;

static class Persistor
{
    private static readonly IExtendedXmlSerializer ExtendedXmlSerializer = new ConfigurationContainer()
        .UseOptimizedNamespaces()
        .Create();

    public static HashSet<CopyOperationMetadata> LoadHashes(IZafiroFile hashesFile)
    {
        using var stream = hashesFile.OpenRead();
        var records = ExtendedXmlSerializer.Deserialize<List<FileSystemRecord>>(stream);

        var query = from r in records
            from n in r.Entries
            select new CopyOperationMetadata(new Host(r.Host), new ZafiroPath(n.Source), new ZafiroPath(n.Destination),
                new Hash(n.Hash));

        return query.ToHashSet();
    }

    public static void SaveHashes(IZafiroFile zafiroFile, IEnumerable<CopyOperationMetadata> hashes)
    {
        var records = from h in hashes
            group h by h.Host
            into g
            select new FileSystemRecord
            {
                Host = g.Key,
                Entries = g.Select(r => new Entry
                {
                    Hash = r.Hash.Bytes,
                    Destination = r.Destination,
                    Source = r.Source,
                }).ToList()
            };

        using var stream = zafiroFile.OpenWrite();
        using var xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true });
        ExtendedXmlSerializer.Serialize(xmlWriter, records.ToList());
    }

    private class FileSystemRecord
    {
        public string Host { get; set; }
        public List<Entry> Entries { get; set; }
    }

    private class Entry
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public byte[] Hash { get; set; }
    }
}