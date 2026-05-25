using System;
using UnityEngine;

public enum SummonResult { Success, BenchFull, InsufficientGold }

[Serializable]
public class SummonBenchManager : HeroBenchManagerBase
{
    private HeroDatabase _heroDatabase;
    private SummonConfig _summonConfig;
    private SummonTable _summonTable;

    private Func<int, bool> _trySpendGold;
    private Action<int, int> _onSummonCostChanged;
    private Action<HeroInstance> _onHeroSummoned;

    private int _currCost;
    public int CurrCost => _currCost;

    public SummonBenchManager(HeroDatabase heroDatabase, SummonConfig summonConfig, SummonTable summonTable)
        : base(summonConfig.capacity)
    {
        _heroDatabase = heroDatabase;
        _summonConfig = summonConfig;
        _summonTable = summonTable;

        _currCost = _summonConfig.baseCost;
    }

    public void Initialize(Func<int, bool> trySpendGold, Action<int, int> onSummonCostChanged, Action<HeroInstance> onHeroSummoned)
    {
        _trySpendGold = trySpendGold;
        _onSummonCostChanged = onSummonCostChanged;
        _onHeroSummoned = onHeroSummoned;
    }

    // === 소환 ===

    public SummonResult TrySummon(int currRound)
    {
        if (IsBenchFull()) return SummonResult.BenchFull;
        if (!_trySpendGold(_currCost)) return SummonResult.InsufficientGold;

        var grade = PickGrade(currRound);
        var heroData = PickHero(grade);
        if (heroData == null) return SummonResult.InsufficientGold;

        var heroInstance = new HeroInstance(heroData);
        AddToBench(heroInstance, -1);
        var prevCost = _currCost;
        var currCost = prevCost + _summonConfig.costIncrement;
        _currCost = currCost;
        _onSummonCostChanged?.Invoke(prevCost, currCost);
        _onHeroSummoned?.Invoke(heroInstance);

        return SummonResult.Success;
    }

    private HeroGrade PickGrade(int currRound)
    {
        var gradeChanceEntries = _summonTable.GetGradeChanceEntries(currRound);
        var randomChance = UnityEngine.Random.Range(0f, 1f);

        var cumulativeChance = 0f;

        foreach (var gradeChanceEntry in gradeChanceEntries)
        {
            cumulativeChance += gradeChanceEntry.chance;

            if (randomChance <= cumulativeChance)
            {
                return gradeChanceEntry.grade;
            }
        }

        return gradeChanceEntries[gradeChanceEntries.Length - 1].grade;
    }

    private HeroData PickHero(HeroGrade grade)
    {
        var pool = _heroDatabase.GetHeroesByGrade(grade);
        if (pool == null || pool.Count == 0) return null;

        int idx = UnityEngine.Random.Range(0, pool.Count);
        return pool[idx];
    }

    public void ResetCost()
    {
        var prevCost = _currCost;
        var currCost = _summonConfig.baseCost;
        _currCost = currCost;
        _onSummonCostChanged?.Invoke(prevCost, currCost);
    }

    // === 관리 ===

    public override bool AddToBench(HeroInstance heroInstance, int idx)
    {
        if (idx >= 0 && idx < bench.Length)
        {
            if (bench[idx] == null)
            {
                bench[idx] = heroInstance;
                NotifyChanged();
                return true;
            }
        }

        for (int i = 0; i < bench.Length; i++)
        {
            if (bench[i] == null)
            {
                bench[i] = heroInstance;
                NotifyChanged();
                return true;
            }
        }

        return false;
    }
}
