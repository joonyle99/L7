using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class SquadBenchManager : HeroBenchManagerBase
{
    private readonly SquadConfig _squadConfig;

    public SquadBenchManager(SquadConfig squadConfig)
        : base(squadConfig.capacity)
    {
        _squadConfig = squadConfig;
    }

    public void Initialize()
    {

    }

    // === 관리 ===

    public override bool AddToBench(HeroInstance heroInstance, int idx)
    {
        if (idx < 0 || idx >= bench.Length) return false;
        if (bench[idx] != null) return false;

        bench[idx] = heroInstance;
        NotifyChanged();
        return true;
    }
}
