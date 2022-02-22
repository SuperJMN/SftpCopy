using System.Net;
using CSharpFunctionalExtensions;
using FileSystem;

namespace Core;

public class RemoteLoginOptions
{
    public RemoteLoginOptions(Maybe<Username> username, DnsEndPoint endPoint)
    {
        Username = username;
        EndPoint = endPoint;
    }

    public Maybe<Username> Username { get; }
    public DnsEndPoint EndPoint { get; }
}