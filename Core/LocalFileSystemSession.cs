﻿using CSharpFunctionalExtensions;
using FileSystem;
using FileSystem.Smart;
using Serilog;

namespace Core;

public class LocalFileSystemSession : IFileSystemSession
{
    private readonly HashSet<CopyOperationMetadata> hashes;
    private readonly IZafiroFile storage;
    private readonly IZafiroFileSystem inner;

    public LocalFileSystemSession(string host, IZafiroFile storage, Maybe<ILogger> logger)
    {
        this.storage = storage;
        hashes = Result.Try(() => Persistor.LoadHashes(storage)).Match(x => x, s => new HashSet<CopyOperationMetadata>());
        var fileSystem = new System.IO.Abstractions.FileSystem();
        var zafiroFileSystem = new ZafiroFileSystem(fileSystem, logger);
        inner = new SmartZafiroFileSystem(zafiroFileSystem, host, hashes);
    }

    public void Dispose()
    {
        Result.Try(() => Persistor.SaveHashes(storage, hashes));
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
}