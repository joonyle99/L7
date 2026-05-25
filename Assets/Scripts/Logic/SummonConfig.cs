using UnityEngine;

[CreateAssetMenu(fileName = "SummonConfig", menuName = "Lucky/SummonConfig")]
public class SummonConfig : ScriptableObject
{
    public int capacity = 5;
    public int baseCost = 3;
    public int costIncrement = 1;
}
