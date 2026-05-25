using System;
using UnityEngine;

public class GameStartEffect1 : MonoBehaviour, IAnimationEventHandler
{
    private Action _onComplete;

    public void Play(Action onComplete)
    {
        _onComplete = onComplete;
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
