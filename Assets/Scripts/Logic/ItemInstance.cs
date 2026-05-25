public abstract class ItemInstance
{
    public bool HasBeenSpawned { get; set; }
}

public abstract class ItemInstance<TData> : ItemInstance where TData : ItemData
{
    protected ItemInstance(TData data)
    {
        Data = data;
    }

    public TData Data { get; private set; }
}
