using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum HeroViewState { Normal, Attack, Ability }
public enum HeroSpriteType { Idle = 0, Attack = 1 }

public class HeroView : ItemView
{
    [SerializeField] private GameObject _level;
    [SerializeField] private HeroStats _heroStats;

    [Space]
    
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Button _levelUpButton;
    [SerializeField] private GameObject _dustEffect;

    private HeroInstance _heroInstance;
    public HeroInstance HeroInstance => _heroInstance;
    public override ItemInstance ItemInstance => _heroInstance;

    private TextMeshPro _levelText;
    private TextMeshPro _stackText;
    private GradeConfig _gradeConfig;

    // 런타임에 생성되는 애들
    private SpriteRenderer[] _spriteRenderers;
    private SpriteRenderer[] _shadows;

    public HeroViewState _viewState;

    protected override void Awake()
    {
        base.Awake();

        _levelText = _level.transform.GetChild(0).GetComponent<TextMeshPro>();
        _stackText = _level.transform.GetChild(1).GetComponent<TextMeshPro>();
        _spriteRenderers = new SpriteRenderer[itemSlots.Length];
        _shadows = new SpriteRenderer[itemSlots.Length];
        _canvas.worldCamera = Camera.main;
        _levelUpButton.onClick.AddListener(OnLevelUpClicked);
        _viewState = HeroViewState.Normal;
    }

    public void Setup(HeroInstance heroInstance, Vector3 spawnOffset, GradeConfig gradeConfig = null)
    {
        _heroInstance = heroInstance;
        _gradeConfig = gradeConfig;

        for (int i = 1; i < itemSlots.Length; i++) ClearItem(i);
        ApplyItem(heroInstance.Data.Prefab, spawnOffset);
        Refresh();
    }

    public void Refresh()
    {
        _levelText.text = $"Lv.{_heroInstance.Level}";
        
        var stack = _heroInstance.Stack;
        var data = _heroInstance.Data;

        _stackText.text = $"{stack} / {data.MaxStack}";

        for (int i = 1; i < itemSlots.Length; i++)
        {
            var shouldOccupy = i <= stack;
            if (shouldOccupy && !IsSlotOccupied(i))
                ApplyItemToSlot(data.Prefab, i);
            else if (!shouldOccupy && IsSlotOccupied(i))
                ClearItem(i);
        }
        
        SetViewState(_viewState);

        _heroStats.SetAttackText(_heroInstance.Attack);
        _heroStats.SetHealthText(_heroInstance.Health);
    }

    public void Refresh(int attack, int health, bool punch = false)
    {
        _heroStats.SetAttackText(attack, punch);
        _heroStats.SetHealthText(health, punch);
    }

    public void Clear()
    {
        ClearItem();
        _heroInstance = null;
    }

    // ======== ... ========

    protected override void OnPrefabInstantiated(int slotIdx, GameObject prefabObject)
    {
        _spriteRenderers[slotIdx] = prefabObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
        _shadows[slotIdx] = prefabObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
        if (_gradeConfig.TryGetGradeVisual(_heroInstance.Data.Grade, out var visual))
            _shadows[slotIdx].color = visual.Color;
    }

    protected override void OnSlotCleared(int slotIdx)
    {
        _spriteRenderers[slotIdx] = null;
        _shadows[slotIdx] = null;
    }

    private void OnLevelUpClicked()
    {
        
    }

    // ======== ... ========

    public override void SetDragging(bool isDragging)
    {
        base.SetDragging(isDragging);

        foreach (var shadow in _shadows) shadow?.gameObject.SetActive(!isDragging);
        _level.SetActive(!isDragging);
        _heroStats.gameObject.SetActive(!isDragging);
    }

    public void SetViewState(HeroViewState viewState)
    {
        _viewState = viewState;

        var (color, thickness, distortion) = viewState switch
        {
            HeroViewState.Normal => (Color.white, 0f, 0f),
            HeroViewState.Attack => (Color.red, 5f, 4f),
            HeroViewState.Ability => (Color.green, 5f, 4f),
            _ => (Color.white, 0f, 0f)
        };

        var mpb = new MaterialPropertyBlock();
        foreach (var sp in _spriteRenderers)
        {
            if (sp == null) continue;
            sp.GetPropertyBlock(mpb);
            mpb.SetColor("_OuterOutlineColor", color);
            mpb.SetFloat("_OuterOutlineThickness", thickness);
            mpb.SetFloat("_FlickerDistortion", distortion);
            sp.SetPropertyBlock(mpb);
        }
    }

    public void SetSprite(HeroSpriteType spriteType)
    {
        var sprite = _heroInstance.Data.Sprites[(int)spriteType];
        foreach (var sr in _spriteRenderers) if (sr != null) sr.sprite = sprite;
    }

    public void SetActiveLevelUp(bool active)
    {
        _levelUpButton.gameObject.SetActive(active);
    }

    public void SetActiveDustEffect(bool active)
    {
        _dustEffect.SetActive(active);
    }
}
