namespace SelectChannel;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

internal class ReadCase<T>(ChannelReader<T> reader) : ICase, IReadCase<T>
{
    private bool _isCompleted;
    private bool _isMatching;
    private bool _isReady;
    private T? _value;

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

    public T Value
    {
        get
        {
            if (TryGetValue(out var result))
            {
                return result;
            }

            throw new ChannelClosedException();
        }
    }

    public async Task<WaitResult> Wait(int index, CancellationToken cancellationToken)
    {
        var result = await reader.WaitToReadAsync(cancellationToken);
        return new WaitResult { Result = result, Index = index };
    }

    public bool TryComplete(bool waitResult)
    {
        if (!waitResult)
        {
            _isCompleted = true;
            _isMatching = true;
            return true;
        }

        if (!reader.TryRead(out var result))
        {
            return false;
        }

        _isCompleted = false;
        _isMatching = true;
        _value = result;
        return true;
    }

    public bool TryComplete()
    {
        if (reader.TryRead(out var result))
        {
            _isCompleted = false;
            _isMatching = true;
            _value = result;
            return true;
        }

        if (!reader.Completion.IsCompleted)
        {
            return false;
        }

        _isCompleted = true;
        _isMatching = true;
        return true;
    }

    public void SetReady()
    {
        _isReady = true;
    }

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        if (!IsMatching)
        {
            throw new InvalidOperationException("can not get value of an non-matching case");
        }

        if (_isCompleted)
        {
            value = default;
            return false;
        }

        value = _value!;
        return true;
    }
}