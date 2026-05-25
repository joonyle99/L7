using TMPro;
using UnityEngine;
using System.Collections;

public class PrepareKingController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _leftKingSp;
    [SerializeField] private GameObject _leftKingBubbleBox;
    [SerializeField] private TextMeshPro _leftKingBubbleText;

    [Space]

    [SerializeField] private Sprite _kingIdleSprite;
    [SerializeField] private Sprite _kingVivaSprite;

    [Space]

    [SerializeField] private float _idleDuration = 3f;
    [SerializeField] private float _bubbleDuration = 2f;

    private static readonly string[] _lines = { "소환하라!", "배치하라!" };

    private int _lineIdx;
    private Coroutine _cycleCoroutine;

    public void Initialize()
    {
        
    }

    public void StartCycle()
    {
        StopCycle();

        _lineIdx = 0;
        _cycleCoroutine = StartCoroutine(CycleCoroutine());
    }

    public void StopCycle()
    {
        if (_cycleCoroutine != null)
        {
            StopCoroutine(_cycleCoroutine);
            _cycleCoroutine = null;
        }

        SetIdle();
    }

    private IEnumerator CycleCoroutine()
    {
        while (true)
        {
            SetViva();

            yield return new WaitForSeconds(_bubbleDuration);

            SetIdle();

            yield return new WaitForSeconds(_idleDuration);
        }
    }

    private void SetIdle()
    {
        _leftKingBubbleBox.SetActive(false);
        _leftKingSp.sprite = _kingIdleSprite;
    }
    private void SetViva()
    {
        _leftKingBubbleBox.SetActive(true);
        _leftKingSp.sprite = _kingVivaSprite;

        _leftKingBubbleText.text = _lines[_lineIdx];
        _lineIdx = (_lineIdx + 1) % _lines.Length;
    }
}
