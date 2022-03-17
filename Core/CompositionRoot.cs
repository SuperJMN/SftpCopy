using System.Reflection;
using Autofac;
using CSharpFunctionalExtensions;
using FileSystem;
using Serilog;

namespace Core;

public static class CompositionRoot
{
    public static IContainer CreateContainer(Maybe<RemoteLoginOptions> loginOptions)
    {
        var cb = new ContainerBuilder();
        loginOptions.Execute(options => cb.RegisterInstance(options).AsSelf().SingleInstance());
        cb.RegisterInstance(Log.Logger).As<ILogger>();
        cb.RegisterType<CopyCommand>().AsSelf();
        cb.RegisterType<Syncer>().As<ISyncer>();
        cb.RegisterType<ZafiroFileSystemComparer>().As<IZafiroFileSystemComparer>();
        cb.Register(context => CreateLoginStore(context.Resolve<IZafiroDirectory>()));
        cb.Register(context =>
        {
            var loginStore = context.Resolve<LoginStore>();
            var remoteLoginOptions = context.Resolve<RemoteLoginOptions>();
            var zafiroDirectory = context.Resolve<IZafiroDirectory>();
            var logger = context.Resolve<ILogger>();

            return new FileSystemRoot(new Dictionary<string, Func<Task<Result<IFileSystemSession>>>>
            {
                ["local"] = () => GetLocalSession(zafiroDirectory, remoteLoginOptions.EndPoint.Host, logger),
                ["remote"] = () => GetRemoteSession(loginStore, remoteLoginOptions, logger)
            });
        }).As<IFileSystemRoot>();

        cb.Register(_ => GetAppFolder()).AsSelf().SingleInstance();

        return cb.Build();
    }

    private static Task<Result<IFileSystemSession>> GetLocalSession(IZafiroDirectory directory, string host,
        ILogger logger)
    {
        var zafiroFile = directory.GetFile("hashes.dat").Value;
        return Task.FromResult(Result.Success<IFileSystemSession>(new LocalFileSystemSession(host, zafiroFile, Maybe<ILogger>.From(logger))));
    }

    private static async Task<Result<IFileSystemSession>> GetRemoteSession(LoginStore loginStore, RemoteLoginOptions remoteLoginOptions, ILogger logger)
    {
        var dnsEndPoint = remoteLoginOptions.EndPoint;
        var host = dnsEndPoint.Host;
        var credentials = await remoteLoginOptions.Username
            .Match(
                username => loginStore.GetCredentials(host, username),
                () => loginStore.GetCredentials(host));

        return await credentials
            .Bind(cred =>
            {
                var creds = new SftpFileSystem.Credentials(cred.Username, cred.Password);
                return RemoteFileSystemSession.Create(dnsEndPoint, creds, Maybe<ILogger>.From(logger));
            });
    }

    private static LoginStore CreateLoginStore(IZafiroDirectory zafiroDirectory)
    {
        var loginStore = new LoginStore(zafiroDirectory.GetFile("logins.dat").Value);
        return loginStore;
    }

    private static IZafiroDirectory GetAppFolder()
    {
        var zafiroFileSystem = new ZafiroFileSystem(new System.IO.Abstractions.FileSystem(), Maybe<ILogger>.None);
        var fs = new System.IO.Abstractions.FileSystem();
        var nativePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
        var appFolderPath = fs.MakeZafiroPathFrom(nativePath!);
        return zafiroFileSystem.GetDirectory(appFolderPath).Value;
    }
}