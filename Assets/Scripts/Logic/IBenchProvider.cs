using System;

public interface IBenchProvider<T> where T : class
{
    T[] Bench { get; }

    event Action OnBenchChanged;

    bool IsBenchFull();
    bool AddToBench(T item, int idx);
    T TakeFromBench(int idx);
}
