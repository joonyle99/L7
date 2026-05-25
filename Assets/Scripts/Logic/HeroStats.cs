using DG.Tweening;
using TMPro;
using UnityEngine;

public class HeroStats : MonoBehaviour
{
    [SerializeField] private TextMeshPro _attackText;
    [SerializeField] private TextMeshPro _healthText;

    [Header("Punch Animation")]
    [SerializeField] private float _punchStrength = 0.4f;
    [SerializeField] private float _punchDuration = 0.35f;
    [SerializeField] private int   _punchVibrato   = 5;
    [SerializeField] private float _punchElasticity = 0.5f;

    public void SetAttackText(int attack, bool punch = false)
    {
        _attackText.text = attack.ToString();
        if (punch) PunchScale(_attackText.transform);
    }

    public void SetHealthText(int health, bool punch = false)
    {
        _healthText.text = health.ToString();
        if (punch) PunchScale(_healthText.transform);
    }

    private void PunchScale(Transform t)
    {
        t.DOKill();
        t.localScale = Vector3.one;
        t.DOScale(1f + _punchStrength, _punchDuration * 0.4f)
         .SetEase(Ease.OutQuad)
         .OnComplete(() => t.DOScale(1f, _punchDuration * 0.6f).SetEase(Ease.InQuad));
    }
}
