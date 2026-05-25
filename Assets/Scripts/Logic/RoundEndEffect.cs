using System;
using UnityEngine;

public class RoundEndEffect : MonoBehaviour, IAnimationEventHandler
{
    [SerializeField] protected Animator _animator;
    private Action _onComplete;

    private static readonly int WinHash  = Animator.StringToHash("Win");
    private static readonly int LoseHash = Animator.StringToHash("Lose");
    private static readonly int DrawHash = Animator.StringToHash("Draw");

    public void Play(RoundOutcome outcome, Action onComplete)
    {
        _onComplete = onComplete;

        var hash = outcome switch
        {
            RoundOutcome.Player_Win => WinHash,
            RoundOutcome.Enemy_Win  => LoseHash,
            RoundOutcome.Draw       => DrawHash,
            _                       => WinHash
        };

        _animator.Play(hash);
    }

    public void OnAnimationEvent(string eventName)
    {
        if (eventName == "End")
        {
            _onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
