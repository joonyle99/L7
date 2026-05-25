using System.Collections.Generic;

public enum BattleEventType { Attack, Ability, Compact, Death }
public enum RoundOutcome { Player_Win, Enemy_Win, Draw }

public class BattleEvent
{
    public int Cycle;
    public BattleEventType EventType;
    public BattleHero Attacker;
    public BattleHero Target;
    public int Damage;

    public int AttackerAtk; // 이벤트 발생 시점의 스냅샷 (선계산으로 인한 참조값 오염 방지)
    public int AttackerHp;
    public int AttackerSlotIdx;
    public int TargetAtk;
    public int TargetHP;
    public int TargetSlotIdx;

    // Compact 이벤트 전용
    public BattleHero Hero;  // 이동하는 영웅
    public int FromSlotIdx;  // 이동 전 슬롯
    public int ToSlotIdx;    // 이동 후 슬롯

    // Ability 이벤트 전용
    public AbilityEffect AbilityEffect;

    // Attack 이벤트 전용
    public bool AttackerFainted; // 기절로 인해 공격을 건너뜀

    public const int CYCLE_PREBATTLE = -1; // 전투 시작 전 초기 Compact용 사이클 번호
}

public class BattleTimeline
{
    public readonly List<BattleEvent> Events = new List<BattleEvent>();
    public RoundOutcome RoundOutcome;
}
