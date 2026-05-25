using UnityEngine;

public class BattleKingController : MonoBehaviour
{
    [SerializeField] private Animator _leftKingAnimator;
    [SerializeField] private Animator _rightKingAnimator;

    private static readonly int CheerHash = Animator.StringToHash("Cheer");
    private static readonly int SurprisedHash = Animator.StringToHash("Surprised");

    public void Initialize() { }

    // playerDied: 이번 사이클에 아군 영웅이 사망했는지
    // enemyDied:  이번 사이클에 적군 영웅이 사망했는지
    public void OnCycleDeaths(bool playerDied, bool enemyDied)
    {
        if (!playerDied && !enemyDied) return;

        if (playerDied && enemyDied)
        {
            // 동시 사망: 양쪽 왕 모두 놀람
            _leftKingAnimator.Play(SurprisedHash);
            _rightKingAnimator.Play(SurprisedHash);
        }
        else if (playerDied)
        {
            _leftKingAnimator.Play(SurprisedHash);
            _rightKingAnimator.Play(CheerHash);
        }
        else
        {
            _leftKingAnimator.Play(CheerHash);
            _rightKingAnimator.Play(SurprisedHash);
        }
    }
}
