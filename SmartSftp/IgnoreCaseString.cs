namespace Cli;

public class IgnoreCaseString
{
    public IgnoreCaseString(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static implicit operator IgnoreCaseString(string str)
    {
        return new IgnoreCaseString(str);
    }

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

        return Equals((IgnoreCaseString) obj);
    }

    public override int GetHashCode()
    {
        return Value.ToLowerInvariant().GetHashCode();
    }

    protected bool Equals(IgnoreCaseString other)
    {
        return string.Equals(Value, other.Value, StringComparison.InvariantCultureIgnoreCase);
    }
}