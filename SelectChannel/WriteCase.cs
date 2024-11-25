namespace SelectChannel;

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

internal class WriteCase<T>(ChannelWriter<T> writer, T value) : ICase, IWriteCase
{
    private bool _isMatching;
    private bool _isReady;

    public bool IsMatching
    {
        get
        {
            if (!_isReady)
            {
                throw new CaseNotReadyException();
            }

            return _isMatching;
        }
    }

    public async Task<WaitResult> Wait(int index, CancellationToken cancellationToken)
    {
        var result = await writer.WaitToWriteAsync(cancellationToken);
        return new WaitResult { Result = result, Index = index };
    }

    public bool TryComplete(bool waitResult)
    {
        if (!waitResult)
        {
            throw new ChannelClosedException();
        }

        if (!writer.TryWrite(value))
        {
            return false;
        }

        _isMatching = true;
        return true;
    }

    public bool TryComplete()
    {
        if (writer.TryWrite(value))
        {
            _isMatching = true;
            return true;
        }

        if (writer.IsCompleted())
        {
            throw new ChannelClosedException();
        }

        return false;
    }

    public void SetReady()
    {
        _isReady = true;
    }
}