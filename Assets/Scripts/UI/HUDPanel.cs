using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HUDPanel : UIPanel
{
    [SerializeField] private TextMeshProUGUI _gold;
    [SerializeField] private TextMeshProUGUI _timer;
    [SerializeField] private TextMeshProUGUI _round;
    [SerializeField] private RectTransform _leftTokenRoot;
    [SerializeField] private RectTransform _rightTokenRoot;
    [SerializeField] private Token _leftTokenPrefab;
    [SerializeField] private Token _rightTokenPrefab;

    private Token[] _leftTokens;
    private Token[] _rightTokens;
    private int _prevLeftCount;
    private int _prevRightCount;
    private readonly List<int> _pendingLeftDissolves = new();
    private readonly List<int> _pendingRightDissolves = new();

    public void Initialize(int startToken)
    {
        _leftTokens = CreateTokens(startToken, _leftTokenPrefab, _leftTokenRoot);
        _rightTokens = CreateTokens(startToken, _rightTokenPrefab, _rightTokenRoot);
        _prevLeftCount = startToken;
        _prevRightCount = startToken;
    }

    private Token[] CreateTokens(int count, Token prefab, RectTransform root)
    {
        var tokens = new Token[count];

        for (int i = 0; i < count; i++)
        {
            tokens[i] = Instantiate(prefab, root);
        }

        return tokens;
    }

    public void SetGoldText(int gold)
    {
        _gold.text = gold.ToString();
    }

    public void SetTimerText(float seconds)
    {
        _timer.text = TimeFormatter.FormatMMSS(seconds);
    }

    public void SetRoundText(int round)
    {
        _round.text = $"Round {round}";
    }

    public void SetTokens(int leftTokenCount, int rightTokenCount)
    {
        for (int i = leftTokenCount; i < _prevLeftCount; i++)
            _pendingLeftDissolves.Add(i);
        for (int i = rightTokenCount; i < _prevRightCount; i++)
            _pendingRightDissolves.Add(i);

        for (int i = 0; i < _leftTokens.Length; i++)
            _leftTokens[i].Icon.gameObject.SetActive(i < leftTokenCount);
        for (int i = 0; i < _rightTokens.Length; i++)
            _rightTokens[i].Icon.gameObject.SetActive(i < rightTokenCount);

        _prevLeftCount = leftTokenCount;
        _prevRightCount = rightTokenCount;
    }

    public void PlayPendingTokenEffects()
    {
        foreach (var i in _pendingLeftDissolves) _leftTokens[i].PlayDestroyEffect();
        foreach (var i in _pendingRightDissolves) _rightTokens[i].PlayDestroyEffect();
        _pendingLeftDissolves.Clear();
        _pendingRightDissolves.Clear();
    }
}
