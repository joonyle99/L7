using System;

public class TokenSystem
{
    private int _playerTokens;
    public int PlayerTokens => _playerTokens;
    private int _enemyTokens;
    public int EnemyTokens => _enemyTokens;

    private event Action<int, int> _onTokensChanged;
    public event Action<RoundOutcome> _onTokensRunOut;

    public TokenSystem(int startToken)
    {
        _playerTokens = startToken;
        _enemyTokens = startToken;
    }

    public void Initialize(Action<int, int> onTokensChanged, Action<RoundOutcome> onTokensRunOut)
    {
        _onTokensChanged = onTokensChanged;
        _onTokensRunOut = onTokensRunOut;

        _onTokensChanged?.Invoke(_playerTokens, _enemyTokens);
    }

    public void ApplyRoundOutcome(RoundOutcome roundOutcome)
    {
        switch (roundOutcome)
        {
            case RoundOutcome.Player_Win:
                _enemyTokens = Math.Max(0, _enemyTokens - 1);
                break;
            case RoundOutcome.Enemy_Win:
                _playerTokens = Math.Max(0, _playerTokens - 1);
                break;
            case RoundOutcome.Draw:
                break;
        }

        _onTokensChanged?.Invoke(_playerTokens, _enemyTokens);

        if (_playerTokens == 0 || _enemyTokens == 0)
        {
            var matchOutcome =
                _playerTokens == 0 && _enemyTokens == 0 ? RoundOutcome.Draw :
                _playerTokens == 0 ? RoundOutcome.Enemy_Win :
                RoundOutcome.Player_Win;
            _onTokensRunOut?.Invoke(matchOutcome);
        }
    }
}
