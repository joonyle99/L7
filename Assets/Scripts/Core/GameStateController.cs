using System;
using UnityEngine;

public enum OutGameState
{
    None,
    Title,
    OutGame
}

public enum InGameState
{
    None,
    Prepare, // 소환, 편성, 합성
    Battle, // 자동 전투
    RoundEnd, // 라운드 결과
    MatchEnd, // 매치 종료 (3승 클리어 or 목숨 소진)
}

public class GameStateController<T> where T : Enum
{
    private T currState;
    public T CurrState => currState;

    public event Action<T, T> OnStateChanged;

    public void ChangeState(T nextState)
    {
        if (currState.Equals(nextState)) return;

        var prevState = currState;
        ExitState(currState);
        currState = nextState;
        EnterState(currState);

        OnStateChanged?.Invoke(prevState, currState);
    }

    private void EnterState(T state)
    {
#if UNITY_EDITOR
        Debug.Log($"<color=yellow>Enter State - {state.ToString()}</color>");
#endif
    }

    private void ExitState(T state)
    {
#if UNITY_EDITOR
        Debug.Log($"<color=yellow>Exit State - {state.ToString()}</color>");
#endif
    }
}
