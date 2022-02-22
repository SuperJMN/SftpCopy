using System.Reflection;
using Autofac;
using CSharpFunctionalExtensions;
using FileSystem;

namespace Core;

public static class CompositionRoot
{
    public static IContainer CreateContainer(Maybe<RemoteLoginOptions> loginOptions)
    {
        var cb = new ContainerBuilder();
        loginOptions.Execute(options => cb.RegisterInstance(options).AsSelf().SingleInstance());
        cb.RegisterType<CopyCommand>().AsSelf();
        cb.RegisterType<Copier>().As<ICopier>();
        cb.RegisterType<ZafiroFileSystemComparer>().As<IZafiroFileSystemComparer>();
        cb.Register(context => CreateLoginStore(context.Resolve<IZafiroDirectory>()));
        cb.Register(context =>
        {
            var loginStore = context.Resolve<LoginStore>();
            var remoteLoginOptions = context.Resolve<RemoteLoginOptions>();
            var zafiroDirectory = context.Resolve<IZafiroDirectory>();

            return new FileSystemRoot(new Dictionary<string, Func<Task<Result<IFileSystemSession>>>>
            {
                ["local"] = () => GetLocalSession(zafiroDirectory, remoteLoginOptions.EndPoint.Host),
                ["remote"] = () => GetRemoteSession(loginStore, remoteLoginOptions)
            });
        }).As<IFileSystemRoot>();

        cb.Register(_ => GetAppFolder()).AsSelf().SingleInstance();

        return cb.Build();
    }

    private static Task<Result<IFileSystemSession>> GetLocalSession(IZafiroDirectory directory, string host)
    {
        var zafiroFile = directory.GetFile("hashes.dat").Value;
        return Task.FromResult(Result.Success<IFileSystemSession>(new LocalFileSystemSession(host, zafiroFile)));
    }

    private static async Task<Result<IFileSystemSession>> GetRemoteSession(LoginStore loginStore, RemoteLoginOptions remoteLoginOptions)
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
                return RemoteFileSystemSession.Create(dnsEndPoint, creds);
            });
    }

    private static LoginStore CreateLoginStore(IZafiroDirectory zafiroDirectory)
    {
        var loginStore = new LoginStore(zafiroDirectory.GetFile("logins.dat").Value);
        return loginStore;
    }

    private static IZafiroDirectory GetAppFolder()
    {
        var zafiroFileSystem = new ZafiroFileSystem(new System.IO.Abstractions.FileSystem());
        var fs = new System.IO.Abstractions.FileSystem();
        var nativePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
        var appFolderPath = fs.MakeZafiroPathFrom(nativePath!);
        return zafiroFileSystem.GetDirectory(appFolderPath).Value;
    }
}