using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RelicDatabase", menuName = "Lucky/RelicDatabase")]
public class RelicDatabase : ScriptableObject
{
    [SerializeField] private List<RelicData> _relics;

    public List<RelicData> GetAllRelics() => _relics;
}
