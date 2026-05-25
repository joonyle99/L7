using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    public Sprite Icon;
    public string Name;
    public GameObject Prefab;
    public Vector2 IconAnchoredPos;
}
