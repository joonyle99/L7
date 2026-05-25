using UnityEngine;

public class RelicView : ItemView
{
    private RelicInstance _relicInstance;
    public RelicInstance RelicInstance => _relicInstance;
    public override ItemInstance ItemInstance => _relicInstance;

    public void Setup(RelicInstance relicInstance, Vector3 spawnOffset)
    {
        _relicInstance = relicInstance;
        ApplyItem(relicInstance.Data.Prefab, spawnOffset);
    }

    public void Clear()
    {
        ClearItem();
        _relicInstance = null;
    }
}
