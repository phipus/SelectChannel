using System;
using System.Linq;

namespace SelectChannel;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public class Select
{
    private readonly List<ICase> _cases = [];
    private DefaultCase? _defaultCase;
    private bool _useShuffle = true;

    public IReadCase<T> Read<T>(ChannelReader<T> reader)
    {
        var readCase = new ReadCase<T>(reader);
        _cases.Add(readCase);
        return readCase;
    }

    public IWriteCase Write<T>(ChannelWriter<T> writer, T value)
    {
        var writeCase = new WriteCase<T>(writer, value);
        _cases.Add(writeCase);
        return writeCase;
    }

    public IDefaultCase DefaultCase()
    {
        return _defaultCase ??= new DefaultCase();
    }

    public async ValueTask Wait(CancellationToken cancellationToken = default)
    {
        if (_useShuffle)
        {
            ShuffleCases();
        }

        if (_defaultCase != null)
        {
            if (_cases.Any(c => c.TryComplete()))
            {
                SetCasesReady();
                _defaultCase.SetReady();
                return;
            }

            _defaultCase.IsMatching = true;
            SetCasesReady();
            _defaultCase.SetReady();
            return;
        }

        var tasks = new Task<WaitResult>[_cases.Count];

        while (true)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = _cases[i].Wait(i, cts.Token);
            }

            var result = await await Task.WhenAny(tasks);
            await cts.CancelAsync();

            if (!_cases[result.Index].TryComplete(result.Result))
            {
                continue;
            }

            SetCasesReady();

            return;
        }
    }

    public void UseShuffle(bool useShuffle)
    {
        _useShuffle = useShuffle;
    }

    public static Select Setup()
    {
        return new Select();
    }

    private void SetCasesReady()
    {
        foreach (var c in _cases)
        {
            c.SetReady();
        }
    }

    private void ShuffleCases()
    {
        var random = new Random();
        for (var i = 1; i < _cases.Count; i++)
        {
            var index = random.Next(i);
            SwapListEntries(_cases, i, index);
        }
    }

    private static void SwapListEntries<T>(List<T> list, int a, int b)
    {
        (list[a], list[b]) = (list[b], list[a]);
    }
}