namespace SelectChannel;

internal class DefaultCase : IDefaultCase
{
    private bool _isReady;
    private bool _isMatching;

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
        internal set => _isMatching = value;
    }

    public void SetReady()
    {
        _isReady = true;
    }
}