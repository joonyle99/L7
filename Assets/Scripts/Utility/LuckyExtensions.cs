public static class LuckyExtensions
{
    public static string Sprite(string name) => $"<sprite name=\"{name}\">";


    public static string ToDisplayText(this HeroGrade grade) => grade switch
    {
        HeroGrade.Normal    => "일반",
        HeroGrade.Rare      => "레어",
        HeroGrade.Epic      => "영웅",
        HeroGrade.Legendary => "전설",
        HeroGrade.Myth      => "신화",
        _                   => grade.ToString()
    };

    public static string ToDisplayText(this AbilityTrigger trigger) => trigger switch
    {
        AbilityTrigger.OnSummon      => "소환 시",
        AbilityTrigger.OnSold        => "판매 시",
        AbilityTrigger.OnBattleStart => "전투 시작 시",
        AbilityTrigger.OnMove        => "이동 시",
        AbilityTrigger.OnAttack      => "공격 시",
        AbilityTrigger.OnPrevAllyHit     => "앞의 아군 피격 시",
        AbilityTrigger.OnPrevAllyOnlyHit => "바로 앞 아군 피격 시",
        AbilityTrigger.OnAllyHit     => "아군 피격 시",
        AbilityTrigger.OnHit         => "피격 시",
        AbilityTrigger.OnAllyDeath   => "아군 사망 시",
        AbilityTrigger.OnEnemyDeath  => "적군 사망 시",
        AbilityTrigger.OnDeath       => "사망 시",
        AbilityTrigger.OnCycleEnd    => "사이클 종료 시",
        _                            => trigger.ToString()
    };

    public static string ToDisplayText(this AbilityTarget target) => target switch
    {
        AbilityTarget.Self         => "자신",
        AbilityTarget.AllAllies    => "전체 아군",
        AbilityTarget.RandomAlly   => "무작위 아군",
        AbilityTarget.RandomAlly2  => "무작위 아군 2명",
        AbilityTarget.PrevAlly     => "앞의 아군",
        AbilityTarget.NextAlly     => "뒤의 아군",
        AbilityTarget.FrontAlly    => "맨 앞의 아군",
        AbilityTarget.BackAlly     => "맨 뒤의 아군",
        AbilityTarget.HighestHpAlly    => "가장 높은 체력을 가진 아군",
        AbilityTarget.LowestHpAlly     => "가장 낮은 체력을 가진 아군",
        AbilityTarget.AllEnemies   => "전체 적군",
        AbilityTarget.RandomEnemy  => "무작위 적군",
        AbilityTarget.RandomEnemy2 => "무작위 적군 2명",
        AbilityTarget.FrontEnemy   => "맨 앞의 적군",
        AbilityTarget.BackEnemy    => "맨 뒤의 적군",
        AbilityTarget.HighestHpEnemy   => "가장 높은 체력을 가진 적군",
        AbilityTarget.LowestHpEnemy    => "가장 낮은 체력을 가진 적군",
        AbilityTarget.AllHeroes    => "전체 영웅",
        _                          => target.ToString()
    };

    public static string ToDisplayText(this AbilityEffect effect, AbilityTarget target, int value)
    {
        var t = target.ToDisplayText();
        return effect switch
        {
            AbilityEffect.IncreaseAttack       => $"{t}의 {Sprite("Attack")} 공격력 +{value}",
            AbilityEffect.DecreaseAttack       => $"{t}의 {Sprite("Attack")} 공격력 -{value}",
            AbilityEffect.IncreaseHealth       => $"{t}의 {Sprite("Health")} 체력 +{value}",
            AbilityEffect.IncreaseAttackHealth => $"{t}의 {Sprite("Attack")} 공격력 +{value} / {Sprite("Health")} 체력 +{value}",
            AbilityEffect.DealDamage           => $"{t}에게 {value} 데미지",
            AbilityEffect.BonusAttack          => $"{t}에게 {value}회 추가 공격",
            AbilityEffect.Smash                => $"{t}에게 강타 ({value} 데미지)",
            AbilityEffect.Faint                => $"{t} {value}턴 기절",
            AbilityEffect.Push                 => $"{t} {value}칸 밀기",
            AbilityEffect.Pull                 => $"{t} {value}칸 당기기",
            AbilityEffect.Summon               => $"유닛 {value}개 소환",
            AbilityEffect.EarnGold             => $"{Sprite("Gold")} 골드 +{value}",
            _                                  => effect.ToString()
        };
    }
}
