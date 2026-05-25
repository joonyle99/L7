using System;
using UnityEngine;
using DG.Tweening;
using JoonyleGameDevKit;
using System.Collections.Generic;

[Serializable]
public class PrepareManager
{
    // ========= 영웅 =========

    private SummonBenchManager _summonBenchManager;
    private SquadBenchManager _squadBenchManager;
    private HeroSlotController _summonSlotController;
    private HeroSlotController _squadSlotController;

    //
    private HeroSlotController _lastController;
    private int _lastSlotIdx;
    private HeroView _lastHeroView;

    // drag
    private Vector3 _dragOriginalPos;
    private Vector3 _dragOffset;
    private bool _isDragActive;
    public bool IsDragActive => _isDragActive;
    public bool SuppressHoverPanel { get; set; }
    public bool IsLocked { get; set; }
    private HeroView _pendingDropView; // 드롭 성공 시 OnBenchChanged로 _lastHeroView가 파괴되므로, 미리 목적지 슬롯의 뷰를 저장해 PlayDropAnimation에 사용

    // drag hover color
    private static readonly Color POINT_DRAG_HOVER_COLOR = Color.yellow;
    private HeroSlotController _dragHoverController;
    private int _dragHoverIdx = -1;

    // selection
    private HeroSlotController _selectedController;
    private int _selectedSlotIdx = -1;
    private Action<HeroInstance, bool, Vector3> _onHeroSelected;
    private Action _onHeroDeselected;

    // push
    private int _pushPreviewSlot = -1;
    private int _pushPreviewDir;
    private List<(int from, int to)> _pushPreviewChain;
    private HeroSlotController _pushPreviewController;

    // sold
    private HeroSellZone _heroSellZone;
    private Action<HeroInstance, Vector3> _onHeroSold;

    // ========= 유물 =========

    private RelicBenchManager _relicManager;
    private RelicSlotController _relicSlotController;
    
    // ========= 공통 =========
    
    private InputProvider _inputProvider;

    public PrepareManager(InputProvider inputProvider)
    {
        _inputProvider = inputProvider;
    }

    public void Initialize(
        SummonBenchManager summonBenchManager,
        SquadBenchManager squadBenchManager,
        RelicBenchManager relicBenchManager,
        HeroSlotController summonSlotController,
        HeroSlotController squadSlotController,
        RelicSlotController relicSlotController,
        HeroSellZone heroSellZone,
        Action<HeroInstance, Vector3> onHeroSold,
        Action<HeroInstance, bool, Vector3> onHeroSelected ,
        Action onHeroDeselected )
    {
        _summonBenchManager = summonBenchManager;
        _squadBenchManager = squadBenchManager;
        _relicManager = relicBenchManager;
        _summonSlotController = summonSlotController;
        _squadSlotController = squadSlotController;
        _relicSlotController = relicSlotController;
        _heroSellZone = heroSellZone;
        _onHeroSold = onHeroSold;
        _onHeroSelected = onHeroSelected;
        _onHeroDeselected = onHeroDeselected;

        _summonBenchManager.Initialize((dstIdx, isLevelUp) =>
        {
            var pos = _summonSlotController.GetItemWorldPosAt(dstIdx);
            if (isLevelUp)
            {
                EffectManager.Instance.Play(VfxType.LevelUp, pos);
                SoundManager.Instance.PlaySfx(SfxType.LevelUp);
            }
            else
            {
                EffectManager.Instance.Play(VfxType.Merge, pos);
                SoundManager.Instance.PlaySfx(SfxType.Merge);
            }
        });
        _squadBenchManager.Initialize((dstIdx, isLevelUp) =>
        {
            var pos = _squadSlotController.GetItemWorldPosAt(dstIdx);
            if (isLevelUp)
            {
                EffectManager.Instance.Play(VfxType.LevelUp, pos);
                SoundManager.Instance.PlaySfx(SfxType.LevelUp);
            }
            else
            {
                EffectManager.Instance.Play(VfxType.Merge, pos);
                SoundManager.Instance.PlaySfx(SfxType.Merge);
            }
        });
    }
    
    public void Tick()
    {
        if (IsLocked) return;
        if (_inputProvider.JustPressed) BeginDrag();
        else if (_inputProvider.IsDragging) UpdateDrag();
        else if (_inputProvider.JustReleased) EndDrag();

        UpdateHover();
    }

    // ========== ... ==========

    private void UpdateHover()
    {
        var screenPos = _inputProvider.GetScreenPos;

        if (_isDragActive)
        {
            var srcIdx = (_lastController == _summonSlotController) ? _lastSlotIdx : -1;
            _summonSlotController?.TryHoverAt(screenPos, false, srcIdx);
            srcIdx = (_lastController == _squadSlotController) ? _lastSlotIdx : -1;
            _squadSlotController?.TryHoverAt(screenPos, false, srcIdx);

            UpdateDragHoverColor();
        }
        else
        {
            _summonSlotController?.TryHoverAt(screenPos, true);
            _squadSlotController?.TryHoverAt(screenPos, true);
        }
    }

    private void UpdateDragHoverColor()
    {
        HeroSlotController currHoverController = null;
        int currHoverIdx = -1;

        if (_summonSlotController != null && _summonSlotController.LastHoveredSlotIdx >= 0)
        {
            currHoverController = _summonSlotController;
            currHoverIdx = _summonSlotController.LastHoveredSlotIdx;
        }
        else if (_squadSlotController != null && _squadSlotController.LastHoveredSlotIdx >= 0)
        {
            currHoverController = _squadSlotController;
            currHoverIdx = _squadSlotController.LastHoveredSlotIdx;
        }

        if (currHoverController == _dragHoverController && currHoverIdx == _dragHoverIdx) return;

        _dragHoverController?.ResetPointColor(_dragHoverIdx);
        _dragHoverController = currHoverController;
        _dragHoverIdx = currHoverIdx;
        _dragHoverController?.SetPointHoverColor(_dragHoverIdx);
    }

    private void ClearDragHoverColor()
    {
        _dragHoverController?.ResetPointColor(_dragHoverIdx);
        _dragHoverController = null;
        _dragHoverIdx = -1;
    }

    // ========== ... ==========

    private void UpdateSelection(HeroSlotController controller, int slotIdx)
    {
        // 빈 곳 클릭 → 선택 해제
        if (controller == null)
        {
            ClearSelection();
            return;
        }

        // 같은 영웅 재클릭 → 토글 해제
        if (controller == _selectedController && slotIdx == _selectedSlotIdx)
        {
            ClearSelection();
            return;
        }

        // 이전 선택 해제
        if (_selectedController != null)
        {
            _selectedController.SetPinnedIdx(-1);
            var selectedHeroView = _selectedController.GetViewAt(_selectedSlotIdx);
            selectedHeroView?.SetSelected(false);
            // selectedHeroView?.SetLevelUpActive(false);
        }

        _selectedController = controller;
        _selectedSlotIdx = slotIdx;

        var currHeroView = controller.GetViewAt(slotIdx);
        var currHero = currHeroView?.HeroInstance;
        controller.SetPinnedIdx(slotIdx);
        currHeroView?.SetSelected(true);
        // currHeroView?.SetLevelUpActive(currHero != null && currHero.CanLevelUp());
        _onHeroSelected?.Invoke(currHero, false, controller.GetItemWorldPosAt(slotIdx));
    }

    private void ClearSelection()
    {
        if (_selectedController == null) return;

        _selectedController.SetPinnedIdx(-1);
        var selectedHeroView = _selectedController.GetViewAt(_selectedSlotIdx);
        selectedHeroView?.SetSelected(false);
        // selectedHeroView?.SetLevelUpActive(false);

        _selectedController = null;
        _selectedSlotIdx = -1;
        _onHeroDeselected?.Invoke();
    }

    // ========== ... ==========

    private void BeginDrag()
    {
        var worldPos = _inputProvider.GetWorldPos.ToVector3();
        var controllers = new[] { _summonSlotController, _squadSlotController };

        foreach (var controller in controllers)
        {
            if (controller == null) continue;

            var slotIdx = controller.GetSlotIndexAtWorldPos(worldPos);
            if (slotIdx < 0 || controller.IsSlotEmpty(slotIdx)) continue;

            _lastController = controller;
            _lastSlotIdx = slotIdx;
            _lastHeroView = controller.GetViewAt(slotIdx);
            _dragOriginalPos = _lastHeroView.transform.position;
            _dragOffset = _lastHeroView.transform.position - _lastHeroView.DragPoint.position;

            break;
        }
    }

    private void UpdateDrag()
    {
        if (_lastController == null) return;
        if (_lastHeroView == null) return;

        var worldPos = _inputProvider.GetWorldPos.ToVector3();

        if (!_isDragActive)
        {
            var currSlotIdx = _lastController.GetSlotIndexAtWorldPos(worldPos);
            var isStillInSlot = currSlotIdx == _lastSlotIdx;

            if (isStillInSlot) return;

            ClearSelection();

            _isDragActive = true;
            _lastHeroView.SetDragging(true);
            _onHeroSelected?.Invoke(_lastHeroView.HeroInstance, true, _lastController.GetItemWorldPosAt(_lastSlotIdx));
        }

        _lastHeroView.transform.position = worldPos + _dragOffset;

        // 현재 커서 위치가 슬롯 위에 있고 그 슬롯이 차 있으면 밀기 프리뷰 갱신
        var hoveredSquadSlot = _squadSlotController?.GetSlotIndexAtWorldPos(worldPos) ?? -1;
        var hoveredSummonSlot = _summonSlotController?.GetSlotIndexAtWorldPos(worldPos) ?? -1;
        if (!TryUpdatePushPreview(hoveredSquadSlot, _squadSlotController, _squadBenchManager.Bench) &&
            !TryUpdatePushPreview(hoveredSummonSlot, _summonSlotController, _summonBenchManager.Bench))
        {
            RevertPushPreview();
        }
    }

    private void EndDrag()
    {
        ClearDragHoverColor();
        RevertPushPreview();

        _summonSlotController?.ClearHover();
        _squadSlotController?.ClearHover();

        if (_lastHeroView != null && _isDragActive)
        {
            _lastHeroView.SetDragging(false);

            var worldPos = _inputProvider.GetWorldPos.ToVector3();
            var transferred = _lastController == _summonSlotController
                ? OnSummonHeroDropped(_lastSlotIdx, worldPos)
                : OnSquadHeroDropped(_lastSlotIdx, worldPos);

            var heroView = _lastHeroView;

            if (transferred)
            {
                heroView = _pendingDropView;
                _pendingDropView = null;
                SuppressHoverPanel = true;
                SoundManager.Instance.PlaySfx(SfxType.UI_Drop, 0.3f);
            }
            else
            {
                heroView.transform.position = _dragOriginalPos;
                SoundManager.Instance.PlaySfx(SfxType.UI_Fail1);
            }

            ClearSelection();
            heroView?.PlayDropAnimation();
            _onHeroDeselected?.Invoke();
        }
        else
        {
            UpdateSelection(_lastController, _lastSlotIdx);
        }

        _isDragActive = false;
        _lastHeroView = null;
        _lastSlotIdx = -1;
        _lastController = null;
    }

    // ========== ... ==========

    private bool OnSummonHeroDropped(int srcIdx, Vector3 worldPos) =>
        OnHeroDropped(srcIdx, worldPos, fromSummon: true,
            _summonBenchManager, _summonSlotController,
            _squadBenchManager, _squadSlotController);

    private bool OnSquadHeroDropped(int srcIdx, Vector3 worldPos) =>
        OnHeroDropped(srcIdx, worldPos, fromSummon: false,
            _squadBenchManager, _squadSlotController,
            _summonBenchManager, _summonSlotController);

    private bool OnHeroDropped(
        int srcIdx, Vector3 worldPos, bool fromSummon,
        HeroBenchManagerBase srcManager, HeroSlotController srcController,
        HeroBenchManagerBase altManager, HeroSlotController altController)
    {
        if (TrySellHero(srcIdx, worldPos, fromSummon)) return true;

        var srcHero = srcManager.GetFromBench(srcIdx);
        if (srcController == null || srcHero == null) return false;

        var dstIdx = srcController.GetSlotIndexAtWorldPos(worldPos);
        var dstController = srcController;
        var dstManager = srcManager;
        if (dstIdx == -1)
        {
            dstIdx = altController?.GetSlotIndexAtWorldPos(worldPos) ?? -1;
            dstController = altController;
            dstManager = altManager;
        }
        if (dstIdx == -1) return false;

        if (dstController == srcController)
        {
            // 같은 벤치 내 이동
            if (srcController.IsSlotEmpty(dstIdx))
            {
                var moved = srcManager.Move(srcIdx, dstIdx);
                if (moved)
                {
                    _pendingDropView = srcController.GetViewAt(dstIdx);
                    // SoundManager.Instance.PlaySfx(SfxType.UI_Drop, 0.3f);
                }
                return moved;
            }

            var dstHero = srcManager.GetFromBench(dstIdx);
            if (dstHero == null) return false;

            if (srcIdx != dstIdx && dstHero.CanMergeWith(srcHero))
            {
                if (srcManager.TryMerge(srcHero, dstIdx))
                {
                    srcManager.TakeFromBench(srcIdx);
                    _pendingDropView = srcController.GetViewAt(dstIdx);
                    _pendingDropView?.Refresh();
                    return true;
                }
            }

            var pushDir = GetPushDir(dstIdx, srcController, srcIdx);
            var pushed = srcManager.MoveWithPush(srcIdx, dstIdx, pushDir);
            if (pushed)
            {
                _pendingDropView = srcController.GetViewAt(dstIdx);
                // SoundManager.Instance.PlaySfx(SfxType.UI_Drop, 0.3f);
            }
            return pushed;
        }
        else
        {
            // 다른 벤치로 이동: 꺼내기 전에 가능 여부를 먼저 확인해야 함
            if (dstController.IsSlotEmpty(dstIdx))
            {
                var takenToAdd = srcManager.TakeFromBench(srcIdx);
                if (takenToAdd == null) return false;
                var added = dstManager.AddToBench(takenToAdd, dstIdx);
                if (added)
                {
                    _pendingDropView = dstController.GetViewAt(dstIdx);
                    // SoundManager.Instance.PlaySfx(SfxType.UI_Drop, 0.3f);
                }
                return added;
            }

            var dstHero = dstManager.GetFromBench(dstIdx);
            if (dstHero == null) return false;

            if (dstHero.CanMergeWith(srcHero))
            {
                if (dstManager.TryMerge(srcHero, dstIdx))
                {
                    srcManager.TakeFromBench(srcIdx);
                    _pendingDropView = dstController.GetViewAt(dstIdx);
                    _pendingDropView?.Refresh();
                    return true;
                }
            }

            var pushDir = GetPushDir(dstIdx, dstController);
            if (!dstManager.CanInsert(dstIdx, pushDir))
            {
                // 밀 공간 없으면 두 벤치 간 자리 교환
                var takenSrc = srcManager.TakeFromBench(srcIdx);
                if (takenSrc == null) return false;
                var takenDst = dstManager.TakeFromBench(dstIdx);
                if (takenDst == null) { srcManager.AddToBench(takenSrc, srcIdx); return false; }
                srcManager.AddToBench(takenDst, srcIdx);
                dstManager.AddToBench(takenSrc, dstIdx);
                _pendingDropView = dstController.GetViewAt(dstIdx);
                // SoundManager.Instance.PlaySfx(SfxType.UI_Drop, 0.3f);
                return true;
            }

            var takenToInsert = srcManager.TakeFromBench(srcIdx);
            if (takenToInsert == null) return false;

            dstManager.Insert(takenToInsert, dstIdx, pushDir);
            _pendingDropView = dstController.GetViewAt(dstIdx);
            // SoundManager.Instance.PlaySfx(SfxType.UI_Drop, 0.3f);
            return true;
        }
    }

    // ========== ... ==========

    private bool TryUpdatePushPreview(int hoveredSlot, HeroSlotController controller, HeroInstance[] bench)
    {
        if (hoveredSlot == -1 || controller.IsSlotEmpty(hoveredSlot)) return false;

        var dstHero = bench[hoveredSlot];

        if (dstHero.CanMergeWith(_lastHeroView.HeroInstance))
        {
            RevertPushPreview();
            return true;
        }

        // 동일 컨트롤러 내부 이동일 때는 드래그 출발 슬롯을 빈 자리로 간주해야 체인 계산이 올바름
        int excludeSrcIdx = _lastController == controller ? _lastSlotIdx : -1;
        int pushDir = GetPushDir(hoveredSlot, controller, excludeSrcIdx);

        // 슬롯이나 방향이 바뀐 경우에만 프리뷰 갱신 (매 프레임 재계산 방지)
        if (hoveredSlot != _pushPreviewSlot || pushDir != _pushPreviewDir)
            ApplyPushPreview(hoveredSlot, pushDir, controller, excludeSrcIdx);

        return true;
    }

    private int GetPushDir(int targetSlot, HeroSlotController controller, int excludeSrc = -1)
    {
        var heroPosX = _lastHeroView.transform.position.x;
        var slotPosX = controller.GetSlotWorldPos(targetSlot).x;
        int preferredDir = heroPosX > slotPosX ? 1 : -1;

        // 진입 방향으로 밀 공간이 없으면 반대 방향으로 폴백
        if (CalcPushChain(targetSlot, preferredDir, controller, excludeSrc) == null &&
            CalcPushChain(targetSlot, (-1) * preferredDir, controller, excludeSrc) != null)
        {
            return (-1) * preferredDir;
        }

        return preferredDir;
    }

    private List<(int from, int to)> CalcPushChain(int targetSlot, int pushDir, HeroSlotController controller, int excludeSrc = -1)
    {
        var chain = new List<(int, int)>();
        var curr = targetSlot;

        while (curr >= 0 && curr < controller.SlotCount)
        {
            if (curr == excludeSrc || controller.IsSlotEmpty(curr)) break;
            var next = curr + pushDir;
            if (next < 0 || next >= controller.SlotCount) return null; // 경계 초과 → 불가
            chain.Add((curr, next));
            curr = next;
        }

        return chain;
    }

    private void ApplyPushPreview(int targetSlot, int pushDir, HeroSlotController controller, int excludeSrc = -1)
    {
        RevertPushPreview();

        var chain = CalcPushChain(targetSlot, pushDir, controller, excludeSrc);
        if (chain == null) return;

        _pushPreviewSlot = targetSlot;
        _pushPreviewDir = pushDir;
        _pushPreviewChain = chain;
        _pushPreviewController = controller;

        foreach (var (from, to) in chain)
        {
            var fromHeroView = controller.GetViewAt(from);
            fromHeroView.transform.DOKill();
            fromHeroView.transform.DOMove(controller.GetItemWorldPosAt(to), 0.12f).SetEase(Ease.OutCubic);
        }
    }

    private void RevertPushPreview()
    {
        if (_pushPreviewChain == null) return;

        foreach (var (from, to) in _pushPreviewChain)
        {
            var fromHeroView = _pushPreviewController.GetViewAt(from);
            fromHeroView.transform.DOKill();
            fromHeroView.transform.DOMove(_pushPreviewController.GetItemWorldPosAt(from), 0.12f).SetEase(Ease.OutCubic);
        }

        _pushPreviewChain = null;
        _pushPreviewSlot = -1;
        _pushPreviewController = null;
    }

    // ========== ... ==========

    private bool TrySellHero(int srcIdx, Vector3 worldPos, bool fromSummon)
    {
        if (_heroSellZone == null || !_heroSellZone.ContainsWorldPos(worldPos)) return false;

        var heroInstance = fromSummon
            ? _summonBenchManager.TakeFromBench(srcIdx)
            : _squadBenchManager.TakeFromBench(srcIdx);
        if (heroInstance == null) return false;

        var slotController = fromSummon ? _summonSlotController : _squadSlotController;
        var slotPos = slotController.GetItemWorldPosAt(srcIdx);
        EffectManager.Instance.Play(VfxType.Sell, slotPos);
        SoundManager.Instance.PlaySfx(SfxType.Sell);

        _onHeroSold?.Invoke(heroInstance, slotPos);
        return true;
    }
}
