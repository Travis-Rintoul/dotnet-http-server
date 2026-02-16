namespace HttpServer.Core.Transport;

public readonly record struct Port
{
    public int Value { get; }
    
    private Port(int value) => Value = value;

    public static Port Create(int value)
    {
        if (value is < 0 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(value));

        return new Port(value);
    }
}