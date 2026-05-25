using TMPro;
using UnityEngine;

public class BattleSlotController : MonoBehaviour
{
    [SerializeField] private HeroView _heroViewPrefab;
    [SerializeField] private GameObject _slotPrefab;
    
    private Transform[] _slots;
    public int SlotCount => _slots.Length;
    
    private GradeConfig _gradeConfig;

    public void Initialize(int capacity, GradeConfig gradeConfig = null)
    {
        _gradeConfig = gradeConfig;
        _slots = new Transform[capacity];

        for (int idx = 0; idx < capacity; idx++)
        {
            var slot = Instantiate(_slotPrefab, transform).transform;
            _slots[idx] = slot;

#if UNITY_EDITOR
            var label = slot.Find("Label")?.GetComponentInChildren<TextMeshPro>(true);
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

    public Transform GetSlot(int idx) => (idx >= 0 && idx < _slots.Length) ? _slots[idx] : null;

    // ========== ... ==========

    public HeroView SpawnHeroView(HeroInstance hero, int idx, bool flipX = false)
    {
        var slot = GetSlot(idx);
        var heroView = Instantiate(_heroViewPrefab, slot);
        heroView.Setup(hero, Vector3.zero, _gradeConfig);
        if (flipX) heroView.SetFlipX(true);
        return heroView;
    }
}
