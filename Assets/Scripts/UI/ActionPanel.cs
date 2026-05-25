using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanel : UIPanel
{
    [SerializeField] private Button _summonButton;
    [SerializeField] private Button _battleButton;
    [SerializeField] private TextMeshProUGUI _summonCost;

    private RectTransform _summonButtonRectTrans;

    public Vector2 SummonButtonScreenPos => _summonButtonRectTrans.anchoredPosition;
    
    private void OnDestroy()
    {
        _summonButton.onClick.RemoveAllListeners();
        _battleButton.onClick.RemoveAllListeners();
    }

    public void Initialize(Action onSummonClicked, Action onBattleClicked)
    {
        _summonButtonRectTrans = _summonButton.GetComponent<RectTransform>();

        _summonButton.onClick.AddListener(() => onSummonClicked?.Invoke());
        _battleButton.onClick.AddListener(() => onBattleClicked?.Invoke());
    }

    public void SetSummonCostText(int prvCost, int currCost)
    {
        _summonCost.text = currCost.ToString();
    }
}
