using System;
using UnityEngine;

public abstract class BenchSlotController<TInstance, TView, TSlot> : ItemSlotController
    where TInstance : ItemInstance
    where TView : ItemView
    where TSlot : ItemSlot
{
    [SerializeField] protected Vector2 itemOffset = Vector2.zero;
    [SerializeField] private TView _viewPrefab;

    [Space]
    
    [SerializeField] private Color _pointHoverColor;

    private IBenchProvider<TInstance> _benchProvider;
    protected IBenchProvider<TInstance> BenchProvider => _benchProvider;

    private Func<TInstance, Vector3, Action, bool> _onSpawnEffect;
    private Action<TInstance, Vector3> _onHoverEnter;
    private Action _onHoverExit;

    private int _lastHoveredSlotIdx = -1;
    private int _lastPinnedSlotIdx = -1;

    public int LastHoveredSlotIdx => _lastHoveredSlotIdx;

    private Camera _mainCamera;

    private void OnDestroy()
    {
        if (_benchProvider != null) _benchProvider.OnBenchChanged -= Refresh;
    }

    protected void InitializeBench(
        IBenchProvider<TInstance> benchProvider,
        Func<TInstance, Vector3, Action, bool> onSpawnEffect = null,
        Action<TInstance, Vector3> onHoverEnter = null,
        Action onHoverExit = null)
    {
        _benchProvider = benchProvider;
        _benchProvider.OnBenchChanged += Refresh;
        _onSpawnEffect = onSpawnEffect;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;

        _mainCamera = Camera.main;

        InitializeSlots(benchProvider.Bench.Length);

        Refresh();
    }

    // ========== ... ==========

    protected override void Refresh()
    {
        var bench = _benchProvider.Bench;

        for (int idx = 0; idx < slots.Length; idx++)
        {
            var slot = GetSlotAtIndexAt(idx);
            var instance = idx < bench.Length ? bench[idx] : null;

            if (instance == null)
            {
                var view = GetViewFromSlot(slot);
                if (view != null)
                {
                    Destroy(view.gameObject);
                    SetViewToSlot(slot, null);
                }
            }
            else
            {
                var view = GetViewFromSlot(slot);
                if (view == null && !slot.IsPendingSpawn)
                {
                    if (_onSpawnEffect != null && !instance.HasBeenSpawned) SpawnViewWithEffect(slot, instance);
                    else SpawnView(slot, instance);
                }
                else if (view != null && GetInstanceFromView(view) != instance)
                {
                    SetupView(slot, instance);
                }
            }
        }
    }

    // ========== ... ==========

    public TInstance GetInstanceAt(int idx) => _benchProvider.Bench[idx];
    public bool IsSlotEmpty(int idx) => _benchProvider.Bench[idx] == null;
    public Vector3 GetItemWorldPosAt(int idx) => GetSlotWorldPos(idx) + (Vector3)itemOffset;
    protected TSlot GetSlotAtIndexAt(int idx) => (TSlot)slots[idx];
    public TView GetViewAt(int idx) => GetViewFromSlot(GetSlotAtIndexAt(idx));

    // ========== ... ==========

    protected virtual TView CreateView(TSlot slot) => Instantiate(_viewPrefab, slot.Root);
    protected virtual TView GetViewFromSlot(TSlot slot) => (TView)slot.View;
    protected virtual void SetViewToSlot(TSlot slot, TView view) => slot.View = view;
    protected virtual TInstance GetInstanceFromView(TView view) => (TInstance)view.ItemInstance;
    protected abstract void SetupView(TSlot slot, TInstance instance);

    // ========== ... ==========

    private void SpawnView(TSlot slot, TInstance instance)
    {
        var view = CreateView(slot);
        SetViewToSlot(slot, view);
        SetupView(slot, instance);
    }

    private void SpawnViewWithEffect(TSlot slot, TInstance instance)
    {
        instance.HasBeenSpawned = true;
        var effectPos = slot.Root.position + (Vector3)itemOffset;
        var effectHandled = _onSpawnEffect.Invoke(instance, effectPos, () =>
        {
            var summonSfx = SfxType.Summon_1 + UnityEngine.Random.Range(0, 4);
            SoundManager.Instance.PlaySfx(summonSfx);
            EffectManager.Instance.Play(VfxType.Spawn, effectPos);
            slot.IsPendingSpawn = false;
            SpawnView(slot, instance);
        });

        if (effectHandled)
        {
            slot.IsPendingSpawn = true;
        }
    }

    // ========== ... ==========

    protected virtual void OnSlotHoverEnter(int idx)
    {
        GetViewFromSlot(GetSlotAtIndexAt(idx))?.PlayHoverAnimation();
        _onHoverEnter?.Invoke(BenchProvider.Bench[idx], GetItemWorldPosAt(idx));
    }

    protected virtual void OnSlotHoverExit(int idx)
    {
        _onHoverExit?.Invoke();
    }

    public void TryHoverAt(Vector2 screenPos, bool excludeEmpty = false, int excludeIdx = -1)
    {
        var worldPos = (Vector3)_mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;
        var currHoverSlotIdx = GetSlotIndexAtWorldPos(worldPos);

        if (excludeEmpty && currHoverSlotIdx != -1)
        {
            if (currHoverSlotIdx == excludeIdx || IsSlotEmpty(currHoverSlotIdx))
                currHoverSlotIdx = -1;
        }

        if (currHoverSlotIdx == _lastHoveredSlotIdx) return;

        if (_lastHoveredSlotIdx != -1 && _lastHoveredSlotIdx != _lastPinnedSlotIdx)
        {
            slots[_lastHoveredSlotIdx]?.SetActiveFrame(false);
            OnSlotHoverExit(_lastHoveredSlotIdx);
        }

        _lastHoveredSlotIdx = currHoverSlotIdx;

        if (_lastHoveredSlotIdx != -1)
        {
            slots[_lastHoveredSlotIdx]?.SetActiveFrame(true);
            OnSlotHoverEnter(_lastHoveredSlotIdx);
        }
    }

    public void ClearHover()
    {
        if (_lastHoveredSlotIdx != -1 && _lastHoveredSlotIdx != _lastPinnedSlotIdx)
            slots[_lastHoveredSlotIdx]?.SetActiveFrame(false);

        _lastHoveredSlotIdx = -1;
    }

    // ========== ... ==========

    public void SetPinnedIdx(int idx)
    {
        if (_lastPinnedSlotIdx != -1 && _lastPinnedSlotIdx != _lastHoveredSlotIdx)
            slots[_lastPinnedSlotIdx]?.SetActiveFrame(false);

        _lastPinnedSlotIdx = idx;

        if (_lastPinnedSlotIdx != -1)
            slots[_lastPinnedSlotIdx]?.SetActiveFrame(true);
    }

    public void SetPointHoverColor(int idx)
    {
        if (idx < 0 || idx >= slots.Length) return;
        slots[idx].SetPointColor(_pointHoverColor);
    }

    public void ResetPointColor(int idx)
    {
        if (idx < 0 || idx >= slots.Length) return;
        slots[idx].SetPointColor();
    }
}
