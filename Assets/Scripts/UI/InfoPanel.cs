using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : UIPanel
{
    [SerializeField] private RectTransform _panel;

    [SerializeField] private GradeConfig _gradeConfig;

    [SerializeField] private Image _icon;
    [SerializeField] private Image _gradeMainIcon;
    [SerializeField] private Image _gradeSubIcon;
    [SerializeField] private TextMeshProUGUI _gradeText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _abilityMainText;
    [SerializeField] private TextMeshProUGUI _abilitySubText;

    [SerializeField] private Vector2 _pinnedAnchoredPos;
    [SerializeField] private Vector2 _hoverOffset;

    private RectTransform _canvasRectTransform;
    private CameraController _cameraController;
    private Camera _mainCamera;
    private Camera _uiCamera;

    private bool _isPinned;

    public void Initialize()
    {
        _canvasRectTransform = GetComponent<RectTransform>();
        _cameraController = FindFirstObjectByType<CameraController>();
        _mainCamera = _cameraController.MainCamera;
        _uiCamera = _cameraController.UICamera;
    }

    // ========== ... ==========

    public void ShowHover(HeroInstance hero, Vector3 worldPos)
    {
        if (_isPinned) return;
        Populate(hero);
        SetHoveredPos(worldPos);
        Show();
    }

    public void HideHover()
    {
        if (_isPinned) return;
        Hide();
    }

    public void ShowPinned(HeroInstance hero, Vector3 worldPos)
    {
        _isPinned = true;
        SetHoveredPos(worldPos);
        Populate(hero);
        Show();
    }

    public void Unpin()
    {
        _isPinned = false;
        Hide();
    }

    // ========== ... ==========

    // TODO: 이후 화면 어디에 Info Panel을 배치하면 좋을지 고민해보기
    private void SetHoveredPos(Vector3 worldPos)
    {
        var screenPos = _mainCamera.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, screenPos, _uiCamera, out var basePoint);

        var canvasHalf = _canvasRectTransform.rect.size * 0.5f;
        var panelHalf = _panel.rect.size * 0.5f;
        var offset = _hoverOffset;
        var tentative  = basePoint + offset;

        if (tentative.x - panelHalf.x < -canvasHalf.x || tentative.x + panelHalf.x > canvasHalf.x)
            offset.x = -offset.x;
        if (tentative.y - panelHalf.y < -canvasHalf.y || tentative.y + panelHalf.y > canvasHalf.y)
            offset.y = -offset.y;

        _panel.anchoredPosition = basePoint + offset;
    }

    private void SetPinnedPos()
    {
        // _panel.anchoredPosition = _pinnedAnchoredPos;
    }

    // ========== ... ==========

    private void Populate(HeroInstance hero)
    {
        var data = hero.Data;
        var name = data.Name;
        var grade = data.Grade;
        var ability = data.Ability;
        _icon.sprite = data.Icon;
        _icon.GetComponent<RectTransform>().anchoredPosition = data.IconAnchoredPos;
        if (_gradeConfig.TryGetGradeVisual(grade, out var visual))
        {
            _gradeMainIcon.sprite = visual.MainIcon;
            _gradeSubIcon.sprite = visual.SubIcon;
        }
        _gradeText.text = grade.ToDisplayText();
        _nameText.text = name;
        _levelText.text = $"Lv.{hero.Level}";
        var triggerText = ability.Trigger.ToDisplayText();
        var effectText = ability.Effect.ToDisplayText(ability.Target, ability.GetValue(hero.Level));
        _abilityMainText.text = $"{triggerText}, <color=red>{ability.Probability}%</color> 확률로\n{LuckyExtensions.Sprite("Arrow")} {effectText}";
        _abilitySubText.text = $"{ability.FlavorText}";
    }

    // ========== ... ==========

    private void OnDrawGizmos()
    {
        RectTransform parentRect = _panel.parent as RectTransform;
        if (parentRect == null) return;

        Vector2 panelSize = _panel.rect.size;
        Vector2 pivot = _panel.pivot;
        float sphereSize = panelSize.x * 0.05f;

        // === Pinned (cyan) ===
        DrawPanelGizmo(_pinnedAnchoredPos, parentRect, panelSize, pivot, Color.cyan, sphereSize);

        // === Hover (yellow) ===
        DrawPanelGizmo(_panel.anchoredPosition + _hoverOffset, parentRect, panelSize, pivot, Color.yellow, sphereSize);

        // 오프셋 연결선
        Vector3 pinnedWorld = parentRect.TransformPoint(_pinnedAnchoredPos);
        Vector3 hoverWorld = parentRect.TransformPoint(_pinnedAnchoredPos + _hoverOffset);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(pinnedWorld, hoverWorld);
    }

    private void DrawPanelGizmo(Vector2 anchoredPos, RectTransform parent, Vector2 size, Vector2 pivot, Color color, float sphereSize)
    {
        Gizmos.color = color;

        Vector2 bl = anchoredPos - size * pivot;
        Vector2 tr = bl + size;

        Vector3 c0 = parent.TransformPoint(new Vector3(bl.x, bl.y, 0));
        Vector3 c1 = parent.TransformPoint(new Vector3(bl.x, tr.y, 0));
        Vector3 c2 = parent.TransformPoint(new Vector3(tr.x, tr.y, 0));
        Vector3 c3 = parent.TransformPoint(new Vector3(tr.x, bl.y, 0));

        Gizmos.DrawLine(c0, c1);
        Gizmos.DrawLine(c1, c2);
        Gizmos.DrawLine(c2, c3);
        Gizmos.DrawLine(c3, c0);
    }
}
