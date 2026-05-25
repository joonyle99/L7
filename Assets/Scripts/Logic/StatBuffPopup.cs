using TMPro;
using System;
using DG.Tweening;
using UnityEngine;

public class StatBuffPopup : MonoBehaviour
{
    [SerializeField] private float _spreadDistance = 0.6f;
    [SerializeField] private float _spreadDuration = 0.35f;
    [SerializeField] private float _spreadAngle = 30f;
    [SerializeField] private float _holdDuration = 0.5f;
    [SerializeField] private float _fadeDuration = 0.2f;

    [SerializeField] private GameObject _attackIcon;
    [SerializeField] private GameObject _healthIcon;
    [SerializeField] private TextMeshPro _attackText;
    [SerializeField] private TextMeshPro _healthText;

    public void Launch(AbilityEffect effect, int value, Action onComplete)
    {
        bool showAttack = effect == AbilityEffect.IncreaseAttack || effect == AbilityEffect.DecreaseAttack || effect == AbilityEffect.IncreaseAttackHealth;
        bool showHealth = effect == AbilityEffect.IncreaseHealth || effect == AbilityEffect.IncreaseAttackHealth;

        _attackIcon?.SetActive(showAttack);
        _healthIcon?.SetActive(showHealth);

        if (_attackText != null && showAttack) _attackText.text = (effect == AbilityEffect.DecreaseAttack ? "-" : "+") + value;
        if (_healthText != null && showHealth) _healthText.text = "+" + value;

        // 아이콘을 중심(0,0)에서 시작
        if (_attackIcon != null) _attackIcon.transform.localPosition = Vector3.zero;
        if (_healthIcon != null) _healthIcon.transform.localPosition = Vector3.zero;

        float rad = _spreadAngle * Mathf.Deg2Rad;
        var leftDir  = new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), 0f) * _spreadDistance;
        var rightDir = new Vector3( Mathf.Sin(rad), Mathf.Cos(rad), 0f) * _spreadDistance;

        var seq = DOTween.Sequence();

        if (showAttack && showHealth)
        {
            if (_attackIcon != null) seq.Join(_attackIcon.transform.DOLocalMove(leftDir,  _spreadDuration).SetEase(Ease.OutBack));
            if (_healthIcon != null) seq.Join(_healthIcon.transform.DOLocalMove(rightDir, _spreadDuration).SetEase(Ease.OutBack));
        }
        else
        {
            var icon = showAttack ? _attackIcon : _healthIcon;
            if (icon != null) seq.Join(icon.transform.DOLocalMove(Vector3.up * _spreadDistance, _spreadDuration).SetEase(Ease.OutBack));
        }

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            DOTween.Sequence()
                .AppendInterval(_holdDuration)
                .Append(transform.DOScale(Vector3.zero, _fadeDuration).SetEase(Ease.InBack))
                .OnComplete(() => Destroy(gameObject));
        });
    }
}
