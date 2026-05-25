using UnityEngine;
using System.Collections.Generic;

public static class BattleRecorder
{
    public static List<(BattleHero Hero, int Dist)> RecordCompact(BattleTimeline timeline, int cycle, BattleHero[] playerSide, BattleHero[] enemySide, int capacity)
    {
        var moved = new List<(BattleHero, int)>();

        // 플레이어: capacity-1-i 로 우측(front)부터 채움
        var aliveIdx = 0;
        for (int i = 0; i < playerSide.Length; i++)
        {
            var hero = playerSide[i];
            if (hero == null || !hero.IsAlive) continue;
            var targetIdx = capacity - 1 - aliveIdx++;
            if (hero.SlotIdx == targetIdx) continue;
            var dist = Mathf.Abs(targetIdx - hero.SlotIdx);
            timeline.Events.Add(new BattleEvent
            {
                Cycle = cycle,
                EventType = BattleEventType.Compact,
                Hero = hero,
                FromSlotIdx = hero.SlotIdx,
                ToSlotIdx = targetIdx,
            });
            hero.SlotIdx = targetIdx;
            moved.Add((hero, dist));
        }

        // 적: i 로 좌측(front)부터 채움
        aliveIdx = 0;
        for (int i = 0; i < enemySide.Length; i++)
        {
            var hero = enemySide[i];
            if (hero == null || !hero.IsAlive) continue;
            var targetIdx = aliveIdx++;
            if (hero.SlotIdx == targetIdx) continue;
            var dist = Mathf.Abs(targetIdx - hero.SlotIdx);
            timeline.Events.Add(new BattleEvent
            {
                Cycle = cycle,
                EventType = BattleEventType.Compact,
                Hero = hero,
                FromSlotIdx = hero.SlotIdx,
                ToSlotIdx = targetIdx,
            });
            hero.SlotIdx = targetIdx;
            moved.Add((hero, dist));
        }

        return moved;
    }

    public static void RecordAttack(BattleTimeline timeline, int cycle, BattleHero attacker, BattleHero target, int damage, bool fainted = false)
    {
        timeline.Events.Add(new BattleEvent
        {
            Cycle = cycle,
            EventType = BattleEventType.Attack,
            Attacker = attacker,
            Target = target,
            Damage = damage,
            AttackerFainted = fainted,
            AttackerAtk = attacker.CurrAttack,
            AttackerHp = attacker.CurrHealth,
            AttackerSlotIdx = attacker.SlotIdx,
            TargetAtk = target.CurrAttack,
            TargetHP = target.CurrHealth,
            TargetSlotIdx = target.SlotIdx,
        });
    }

    public static void RecordAbility(BattleTimeline timeline, int cycle, BattleHero caster, BattleHero target, AbilityEffect effect, int value)
    {
        timeline.Events.Add(new BattleEvent
        {
            Cycle = cycle,
            EventType = BattleEventType.Ability,
            AbilityEffect = effect,
            Attacker = caster,
            Target = target,
            Damage = value,
            AttackerAtk = caster.CurrAttack,
            AttackerHp = caster.CurrHealth,
            AttackerSlotIdx = caster.SlotIdx,
            TargetAtk = target.CurrAttack,
            TargetHP = target.CurrHealth,
            TargetSlotIdx = target.SlotIdx,
        });
    }

    public static void RecordDeath(BattleTimeline timeline, int cycle, BattleHero target)
    {
        timeline.Events.Add(new BattleEvent
        {
            Cycle = cycle,
            EventType = BattleEventType.Death,
            Target = target,
            TargetAtk = target.CurrAttack,
            TargetHP = target.CurrHealth,
        });
    }
}
