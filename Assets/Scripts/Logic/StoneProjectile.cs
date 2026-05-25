using System;
using DG.Tweening;
using UnityEngine;

public class StoneProjectile : MonoBehaviour
{
    [SerializeField] private float _arcHeight = 1.5f;
    [SerializeField] private float _duration = 0.4f;

    private Action _onArrived;
    private Vector3 _start;
    private Vector3 _end;

    public void Launch(Vector3 start, Vector3 end, Action onArrived)
    {
        _start = start;
        _end = end;
        _onArrived = onArrived;
        transform.position = start;

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
