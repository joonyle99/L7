using UnityEngine;

public abstract class ItemSlot
{
    public Transform Root;
    public SpriteRenderer Frame;
    public ItemSlotPoint Point;
    public ItemView View;
    public bool IsPendingSpawn;

    public void SetActiveFrame(bool active) => Frame?.gameObject.SetActive(active);
    public void SetActivePoint(bool active, bool highlight = false) => Point?.SetActive(active, highlight);
    public void SetPointColor(Color? color = null) => Point?.SetColor(color);
}
