using System;

[Serializable]
public abstract class BenchManagerBase<T> : IBenchProvider<T> where T : class
{
    protected T[] bench;
    public T[] Bench => bench;
    public event Action OnBenchChanged;

    protected BenchManagerBase(int capacity)
    {
        bench = new T[capacity];
    }

    public abstract bool AddToBench(T item, int idx);

    public T TakeFromBench(int idx)
    {
        if (idx < 0 || idx >= bench.Length) return null;

        var item = bench[idx];
        if (item == null) return null;

        bench[idx] = null;
        OnBenchChanged?.Invoke();

        return item;
    }

    public void ClearBench()
    {
        for (int i = 0; i < bench.Length; i++)
        {
            bench[i] = null;
        }

        NotifyChanged();
    }

    public T GetFromBench(int idx)
    {
        if (idx < 0 || idx >= bench.Length) return null;
        return bench[idx];
    }

    public bool IsBenchFull()
    {
        for (int i = 0; i < bench.Length; i++)
        {
            if (bench[i] == null) return false;
        }
        return true;
    }

    public bool Move(int srcIdx, int dstIdx)
    {
        if (srcIdx < 0 || srcIdx >= bench.Length) return false;
        if (dstIdx < 0 || dstIdx >= bench.Length) return false;
        if (srcIdx == dstIdx) return false;
        if (bench[srcIdx] == null) return false;

        (bench[dstIdx], bench[srcIdx]) = (bench[srcIdx], bench[dstIdx]);
        NotifyChanged();

        return true;
    }

    public bool Insert(T item, int targetIdx, int pushDir)
    {
        var emptyIdx = FindEmptyInDir(targetIdx, pushDir);
        if (emptyIdx == -1) return false;

        ShiftRange(targetIdx, emptyIdx, pushDir);

        bench[targetIdx] = item;
        NotifyChanged();

        return true;
    }

    public bool MoveWithPush(int srcIdx, int dstIdx, int pushDir)
    {
        if (srcIdx == dstIdx) return false;

        var item = bench[srcIdx];
        bench[srcIdx] = null;

        if (!Insert(item, dstIdx, pushDir))
        {
            bench[srcIdx] = item;
            return false;
        }

        return true;
    }

    public bool CanInsert(int targetIdx, int pushDir) => FindEmptyInDir(targetIdx, pushDir) != -1;

    private int FindEmptyInDir(int startIdx, int pushDir)
    {
        for (int i = startIdx; i >= 0 && i < bench.Length; i += pushDir)
        {
            if (bench[i] == null) return i;
        }

        return -1;
    }

    private void ShiftRange(int fromIdx, int toIdx, int pushDir)
    {
        for (int i = toIdx; i != fromIdx; i -= pushDir)
        {
            bench[i] = bench[i - pushDir];
        }

        bench[fromIdx] = null;
    }

    protected void NotifyChanged() => OnBenchChanged?.Invoke();
}
