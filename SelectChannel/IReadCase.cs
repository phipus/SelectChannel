namespace SelectChannel;

using System.Diagnostics.CodeAnalysis;

public interface IReadCase<T>
{
    public bool IsMatching { get; }

    public T Value { get; }

    public bool TryGetValue([NotNullWhen(true)] out T? value);
}