using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControllerPanel : UIPanel
{
    [SerializeField] private Button _playbackButton;
    [SerializeField] private Button _autoButton;
    [SerializeField] private Button _speedButton;

    [Space]

    [SerializeField] private Image _playbackIcon;
    [SerializeField] private Image _autoIcon;
    [SerializeField] private Image _speedIcon;

    [Space]

    [SerializeField] private TextMeshProUGUI _playbackText;
    [SerializeField] private TextMeshProUGUI _autoText;
    [SerializeField] private TextMeshProUGUI _speedText;

    [Space]

    [SerializeField] private UIButtonOverlayFeedback _playbackFeedback;
    [SerializeField] private UIButtonOverlayFeedback _autoFeedback;
    [SerializeField] private UIButtonOverlayFeedback _speedFeedback;

    [Space]

    [SerializeField] private Sprite _startIcon;
    [SerializeField] private Sprite _stopIcon;

    private void OnDestroy()
    {
        _playbackButton.onClick.RemoveAllListeners();
        _autoButton.onClick.RemoveAllListeners();
        _speedButton.onClick.RemoveAllListeners();
    }

    public void Initialize(Action onPlaybackClicked, Action onAutoClicked, Action onSpeedClicked)
    {
        _playbackButton.onClick.AddListener(() => onPlaybackClicked?.Invoke());
        _autoButton.onClick.AddListener(() => onAutoClicked?.Invoke());
        _speedButton.onClick.AddListener(() => onSpeedClicked?.Invoke());
    }

    public void SetPlaybackState(bool isPlaying)
    {
        _playbackIcon.sprite = isPlaying ? _stopIcon : _startIcon;
        _playbackText.text = isPlaying ? "정지" : "재생";
        _playbackIcon.SetNativeSize();
    }

    public void SetAutoState(bool isActive) => _autoFeedback?.RefreshOverlay(isActive);
    public void SetSpeedState(bool isActive) => _speedFeedback?.RefreshOverlay(isActive);
}
