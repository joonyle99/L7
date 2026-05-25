using System;
using DG.Tweening;
using UnityEngine;
using System.Collections.Generic;

public class SummonTrail : MonoBehaviour
{
    [SerializeField] private float _arcHeight = 2f;
    [SerializeField] private float _duration = 0.6f;

    [Space]

    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private TrailRenderer _trail;

    private readonly List<Vector3> _controlPoints = new List<Vector3>(3);
    private Action _onArrived;

    public void Launch(Vector3 start, Vector3 end, Action onArrived, Color? color = null)
    {
        _spriteRenderer.color = color ?? Color.white;
        _trail.startColor = color ?? Color.white;
        _trail.endColor = color ?? Color.white;
        
        _onArrived = onArrived;

        transform.position = start;

        var mid = (start + end) * 0.5f + Vector3.up * _arcHeight;
        _controlPoints.Clear();
        _controlPoints.Add(start);
        _controlPoints.Add(mid);
        _controlPoints.Add(end);

        DOVirtual.Float(0f, 1f, _duration, OnTick).SetEase(Ease.Linear).OnComplete(OnArrived);

#if UNITY_EDITOR
        Debug.DrawLine(start, mid, Color.yellow, _duration);
        Debug.DrawLine(mid, end, Color.yellow, _duration);
#endif
    }

    private void OnTick(float t)
    {
        transform.position = BezierCurve.Evaluate(_controlPoints, t);
    }

    private void OnArrived()
    {
        _onArrived?.Invoke();
        Destroy(gameObject);
    }
}
