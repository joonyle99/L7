using System;

public class GoldSystem
{
    private int _gold;
    public int Gold => _gold;

    private event Action<int> _onGoldChanged;

    public GoldSystem(int startGold)
    {
        _gold = startGold;
    }

    public void Initialize(Action<int> onGoldChanged)
    {
        _onGoldChanged += onGoldChanged;
        _onGoldChanged?.Invoke(_gold);
    }

    public bool CanSpend(int amount) => _gold >= amount;

    public bool TrySpend(int amount)
    {
        if (!CanSpend(amount))
        {
            return false;
        }

        _gold -= amount;
        _onGoldChanged?.Invoke(_gold);

        return true;
    }

    public void AddGold(int amount)
    {
        _gold += amount;
        _onGoldChanged?.Invoke(_gold);
    }

    public void SetGold(int amount)
    {
        _gold = amount;
        _onGoldChanged?.Invoke(_gold);
    }
}
