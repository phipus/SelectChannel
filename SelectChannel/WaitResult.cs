namespace SelectChannel;

internal readonly struct WaitResult
{
    public bool Result { get; init; }
    public int Index { get; init; }
}