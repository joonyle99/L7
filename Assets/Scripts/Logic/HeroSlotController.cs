using System;
using UnityEngine;
using JoonyleGameDevKit;

public sealed class HeroSlotController : BenchSlotController<HeroInstance, HeroView, HeroSlot>
{
    private GradeConfig _gradeConfig;

    public void Initialize(
        IBenchProvider<HeroInstance> benchProvider,
        GradeConfig gradeConfig = null,
        Func<HeroInstance, Vector3, Action, bool> onSpawnEffect = null,
        Action<HeroInstance, Vector3> onHoverEnter = null,
        Action onHoverExit = null)
    {
        _gradeConfig = gradeConfig;
        InitializeBench(benchProvider, onSpawnEffect, onHoverEnter, onHoverExit);
    }

    protected override ItemSlot CreateSlot() => new HeroSlot();
    protected override void SetupView(HeroSlot slot, HeroInstance instance)
    {
        slot.HeroView.Setup(instance, itemOffset, _gradeConfig);
        slot.HeroView.transform.localPosition = itemOffset.ToVector3();
    }

    public void SetActivePoints(bool active, HeroInstance hero)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var instance = IsSlotEmpty(i) ? null : GetInstanceAt(i);
            var highlight = active && hero != null && instance != null && instance.Data == hero.Data;
            slots[i].SetActivePoint(active, highlight);
        }
    }
}
