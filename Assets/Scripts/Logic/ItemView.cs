using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class ItemView : MonoBehaviour
{
    [SerializeField] private Transform _itemRoot;
    [SerializeField] private SortingGroup _sortingGroup;
    [SerializeField] private Transform _dragPoint;
    public Transform DragPoint => _dragPoint ?? transform;
    [SerializeField, SortingLayer] private int _dragSortingLayer;
    [SerializeField] private int _dragSortingOrder;
    
    protected Transform[] itemSlots;
    private GameObject[] _prefabObjects;
    private int _defaultSortingLayerID;
    private int _defaultSortingOrder;
    private Vector3 _originItemScale;
    private Vector3 _spawnOffset;
    private bool _animLocked;

    public abstract ItemInstance ItemInstance { get; }

    protected virtual void Awake()
    {
        _defaultSortingLayerID = _sortingGroup.sortingLayerID;
        _defaultSortingOrder = _sortingGroup.sortingOrder;
        _originItemScale = _itemRoot.localScale;

        var childCount = _itemRoot.childCount;
        itemSlots = childCount > 0 ? new Transform[childCount] : new Transform[] { _itemRoot };
        for (int i = 0; i < childCount; i++) itemSlots[i] = _itemRoot.GetChild(i);
        _prefabObjects = new GameObject[itemSlots.Length];
    }

    // ======== ... ========

    public void SetSelected(bool isSelected)
    {
        if (isSelected) PlaySelectAnimation();
    }

    public void SetFlipX(bool flip)
    {
        _itemRoot.localScale = new Vector3(
            flip ? -_originItemScale.x : _originItemScale.x,
            _originItemScale.y,
            _originItemScale.z);
    }

    public virtual void SetDragging(bool isDragging)
    {
        _sortingGroup.sortingLayerID = isDragging ? _dragSortingLayer : _defaultSortingLayerID;
        _sortingGroup.sortingOrder   = isDragging ? _dragSortingOrder : _defaultSortingOrder;
    }

    // ======== ... ========

    protected bool IsSlotOccupied(int slotIdx) => _prefabObjects != null && slotIdx >= 0 && slotIdx < _prefabObjects.Length && _prefabObjects[slotIdx] != null;

    protected virtual void OnPrefabInstantiated(int slotIdx, GameObject prefabObject) { }

    protected void ApplyItem(GameObject prefab, Vector3 spawnOffset)
    {
        _spawnOffset = spawnOffset;
        ApplyItemToSlot(prefab, 0);
    }

    protected void ApplyItemToSlot(GameObject prefab, int slotIdx)
    {
        if (_prefabObjects == null || slotIdx < 0 || slotIdx >= _prefabObjects.Length) return;

        if (_prefabObjects[slotIdx] != null)
        {
            Destroy(_prefabObjects[slotIdx]);
            _prefabObjects[slotIdx] = null;
        }

        if (prefab == null) return;

        var prefabObject = Instantiate(prefab, itemSlots[slotIdx]);
        prefabObject.transform.localPosition = _spawnOffset;
        prefabObject.transform.localRotation = Quaternion.identity;
        _prefabObjects[slotIdx] = prefabObject;

        OnPrefabInstantiated(slotIdx, prefabObject);
    }

    protected void ClearItem()
    {
        if (_prefabObjects == null) return;
        
        for (int i = 0; i < _prefabObjects.Length; i++)
        {
            if (_prefabObjects[i] != null)
            {
                Destroy(_prefabObjects[i]);
                _prefabObjects[i] = null;
            }
        }
    }

    protected void ClearItem(int slotIdx)
    {
        if (_prefabObjects == null || slotIdx < 0 || slotIdx >= _prefabObjects.Length) return;
        if (_prefabObjects[slotIdx] != null)
        {
            Destroy(_prefabObjects[slotIdx]);
            _prefabObjects[slotIdx] = null;
            OnSlotCleared(slotIdx);
        }
    }

    protected virtual void OnSlotCleared(int slotIdx) { }

    protected virtual void OnDestroy()
    {
        transform.DOKill();
        _itemRoot?.DOKill();
    }

    // ======== ... ========

    public void PlayHoverAnimation()
    {
        if (_animLocked) return;

        _itemRoot.DOKill();
        _itemRoot.localScale = _originItemScale;
        _itemRoot.DOPunchScale(_originItemScale * 0.05f, 0.08f, 1, 0f);
    }

    public void PlaySelectAnimation()
    {
        _animLocked = true;

        _itemRoot.DOKill();
        _itemRoot.localScale = _originItemScale;
        _itemRoot.DOPunchScale(_originItemScale * 0.15f, 0.13f, 10, 0.8f).OnComplete(() => _animLocked = false);
    }

    public void PlayDropAnimation()
    {
        _animLocked = true;

        _itemRoot.DOKill();
        _itemRoot.localScale = _originItemScale;
        _itemRoot.DOPunchScale(_originItemScale * 0.35f, 0.45f, 6, 0.3f).OnComplete(() => _animLocked = false);
    }
}
