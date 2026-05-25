using System;
using UnityEngine;

public class RoundManager
{
    private int _currRound;
    public int CurrRound => _currRound;

    private RoundTable _roundTable;

    private event Action<int> _onRoundChanged;

    public RoundManager(RoundTable roundTable)
    {
        _roundTable = roundTable;
        _currRound = 1;
    }
    
    public void Initialize(Action<int> onRoundChanged)
    {
        _onRoundChanged += onRoundChanged;
        _onRoundChanged?.Invoke(_currRound);
    }

    public void NextRound()
    {
        _currRound++;
        _onRoundChanged?.Invoke(_currRound);
    }

    public RoundData GetCurrRoundData()
    {
        return _roundTable.GetRoundData(_currRound);
    }

    public void Reset()
    {
        _currRound = 0;
    }
}
