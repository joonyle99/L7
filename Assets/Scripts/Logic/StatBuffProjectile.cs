using System;
using DG.Tweening;
using UnityEngine;

public class StatBuffProjectile : MonoBehaviour
{
    [SerializeField] private float _arcHeight = 1.5f;
    [SerializeField] private float _duration = 0.4f;

    [SerializeField] private GameObject _attackIcon;
    [SerializeField] private GameObject _healthIcon;

    private Action _onArrived;
    private Vector3 _start;
    private Vector3 _end;

    public void Launch(Vector3 start, Vector3 end, AbilityEffect effect, Action onArrived)
    {
        _start = start;
        _end = end;
        _onArrived = onArrived;
        transform.position = start;

        _attackIcon?.SetActive(effect == AbilityEffect.IncreaseAttack || effect == AbilityEffect.DecreaseAttack || effect == AbilityEffect.IncreaseAttackHealth);
        _healthIcon?.SetActive(effect == AbilityEffect.IncreaseHealth || effect == AbilityEffect.IncreaseAttackHealth);

        DOVirtual.Float(0f, 1f, _duration, OnTick).SetEase(Ease.Linear).OnComplete(() =>
        {
            _onArrived?.Invoke();
            Destroy(gameObject);
        });
    }

    private void OnTick(float t)
    {
        float x = Mathf.Lerp(_start.x, _end.x, t);
        float y = Mathf.Lerp(_start.y, _end.y, t) + _arcHeight * 4f * t * (1f - t);
        transform.position = new Vector3(x, y, _start.z);
    }
}
