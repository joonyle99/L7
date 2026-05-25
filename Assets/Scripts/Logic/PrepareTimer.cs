using System;

public class PrepareTimer
{
    private readonly float _duration;

    private float _remaining;
    public float Remaining => _remaining;
    private bool _isRunning;
    public bool IsRunning => _isRunning;

    private event Action<float> _onTick;
    private event Action _onExpired;

    public PrepareTimer(float duration)
    {
        _duration = duration;
    }

    public void Initialize(Action<float> onTick, Action onExpired)
    {
        _onTick += onTick;
        _onExpired += onExpired;
    }

    public void OnStateChanged(InGameState prevState, InGameState currState)
    {
        if (currState == InGameState.Prepare) Start();
        else if (prevState == InGameState.Prepare) Stop();
    }

    public void Start()
    {
        _remaining = _duration;
        _isRunning = true;
        _onTick?.Invoke(_remaining);
    }

    public void Stop()
    {
        _isRunning = false;
    }

    public void Tick(float deltaTime)
    {
        if (!_isRunning) return;

        _remaining -= deltaTime;
        
        if (_remaining <= 0f)
        {
            _remaining = 0f;
            _isRunning = false;
            _onTick?.Invoke(_remaining);
            _onExpired?.Invoke();

            return;
        }

        _onTick?.Invoke(_remaining);
    }
}
