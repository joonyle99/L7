using System;
using UnityEngine;
using JoonyleGameDevKit;

public sealed class RelicSlotController : BenchSlotController<RelicInstance, RelicView, RelicSlot>
{
    public void Initialize(
        IBenchProvider<RelicInstance> benchProvider,
        Func<RelicInstance, Vector3, Action, bool> onSpawnEffect = null,
        Action<RelicInstance, Vector3> onHoverEnter = null,
        Action onHoverExit = null)
    {
        InitializeBench(benchProvider, onSpawnEffect, onHoverEnter, onHoverExit);
    }

    protected override ItemSlot CreateSlot() => new RelicSlot();
    protected override void SetupView(RelicSlot slot, RelicInstance instance)
    {
        slot.RelicView.Setup(instance, itemOffset);
        slot.RelicView.transform.localPosition = itemOffset.ToVector3();
    }
}
