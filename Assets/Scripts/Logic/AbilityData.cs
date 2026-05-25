using UnityEngine;

public enum AbilityTrigger
{
    OnSummon,
    OnSold,
    OnBattleStart,
    OnMove,
    OnAttack,
    OnPrevAllyHit,
    OnPrevAllyOnlyHit,
    OnAllyHit,
    OnHit,
    OnAllyDeath,
    OnEnemyDeath,
    OnDeath,
    OnCycleEnd,
}

public enum AbilityEffect
{
    IncreaseAttack,
    DecreaseAttack,
    IncreaseHealth,
    IncreaseAttackHealth,
    DealDamage,
    BonusAttack,
    Smash,
    Faint,
    Push,
    Pull,
    Summon,
    EarnGold,
}

public enum AbilityTarget
{
    Self,
    AllAllies,
    RandomAlly,
    RandomAlly2,
    PrevAlly,
    NextAlly,
    FrontAlly,
    BackAlly,
    HighestHpAlly,
    LowestHpAlly,
    

    AllEnemies,
    RandomEnemy,
    RandomEnemy2,
    FrontEnemy,
    BackEnemy,
    HighestHpEnemy,
    LowestHpEnemy,

    AllHeroes,
}

[System.Serializable]
public struct AbilityData
{
    public AbilityTrigger Trigger; // 트리거 조건
    public AbilityEffect Effect; // 능력 효과
    public AbilityTarget Target; // 효과 대상
    public int[] Values; // 레벨별 효과 수치 (index 0 = Lv1, 1 = Lv2, 2 = Lv3)
    public float Probability; // 발동 확률 (0 ~ 100)
    public string FlavorText; // 부연 설명

    public int GetValue(int level)
    {
        if (Values == null || Values.Length == 0) return 0;
        return Values[Mathf.Clamp(level - 1, 0, Values.Length - 1)];
    }
}
