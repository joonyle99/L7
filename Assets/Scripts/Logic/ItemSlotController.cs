using TMPro;
using UnityEngine;

public abstract class ItemSlotController : MonoBehaviour
{
    [SerializeField] private GameObject _slotPrefab;
    [SerializeField] private Vector2 _slotBoxSize = Vector2.one;
    [SerializeField] private Vector2 _slotBoxOffset = Vector2.zero;

    protected ItemSlot[] slots;
    public int SlotCount => slots.Length;

    private Vector2 _slotBoxHalfSize;

    protected void InitializeSlots(int capacity)
    {
        slots = new ItemSlot[capacity];
        _slotBoxHalfSize = _slotBoxSize * 0.5f;

        for (int idx = 0; idx < capacity; idx++)
        {
            var slot = CreateSlot();
            slot.Root = Instantiate(_slotPrefab, transform).transform;
            slot.Frame = slot.Root.Find("Frame")?.GetComponentInChildren<SpriteRenderer>(true);
            slot.Point = slot.Root.Find("Point")?.GetComponentInChildren<ItemSlotPoint>(true);
            slots[idx] = slot;

#if UNITY_EDITOR
            var label = slot.Root.Find("Label")?.GetComponentInChildren<TextMeshPro>(true);
            if (label != null)
            {
                // label.gameObject.SetActive(true);
                label.text = idx.ToString();
            }
#endif
        }

        if (TryGetComponent<SlotLayout>(out var slotLayout))
        {
            slotLayout.Arrange();
        }
    }

    // ========== ... ==========

    protected abstract ItemSlot CreateSlot();
    protected abstract void Refresh();

    // ========== ... ==========

    public Transform GetSlotRoot(int idx) => slots[idx].Root;
    public Vector3 GetSlotWorldPos(int idx) => GetSlotRoot(idx).position;
    public int GetSlotIndexAtWorldPos(Vector3 worldPos)
    {
        for (int idx = 0; idx < slots.Length; idx++)
        {
            var diffVector = (Vector2)GetSlotWorldPos(idx) + _slotBoxOffset - (Vector2)worldPos;
            var withinX = Mathf.Abs(diffVector.x) <= _slotBoxHalfSize.x;
            var withinY = Mathf.Abs(diffVector.y) <= _slotBoxHalfSize.y;

            if (withinX && withinY)
            {
                return idx;
            }
        }

        return -1;
    }

    // ========== ... ==========

    private void OnDrawGizmos()
    {
        if (slots == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);

        foreach (var slot in slots)
        {
            if (slot != null)
            {
                var center = slot.Root.position + (Vector3)_slotBoxOffset;
                Gizmos.DrawWireCube(center, _slotBoxSize);
            }
        }
    }
}
