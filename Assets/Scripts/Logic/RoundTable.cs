using System;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyHeroEntry
{
    public HeroData data;
    public int level;
}

[System.Serializable]
public class RoundData
{
    public List<EnemyHeroEntry> enemyHeroes;
}

[CreateAssetMenu(fileName = "RoundTable", menuName = "Lucky/RoundTable")]
public class RoundTable : ScriptableObject
{
    [SerializeField] private RoundData[] _rounds;

    public int RoundCount => _rounds.Length;

    public RoundData GetRoundData(int round)
    {
        int min = 0;
        int max = _rounds.Length - 1;
        int index = Mathf.Clamp(round - 1, min, max);

        return _rounds[index];
    }
}
