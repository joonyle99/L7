using UnityEngine;
using System.Collections.Generic;

public enum SlotAlignment { Center, Left, Right }

public class SlotLayout : MonoBehaviour
{
    [SerializeField] private float _spacing = 1.5f;
    [SerializeField] private SlotAlignment _alignment = SlotAlignment.Center;

#if UNITY_EDITOR
    [ContextMenu("Arrange")]
#endif

    public void Arrange()
    {
        var activeChildren = new List<Transform>();
        
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.gameObject.activeSelf) activeChildren.Add(child);
        }

        int count = activeChildren.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * _spacing;
        float startX = _alignment switch
        {
            SlotAlignment.Center => -totalWidth / 2f,
            SlotAlignment.Left   => 0f,
            SlotAlignment.Right  => -totalWidth,
            _                    => -totalWidth / 2f,
        };

        for (int i = 0; i < count; i++)
        {
            activeChildren[i].localPosition = new Vector3(startX + i * _spacing, activeChildren[i].localPosition.y, 0f);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Arrange();
    }
#endif
}