using TMPro;
using UnityEngine;

public class HeroSellZone : MonoBehaviour
{
    [SerializeField] private TextMeshPro _price;
    
    [Space]
    
    [SerializeField] private Vector2 _size = Vector2.one;
    [SerializeField] private Vector2 _offset = Vector2.zero;
    private Vector2 _halfSize;

    public void Initialize()
    {
        _halfSize = _size * 0.5f;
    }

    public void SetPriceText(int price)
    {
        _price.text = $"끌어서 판매 +{price}";
    }

    public bool ContainsWorldPos(Vector3 worldPos)
    {
        var diffVector = (Vector2)transform.position + _offset - (Vector2)worldPos;
        var withinX = Mathf.Abs(diffVector.x) <= _halfSize.x;
        var withinY = Mathf.Abs(diffVector.y) <= _halfSize.y;
        
        return withinX && withinY;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireCube((Vector3)((Vector2)transform.position + _offset), _size);
    }
}
