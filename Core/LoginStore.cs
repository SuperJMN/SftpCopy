using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using Newtonsoft.Json;
using CliLogin = Core.Login;

namespace Core;

internal class LoginStore
{
    private readonly IFileSystem fileSystem;

    public LoginStore(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public Task<Result> AddOrReplace(CliLogin login)
    {
        return Result.Success()
            .Bind(GetLogins)
            .Tap(logins => logins[login.User] = login.Password)
            .Bind(SaveLogins);
    }

    public Task<Result<MachineCredentials>> GetPassword(Host host, Username username)
    {
        return Result.Success()
            .Bind(GetLogins)
            .Bind(logins =>
            {
                var tryFind = logins.TryFind(new MachineUser(host, username));
                return tryFind.Select(pwd => new MachineCredentials(username, pwd))
                    .ToResult($"Cannot find login for host: {host}, username {username}");
            });
    }

    public Task<Result<MachineCredentials>> GetPassword(Host host)
    {
        return Result.Success()
            .Bind(GetLogins)
            .Bind(logins => logins.TryFirst(r => r.Key.Host.Equals(host))
                .Select(r => new MachineCredentials(r.Key.Username, r.Value))
                .ToResult($"Cannot find any login for the host {host}"));
    }

    private static IEnumerable<Login> ToLogins(Dictionary<MachineUser, string> logins)
    {
        return logins.Select(r =>
            new Login {Host = r.Key.Host.Value, Username = r.Key.Username.Value, Password = r.Value});
    }

    private static byte[] ToBytes(string serialized)
    {
        var rawBytes = Encoding.UTF8.GetBytes(serialized);
        var protectedBytes = Protect(rawBytes);
        return protectedBytes;
    }

    private static byte[] Protect(byte[] rawBytes)
    {
        return ProtectedData.Protect(rawBytes, null, DataProtectionScope.CurrentUser);
    }

    private static string FromBytes(byte[] bytes)
    {
        var unprotect = Unprotect(bytes);
        return Encoding.UTF8.GetString(unprotect);
    }

    private static byte[] Unprotect(byte[] bytes)
    {
        return ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
    }

    private Task<Result> SaveLogins(Dictionary<MachineUser, string> loginDictionary)
    {
        return Result.Try(() =>
            {
                var logins = ToLogins(loginDictionary);
                var serialized = JsonConvert.SerializeObject(logins);
                var bytes = ToBytes(serialized);
                return bytes;
            })
            .Bind(WriteBytes);
    }

    private Task<Result> WriteBytes(byte[] bytes)
    {
        return Result.Try(async () =>
        {
            var fi = fileSystem.FileInfo.FromFileName("logins.dat");
            await using var stream = fi.OpenWrite();
            await stream.WriteAsync(bytes);
        });
    }

    private Task<Result<Dictionary<MachineUser, string>>> GetLogins()
    {
        return Result.Success()
            .Bind(() => LoadFromFile().OnFailureCompensate(() => Result.Success(new Collection<Login>())))
            .Map(r => r.ToDictionary(x => new MachineUser(new Host(x.Host), new Username(x.Username)),
                x => x.Password));
    }

    private Task<Result<Collection<Login>>> LoadFromFile()
    {
        return Result
            .Success(fileSystem.FileInfo.FromFileName("logins.dat"))
            .Ensure(f => f.Exists, "The file doesn't exist")
            .Map(f => f.ReadAllBytes())
            .OnSuccessTry(FromBytes)
            .OnSuccessTry(s => JsonConvert.DeserializeObject<Collection<Login>>(s))
            .Ensure(s => s != null, "Cannot deserialize from string");
    }

    private class Login
    {
        public string? Host { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}