using System;
using UnityEngine;
using System.Collections.Generic;

public static class PrepareAbilityExecutor
{
    public static void TryExecuteOnSummon(HeroInstance caster, HeroInstance[] squadBench, Action<HeroInstance, AbilityEffect, int> onEffectApplied)
    {
        var ability = caster.Data.Ability;
        if (ability.Trigger != AbilityTrigger.OnSummon) return;
        if (UnityEngine.Random.value * 100f > ability.Probability) return;

        var targets = SelectTargets(ability.Target, caster, squadBench);
        if (targets.Count == 0) return;

        int value = ability.GetValue(caster.Level);

        Debug.Log($"<color=#CE93D8>[PrepareAbility] {caster.Data.Name} → {ability.Trigger} | {ability.Effect} | {ability.Target} | Value={value} | targets={targets.Count}</color>");

        foreach (var target in targets)
        {
            ApplyEffect(ability.Effect, caster, target, value);
            onEffectApplied?.Invoke(target, ability.Effect, value);
        }
    }

    private static List<HeroInstance> SelectTargets(AbilityTarget target, HeroInstance caster, HeroInstance[] squadBench)
    {
        var result = new List<HeroInstance>();

        switch (target)
        {
            case AbilityTarget.Self:
                result.Add(caster);
                break;

            case AbilityTarget.AllAllies:
                foreach (var h in squadBench)
                    if (h != null) result.Add(h);
                break;

            case AbilityTarget.RandomAlly:
            {
                var pool = BuildPool(squadBench);
                if (pool.Count > 0) result.Add(pool[UnityEngine.Random.Range(0, pool.Count)]);
                break;
            }

            case AbilityTarget.RandomAlly2:
            {
                var pool = BuildPool(squadBench);
                Shuffle(pool);
                for (int i = 0; i < Mathf.Min(2, pool.Count); i++) result.Add(pool[i]);
                break;
            }

            case AbilityTarget.FrontAlly:
            {
                for (int i = 0; i < squadBench.Length; i++)
                    if (squadBench[i] != null) { result.Add(squadBench[i]); break; }
                break;
            }

            case AbilityTarget.BackAlly:
            {
                for (int i = squadBench.Length - 1; i >= 0; i--)
                    if (squadBench[i] != null) { result.Add(squadBench[i]); break; }
                break;
            }

            case AbilityTarget.HighestHpAlly:
            {
                HeroInstance best = null;
                foreach (var h in squadBench)
                    if (h != null && (best == null || h.Health > best.Health)) best = h;
                if (best != null) result.Add(best);
                break;
            }

            case AbilityTarget.LowestHpAlly:
            {
                HeroInstance best = null;
                foreach (var h in squadBench)
                    if (h != null && (best == null || h.Health < best.Health)) best = h;
                if (best != null) result.Add(best);
                break;
            }

            // 준비 페이즈에는 적 진영이 없으므로 Enemy 타입은 대상 없음
            // PrevAlly / NextAlly는 소환 벤치 기준 위치가 없으므로 대상 없음
        }

        return result;
    }

    private static void ApplyEffect(AbilityEffect effect, HeroInstance caster, HeroInstance target, int value)
    {
        switch (effect)
        {
            case AbilityEffect.IncreaseAttack:
                target.Attack += value;
                break;
            case AbilityEffect.DecreaseAttack:
                target.Attack = Mathf.Max(0, target.Attack - value);
                break;
            case AbilityEffect.IncreaseHealth:
                target.Health += value;
                break;
            case AbilityEffect.IncreaseAttackHealth:
                target.Attack += value;
                target.Health += value;
                break;
            case AbilityEffect.EarnGold:
                caster.GoldStack += value;
                break;
        }
    }

    private static List<HeroInstance> BuildPool(HeroInstance[] bench)
    {
        var pool = new List<HeroInstance>();
        foreach (var h in bench)
            if (h != null) pool.Add(h);
        return pool;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
