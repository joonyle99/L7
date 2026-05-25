using System;

public class RelicBenchManager : BenchManagerBase<RelicInstance>
{
    private readonly RelicDatabase _relicDatabase;
    private readonly RelicConfig _relicConfig;

    public RelicBenchManager(RelicDatabase relicDatabase, RelicConfig relicConfig)
        : base(relicConfig.capacity)
    {
        _relicDatabase = relicDatabase;
        _relicConfig = relicConfig;

        TryAddRelic(PickRelic());
        TryAddRelic(PickRelic());
    }

    // === 획득 ===

    public bool TryAddRelic(RelicData data)
    {
        if (data == null || IsBenchFull()) return false;

        var relicInstance = new RelicInstance(data);
        return AddToBench(relicInstance, -1);
    }

    private RelicData PickRelic()
    {
        var relics = _relicDatabase.GetAllRelics();
        if (relics == null || relics.Count == 0) return null;
        return relics[UnityEngine.Random.Range(0, relics.Count)];
    }

    // === 관리 ===

    public override bool AddToBench(RelicInstance item, int idx)
    {
        for (int i = 0; i < bench.Length; i++)
        {
            if (bench[i] == null)
            {
                bench[i] = item;
                NotifyChanged();

                return true;
            }
        }

        return false;
    }
}
