using System;
using System.IO;
using System.IO.Abstractions;
using System.Net;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using CSharpFunctionalExtensions;
using FileSystem;
using SftpFileSystem;
using Xunit;

namespace Core.Tests;

public class CompositionTests
{
    [Fact]
    public async Task Test()
    {
        //var dnsEndPoint = new DnsEndPoint("localhost", 22);
        //var copyOptions = new CopyOptions("E:/Repos/SuperJMN/Deployer/Docs", "upload", new LoginOptions());

        //var cb = new ContainerBuilder();

        //cb.RegisterType<CopyCommand>().AsSelf();
        //cb.RegisterInstance(copyOptions).AsSelf().SingleInstance();
        //cb.Register(context => ExportLocalDir(context.Resolve<CopyOptions>())).Named<Result<IZafiroDirectory>>("local");
        //cb.Register(context => ExportRemoteDir(context.Resolve<Result<IZafiroFileSystem>>(), context.Resolve<CopyOptions>())).Named<Result<IZafiroDirectory>>("remote");
        //cb.Register(context => ExportFileSystem(context.Resolve<CopyOptions>()))
        //    .As<Result<IFileSystem>>()
        //    .OnRelease(DisposeValue);
        //cb.Register(context =>
        //{
        //    var fs = context.Resolve<Result<IFileSystem>>();
        //    return fs.Map(r => (IZafiroFileSystem)new ZafiroFileSystem(r));
        //}).As<Result<IZafiroFileSystem>>();
        //cb.RegisterType<Copier>().As<ICopier>();
        //cb.RegisterType<ZafiroFileSystemComparer>().As<IZafiroFileSystemComparer>();

        //var c = cb.Build();

        //await using var scope = c.BeginLifetimeScope();

        //var parameters = new Parameter[]
        //{
        //    ResolvedParameter<Result<IZafiroDirectory>>("source", "local"),
        //    ResolvedParameter<Result<IZafiroDirectory>>("destination", "remote"),
        //};

        //var n = scope.Resolve<CopyCommand>(parameters);
        //var result = await n.Execute();
    }

    private static void DisposeValue(Result<IFileSystem> r)
    {
        r.OnSuccessTry(obj =>
        {
            if (obj is IDisposable d)
            {
                d.Dispose();
            }
        });
    }

    private static ResolvedParameter ResolvedParameter<T>(string paramName, string serviceName)
    {
        return new ResolvedParameter(
            (info, _) => info.ParameterType == typeof(T) && info.Name == paramName,
            (_, context) => context.ResolveNamed<T>(serviceName));
    }

    private static Result<IFileSystem> ExportFileSystem(CopyOptions copyOptions)
    {
        var host = copyOptions.RemoteOptions.EndPoint.Host;
        var port = copyOptions.RemoteOptions.EndPoint.Port;
        var username = copyOptions.RemoteOptions.MachineCredentials.Username;
        var password = copyOptions.RemoteOptions.MachineCredentials.Password;

        var fs = SftpFileSystem.FileSystem.Connect(host, port, new Credentials(username, password)).Map(f => (IFileSystem)f);
        return fs;
    }

    private static Result<IZafiroDirectory> ExportLocalDir(CopyOptions copyOptions)
    {
        return new ZafiroFileSystem(new System.IO.Abstractions.FileSystem()).GetDirectory(copyOptions.Source);
    }

    private static Result<IZafiroDirectory> ExportRemoteDir(Result<IZafiroFileSystem> remoteFileSystem, CopyOptions copyOptions)
    {
        return remoteFileSystem.Bind(system => system.GetDirectory(copyOptions.Destination));
    }
}