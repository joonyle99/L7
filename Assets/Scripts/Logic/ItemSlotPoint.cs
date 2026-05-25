using DG.Tweening;
using UnityEngine;

public class ItemSlotPoint : MonoBehaviour
{
    private const float BOB_AMPLITUDE = 0.15f;
    private const float BOB_DURATION = 0.5f;
    private const float SCALE_AMOUNT = 1.3f;
    private const float SCALE_DURATION = 0.4f;

    private Tween _bobTween;
    private Tween _scaleTween;
    private float _originPosY;
    private Vector3 _originScale = Vector3.one;
    private Color _originColor = Color.white;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        _originPosY = transform.localPosition.y;
        _originScale = transform.localScale;
        _originColor = _spriteRenderer.color;

        _bobTween = transform
            .DOLocalMoveY(_originPosY + BOB_AMPLITUDE, BOB_DURATION)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDestroy()
    {
        _bobTween?.Kill();
        _scaleTween?.Kill();
    }

    public void SetActive(bool active, bool highlight = false)
    {
        _scaleTween?.Kill();
        _scaleTween = null;
        
        transform.localScale = _originScale;

        if (!active)
        {
            transform.localScale = _originScale;
            _spriteRenderer.color = _originColor;
        }
        else if (highlight)
        {
            _scaleTween = transform
                .DOScale(_originScale * SCALE_AMOUNT, SCALE_DURATION)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        gameObject.SetActive(active);
    }

    public void SetColor(Color? color = null) => _spriteRenderer.color = color ?? _originColor;
}
