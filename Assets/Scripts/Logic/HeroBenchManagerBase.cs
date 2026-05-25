using System;

[Serializable]
public abstract class HeroBenchManagerBase : BenchManagerBase<HeroInstance>
{
    private Action<int, bool> _onMerge; // dstIdx, isLevelUp

    protected HeroBenchManagerBase(int capacity) : base(capacity) { }

    public void Initialize(Action<int, bool> onMerge)
    {
        _onMerge = onMerge;
    }

    public bool TryMerge(HeroInstance srcHero, int dstIdx)
    {
        if (srcHero == null) return false;
        if (dstIdx < 0 || dstIdx >= bench.Length) return false;

        var dstHero = bench[dstIdx];
        if (!dstHero.CanMergeWith(srcHero)) return false;

        // src 영웅이 Lv1 단위로 몇 카피를 쌓았는가"를 선형으로 변환하는 공식
        var srcStack = (srcHero.Level - 1) * srcHero.Data.MaxStack + srcHero.Stack + 1;
        var prevLevel = dstHero.Level;
        dstHero.AbsorbStack(srcStack);
        var isLevelUp = dstHero.Level > prevLevel && dstHero.Level > srcHero.Level;

        _onMerge?.Invoke(dstIdx, isLevelUp);
        return true;
    }
}
