using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Token : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;

    [Header("Dissolve Effect")]
    [SerializeField] private int _sliceCount = 4;
    [SerializeField] private float _force = 30f;
    [SerializeField] private float _gravityScale = -0.1f;
    [SerializeField] private float _fadeDelay = 0.2f;
    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private float _sourceFadeOutDuration = 0.2f;

    public Image Icon => _icon;

    public void PlayDestroyEffect()
    {
        var iconRect = _icon.rectTransform;

        var corners = new Vector3[4];
        iconRect.GetWorldCorners(corners);
        Vector3 worldPos = new Vector3(
            (corners[0].x + corners[2].x) * 0.5f,
            (corners[0].y + corners[2].y) * 0.5f,
            0f);

        float iconWorldHeight = corners[1].y - corners[0].y;
        float scaleFactor = iconWorldHeight / _icon.sprite.bounds.size.y;

        _icon.gameObject.SetActive(false);
        _background.gameObject.SetActive(false);

        var tempGO = new GameObject("TokenDissolveTemp");
        var sr = tempGO.AddComponent<SpriteRenderer>();
        sr.sprite = _icon.sprite;
        sr.color = _icon.color;
        sr.sortingOrder = 999;
        tempGO.transform.position = worldPos;
        tempGO.transform.localScale = Vector3.one * scaleFactor;

        SpriteExploder.Dissolve(sr, worldPos,
            sliceCount: _sliceCount,
            force: _force,
            gravityScale: _gravityScale,
            fadeDelay: _fadeDelay,
            fadeDuration: _fadeDuration);

        sr.DOFade(0f, _sourceFadeOutDuration).OnComplete(() => Destroy(tempGO));
    }
}
