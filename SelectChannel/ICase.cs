namespace SelectChannel;

using System.Threading;
using System.Threading.Tasks;

internal interface ICase
{
    Task<WaitResult> Wait(int index, CancellationToken cancellationToken);

    bool TryComplete(bool waitResult);
    bool TryComplete();

    void SetReady();
}