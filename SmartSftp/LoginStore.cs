using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using Newtonsoft.Json;

namespace Cli;

public class LoginStore
{
    private readonly IFileSystem fileSystem;

    public LoginStore(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public Task<Result> AddOrReplace(string host, string username, string password)
    {
        return Result.Success()
            .Bind(GetLogins)
            .Tap(logins => logins[new QualifiedUser(host, username)] = password)
            .Bind(SaveLogins);
    }

    public Task<Result<Credentials>> GetPassword(string host, string username)
    {
        return Result.Success()
            .Bind(GetLogins)
            .Bind(logins =>
            {
                var tryFind = logins.TryFind(new QualifiedUser(host, username));
                return tryFind.Select(pwd => new Credentials(username, pwd))
                    .ToResult($"Cannot find login for host: {host}, username {username}");
            });
    }

    public Task<Result<Credentials>> GetPassword(string host)
    {
        return Result.Success()
            .Bind(GetLogins)
            .Bind(logins => logins.TryFirst(r => r.Key.Host.Equals(new IgnoreCaseString(host)))
                .Select(r => new Credentials(r.Key.Username.Value, r.Value))
                .ToResult($"Cannot find any login for the host {host}"));
    }

    private static IEnumerable<Login> ToLogins(Dictionary<QualifiedUser, string> logins)
    {
        return logins.Select(r =>
            new Login {Host = r.Key.Host, Username = r.Key.Username, Password = r.Value});
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

    private Task<Result> SaveLogins(Dictionary<QualifiedUser, string> loginDictionary)
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

    private Task<Result<Dictionary<QualifiedUser, string>>> GetLogins()
    {
        return Result.Success()
            .Bind(() => LoadFromFile().OnFailureCompensate(() => Result.Success(new Collection<Login>())))
            .Map(r => r.ToDictionary(x => new QualifiedUser(x.Host, x.Username), x => x.Password));
    }

    private Task<Result<Collection<Login>>> LoadFromFile()
    {
        return Result
            .Success(fileSystem.FileInfo.FromFileName("logins.dat"))
            .Ensure(f => f.Exists, "The file doesn't exist")
            .Map(f => f.ReadAllBytes())
            .OnSuccessTry(FromBytes)
            .Map(s => JsonConvert.DeserializeObject<Collection<Login>>(s))
            .Ensure(s => s != null, "Cannot deserialize from string");
    }

    private class Login
    {
        public IgnoreCaseString Host { get; set; }
        public IgnoreCaseString Username { get; set; }
        public string Password { get; set; }
    }

    public class QualifiedUser
    {
        public QualifiedUser(IgnoreCaseString Host, IgnoreCaseString Username)
        {
            this.Host = Host;
            this.Username = Username;
        }

        public IgnoreCaseString Host { get; init; }
        public IgnoreCaseString Username { get; init; }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((QualifiedUser) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Host, Username);
        }

        protected bool Equals(QualifiedUser other)
        {
            return Host.Equals(other.Host) && Username.Equals(other.Username);
        }
    }
}

public record Credentials(string Username, string Password);