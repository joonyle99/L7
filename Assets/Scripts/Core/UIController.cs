using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class UIController : MonoBehaviour, IGameStateListener<InGameState>
{
    [SerializeField] private HUDPanel _hudPanel;
    [SerializeField] private ActionPanel _actionPanel;
    [SerializeField] private InfoPanel _infoPanel;

    [Space]

    [SerializeField] private ControllerPanel _controllerPanel;
    [SerializeField] private TouchPanel _touchPanel;

    [Space]

    [SerializeField] private ToastPanel _toastPanel;

    [Space]

    [Header("Letterbox Transition")]
    [SerializeField] private RectTransform _letterboxTop;
    [SerializeField] private RectTransform _letterboxBottom;
    [SerializeField] private float _letterboxCloseTime = 0.5f;
    [SerializeField] private float _letterboxHoldTime = 0.2f;
    [SerializeField] private float _letterboxOpenTime = 0.5f;
    
    public InfoPanel InfoPanel => _infoPanel;
    
    private Camera _camera;

    private void OnDestroy()
    {
        
    }

    public void Initialize(int startToken, Action onSummonClicked, Action onBattleClicked, Action onPlaybackClicked, Action onAutoClicked, Action onSpeedClicked, Action onTouchClicked)
    {
        _camera = Camera.main;

        _hudPanel.Initialize(startToken);
        _actionPanel.Initialize(onSummonClicked, onBattleClicked);
        _infoPanel.Initialize();
        _controllerPanel.Initialize(onPlaybackClicked, onAutoClicked, onSpeedClicked);
        _touchPanel.Initialize(onTouchClicked);
    }

    public void OnStateChanged(InGameState prevState, InGameState currState)
    {

    }

    public void PlayLetterboxTransition(Action onBlack, Action onComplete = null)
    {
        if (_letterboxTop == null || _letterboxBottom == null)
        {
            onBlack?.Invoke();
            onComplete?.Invoke();
            return;
        }

        var barHeight = _letterboxTop.rect.height;

        _letterboxTop.gameObject.SetActive(true);
        _letterboxBottom.gameObject.SetActive(true);
        _letterboxTop.anchoredPosition = Vector2.zero;
        _letterboxBottom.anchoredPosition = Vector2.zero;

        DOTween.Sequence()
            .Join(_letterboxTop.DOAnchorPosY(-barHeight, _letterboxCloseTime).SetEase(Ease.InQuad))
            .Join(_letterboxBottom.DOAnchorPosY(barHeight, _letterboxCloseTime).SetEase(Ease.InQuad))
            .AppendCallback(() => onBlack?.Invoke())
            .AppendInterval(_letterboxHoldTime)
            .Append(_letterboxTop.DOAnchorPosY(0, _letterboxOpenTime).SetEase(Ease.OutQuad))
            .Join(_letterboxBottom.DOAnchorPosY(0, _letterboxOpenTime).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                onComplete?.Invoke();
                _letterboxTop.gameObject.SetActive(false);
                _letterboxBottom.gameObject.SetActive(false);
            });
    }

    public void SetGoldText(int gold) => _hudPanel.SetGoldText(gold);
    public void SetTimerText(float seconds) => _hudPanel.SetTimerText(seconds);
    public void SetRoundText(int round) => _hudPanel.SetRoundText(round);
    public void SetTokens(int playerTokens, int enemyTokens) => _hudPanel.SetTokens(playerTokens, enemyTokens);
    public void SetSummonCostText(int prevCost, int currCost) => _actionPanel.SetSummonCostText(prevCost, currCost);
    public void ShowToast(string message) => _toastPanel?.ShowToast(message);
    public void PlayPendingTokenEffects() => _hudPanel.PlayPendingTokenEffects();

    public void SetPlaybackState(bool isPlaying) => _controllerPanel.SetPlaybackState(isPlaying);
    public void SetAutoState(bool isActive) => _controllerPanel.SetAutoState(isActive);
    public void SetSpeedState(bool isActive) => _controllerPanel.SetSpeedState(isActive);

    public Vector3 GetSummonButtonWorldPos()
    {
        var screenPos = _actionPanel.SummonButtonScreenPos;
        var worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

#if UNITY_EDITOR
        // Debug.Log(screenPos);
        // Debug.Log(worldPos);
        Debug.DrawRay(worldPos, Vector3.up * 0.5f, Color.red, 1f);
        Debug.DrawRay(worldPos, Vector3.right * 0.5f, Color.green, 1f);
#endif

        return worldPos;
    }
}
