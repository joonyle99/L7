using UnityEngine;
using System.Collections.Generic;

public static class AbilityExecutor
{
    /// <summary>
    /// 영웅 한 명의 어빌리티를 트리거에 대해 실행 시도
    /// 사용 예: OnAttack → 지금 공격한 영웅 한 명만 체크
    /// </summary>
    public static void TryExecute(
        BattleHero caster,
        AbilityTrigger trigger,
        BattleHero[] allies,
        BattleHero[] enemies,
        BattleTimeline timeline,
        int cycle)
    {
        if (caster == null) return;
        if (!caster.IsAlive // 죽었는데
            && trigger != AbilityTrigger.OnDeath // Trigger가 OnSelfDeath가 아니고
            && trigger != AbilityTrigger.OnHit) return; // Trigger가 OnHit도 아니라면 전투 이벤트로 기록하지 않음

        var ability = caster.Source.Data.Ability;

        if (ability.Trigger != trigger) return; // 1. 실패: 영웅 능력의 트리거와 같은지 체크
        if (Random.value * 100f > ability.Probability) return; // 2. 실패: 능력 발동 확률 계산 (0~100)
        var selectedTargets = SelectTargets(ability.Target, caster, allies, enemies);
        if (selectedTargets.Count == 0) return; // 3. 실패: 능력의 대상이 존재하는지 체크

        Debug.Log($"<color=#CE93D8>[Ability] {caster.Source.Data.Name} → {ability.Trigger} | {ability.Effect} | {ability.Target} | Value={ability.GetValue(caster.Source.Level)} | targets={selectedTargets.Count}</color> (cycle={cycle})");

        foreach (var target in selectedTargets)
        {
            ApplyEffect(ability, caster, target, allies, enemies, timeline, cycle);
        }
    }

    /// <summary>
    /// 진영 전체를 순회하며 각자 TryExecute 호출
    /// 사용 예: OnAllyDeath → 아군 전원이 각자 "아군이 죽었을 때" 어빌리티를 가지고 있는지 체크
    /// </summary>
    public static void TryExecuteAll(
        BattleHero[] side,
        AbilityTrigger trigger,
        BattleHero[] allies,
        BattleHero[] enemies,
        BattleTimeline timeline,
        int cycle)
    {
        foreach (var hero in side)
        {
            if (hero == null) continue;
            TryExecute(hero, trigger, allies, enemies, timeline, cycle);
        }
    }

    // ========= 타겟 해석 =========

    private static List<BattleHero> SelectTargets(AbilityTarget target, BattleHero caster, BattleHero[] allies, BattleHero[] enemies)
    {
        var result = new List<BattleHero>();

        switch (target)
        {
            case AbilityTarget.Self:
                if (caster.IsAlive) result.Add(caster);
                break;

            case AbilityTarget.AllAllies:
                foreach (var h in allies)
                    if (h != null && h.IsAlive && h != caster) result.Add(h);
                break;

            case AbilityTarget.RandomAlly:
                var aliveAllies = new List<BattleHero>();
                foreach (var h in allies)
                    if (h != null && h.IsAlive && h != caster) aliveAllies.Add(h);
                if (aliveAllies.Count > 0) result.Add(aliveAllies[Random.Range(0, aliveAllies.Count)]);
                break;

            case AbilityTarget.RandomAlly2:
                var aliveAllies2 = new List<BattleHero>();
                foreach (var h in allies)
                    if (h != null && h.IsAlive && h != caster) aliveAllies2.Add(h);
                Shuffle(aliveAllies2);
                for (int i = 0; i < Mathf.Min(2, aliveAllies2.Count); i++) result.Add(aliveAllies2[i]);
                break;

            case AbilityTarget.PrevAlly:
                var prevAlly = FindPrev(allies, caster);
                if (prevAlly != null) result.Add(prevAlly);
                break;

            case AbilityTarget.NextAlly:
                var nextAlly = FindNext(allies, caster);
                if (nextAlly != null) result.Add(nextAlly);
                break;

            case AbilityTarget.FrontAlly:
                var frontAlly = FindFront(allies, caster);
                if (frontAlly != null) result.Add(frontAlly);
                break;

            case AbilityTarget.BackAlly:
                var backAlly = FindBack(allies, caster);
                if (backAlly != null) result.Add(backAlly);
                break;

            case AbilityTarget.HighestHpAlly:
                var highestHpAlly = FindHighestHp(allies, caster);
                if (highestHpAlly != null) result.Add(highestHpAlly);
                break;

            case AbilityTarget.LowestHpAlly:
                var lowestHpAlly = FindLowestHp(allies, caster);
                if (lowestHpAlly != null) result.Add(lowestHpAlly);
                break;

            case AbilityTarget.AllEnemies:
                foreach (var h in enemies)
                    if (h != null && h.IsAlive) result.Add(h);
                break;

            case AbilityTarget.RandomEnemy:
                var aliveEnemies = new List<BattleHero>();
                foreach (var h in enemies)
                    if (h != null && h.IsAlive) aliveEnemies.Add(h);
                if (aliveEnemies.Count > 0) result.Add(aliveEnemies[Random.Range(0, aliveEnemies.Count)]);
                break;

            case AbilityTarget.RandomEnemy2:
                var aliveEnemies2 = new List<BattleHero>();
                foreach (var h in enemies)
                    if (h != null && h.IsAlive) aliveEnemies2.Add(h);
                Shuffle(aliveEnemies2);
                for (int i = 0; i < Mathf.Min(2, aliveEnemies2.Count); i++) result.Add(aliveEnemies2[i]);
                break;

            case AbilityTarget.FrontEnemy:
                var frontEnemy = FindFront(enemies, null);
                var allEnemyLog = string.Join(", ", System.Linq.Enumerable.Select(enemies, h => h == null ? "null" : $"{h.Source.Data.Name}(slot={h.SlotIdx}, alive={h.IsAlive})"));
                Debug.Log($"<color=#FF6F00>[FrontEnemy]</color> enemies=[{allEnemyLog}] → selected={frontEnemy?.Source.Data.Name ?? "null"}");
                if (frontEnemy != null) result.Add(frontEnemy);
                break;

            case AbilityTarget.BackEnemy:
                var backEnemy = FindBack(enemies, null);
                if (backEnemy != null) result.Add(backEnemy);
                break;

            case AbilityTarget.HighestHpEnemy:
                var highestHpEnemy = FindHighestHp(enemies, null);
                if (highestHpEnemy != null) result.Add(highestHpEnemy);
                break;

            case AbilityTarget.LowestHpEnemy:
                var lowestHpEnemy = FindLowestHp(enemies, null);
                if (lowestHpEnemy != null) result.Add(lowestHpEnemy);
                break;

            case AbilityTarget.AllHeroes:
                foreach (var h in allies)
                    if (h != null && h.IsAlive && h != caster) result.Add(h);
                foreach (var h in enemies)
                    if (h != null && h.IsAlive) result.Add(h);
                break;
        }

        return result;
    }

    /// <summary>
    /// 영웅이 피해를 받았을 때 아군들의 연쇄 트리거 처리
    /// OnAllyHit: hitHero 제외 아군 전원 체크
    /// OnPrevAllyHit: hitHero가 자신 앞에 있는 영웅 모두 트리거
    /// OnPrevAllyOnlyHit: hitHero가 자신의 바로 앞 영웅인 경우만 트리거
    /// </summary>
    public static void TryExecuteAllyHitChain(
        BattleHero hitHero,
        BattleHero[] hitSide,
        BattleHero[] otherSide,
        BattleTimeline timeline,
        int cycle)
    {
        bool isPlayer = IsPlayerSide(hitSide);
        foreach (var hero in hitSide)
        {
            if (hero == null || !hero.IsAlive || hero == hitHero) continue;
            TryExecute(hero, AbilityTrigger.OnAllyHit, hitSide, otherSide, timeline, cycle);

            bool hitHeroIsInFront = isPlayer ? hitHero.SlotIdx > hero.SlotIdx : hitHero.SlotIdx < hero.SlotIdx;
            
            if (hitHeroIsInFront)
                TryExecute(hero, AbilityTrigger.OnPrevAllyHit, hitSide, otherSide, timeline, cycle);

            if (FindPrev(hitSide, hero) == hitHero)
                TryExecute(hero, AbilityTrigger.OnPrevAllyOnlyHit, hitSide, otherSide, timeline, cycle);
        }
    }

    // Player: SlotIdx 높을수록 앞 / Enemy: SlotIdx 낮을수록 앞
    private static bool IsPlayerSide(BattleHero[] side)
    {
        foreach (var h in side) if (h != null) return h.IsPlayer;
        return false;
    }

    // 가장 앞 생존 영웅 (caster 제외)
    private static BattleHero FindFront(BattleHero[] side, BattleHero exclude)
    {
        bool isPlayer = IsPlayerSide(side);
        BattleHero result = null;
        foreach (var h in side)
        {
            if (h == null || !h.IsAlive || h == exclude) continue;
            if (result == null) { result = h; continue; }
            if (isPlayer ? h.SlotIdx > result.SlotIdx : h.SlotIdx < result.SlotIdx) result = h;
        }
        return result;
    }

    // 가장 뒤 생존 영웅 (caster 제외)
    private static BattleHero FindBack(BattleHero[] side, BattleHero exclude)
    {
        bool isPlayer = IsPlayerSide(side);
        BattleHero result = null;
        foreach (var h in side)
        {
            if (h == null || !h.IsAlive || h == exclude) continue;
            if (result == null) { result = h; continue; }
            if (isPlayer ? h.SlotIdx < result.SlotIdx : h.SlotIdx > result.SlotIdx) result = h;
        }
        return result;
    }

    // reference 바로 앞 생존 영웅
    private static BattleHero FindPrev(BattleHero[] side, BattleHero reference)
    {
        if (reference == null) return null;
        bool isPlayer = IsPlayerSide(side);
        BattleHero result = null;
        foreach (var h in side)
        {
            if (h == null || !h.IsAlive || h == reference) continue;
            bool isAhead = isPlayer ? h.SlotIdx > reference.SlotIdx : h.SlotIdx < reference.SlotIdx;
            if (!isAhead) continue;
            if (result == null) { result = h; continue; }
            if (isPlayer ? h.SlotIdx < result.SlotIdx : h.SlotIdx > result.SlotIdx) result = h;
        }
        return result;
    }

    // reference 바로 뒤 생존 영웅
    private static BattleHero FindNext(BattleHero[] side, BattleHero reference)
    {
        if (reference == null) return null;
        bool isPlayer = IsPlayerSide(side);
        BattleHero result = null;
        foreach (var h in side)
        {
            if (h == null || !h.IsAlive || h == reference) continue;
            bool isBehind = isPlayer ? h.SlotIdx < reference.SlotIdx : h.SlotIdx > reference.SlotIdx;
            if (!isBehind) continue;
            if (result == null) { result = h; continue; }
            if (isPlayer ? h.SlotIdx > result.SlotIdx : h.SlotIdx < result.SlotIdx) result = h;
        }
        return result;
    }

    // 현재 체력이 가장 높은 생존 영웅 (exclude 제외)
    private static BattleHero FindHighestHp(BattleHero[] side, BattleHero exclude)
    {
        BattleHero result = null;
        foreach (var h in side)
        {
            if (h == null || !h.IsAlive || h == exclude) continue;
            if (result == null || h.CurrHealth > result.CurrHealth) result = h;
        }
        return result;
    }

    // 현재 체력이 가장 낮은 생존 영웅 (exclude 제외)
    private static BattleHero FindLowestHp(BattleHero[] side, BattleHero exclude)
    {
        BattleHero result = null;
        foreach (var h in side)
        {
            if (h == null || !h.IsAlive || h == exclude) continue;
            if (result == null || h.CurrHealth < result.CurrHealth) result = h;
        }
        return result;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ========= 효과 적용 =========

    private static void ApplyEffect(AbilityData ability, BattleHero caster, BattleHero target, BattleHero[] allies, BattleHero[] enemies, BattleTimeline timeline, int cycle)
    {
        int value = ability.GetValue(caster.Source.Level);

        switch (ability.Effect)
        {
            case AbilityEffect.IncreaseAttack:
                target.CurrAttack += value;
                // Debug.Log($"<color=#CE93D8>[Ability] {target.Source.Data.Name} 공격력 +{value} → {target.CurrAttack}</color>");
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, value);
                break;

            case AbilityEffect.DecreaseAttack:
                target.CurrAttack = Mathf.Max(0, target.CurrAttack - value);
                // Debug.Log($"<color=#CE93D8>[Ability] {target.Source.Data.Name} 공격력 -{value} → {target.CurrAttack}</color>");
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, value);
                break;

            case AbilityEffect.IncreaseHealth:
                target.CurrHealth += value;
                // Debug.Log($"<color=#CE93D8>[Ability] {target.Source.Data.Name} 체력 +{value} → {target.CurrHealth}</color>");
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, value);
                break;

            case AbilityEffect.IncreaseAttackHealth:
                target.CurrAttack += value;
                target.CurrHealth += value;
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, value);
                break;

            case AbilityEffect.DealDamage:
                target.CurrHealth = Mathf.Max(0, target.CurrHealth - value);
                // Debug.Log($"<color=#CE93D8>[Ability] {caster.Source.Data.Name} → {target.Source.Data.Name} 데미지 {value} | 남은 HP {target.CurrHealth}</color>");
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, value);
                TryExecute(target, AbilityTrigger.OnHit, enemies, allies, timeline, cycle);
                TryExecuteAllyHitChain(target, enemies, allies, timeline, cycle);
                if (!target.IsAlive)
                {
                    BattleRecorder.RecordDeath(timeline, cycle, target);
                    TryExecute(target, AbilityTrigger.OnDeath, enemies, allies, timeline, cycle);
                    TryExecuteAll(enemies, AbilityTrigger.OnAllyDeath, enemies, allies, timeline, cycle);
                    TryExecuteAll(allies, AbilityTrigger.OnEnemyDeath, allies, enemies, timeline, cycle);
                }
                break;

            case AbilityEffect.BonusAttack:
                value = caster.CurrAttack;
                Debug.Log($"<color=#FF6F00>[BonusAttack]</color> {caster.Source.Data.Name} → {target.Source.Data.Name} | damage={value} | target HP {target.CurrHealth} → {target.CurrHealth - value}");
                target.CurrHealth = Mathf.Max(0, target.CurrHealth - value);
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, value);
                TryExecute(target, AbilityTrigger.OnHit, enemies, allies, timeline, cycle);
                TryExecuteAllyHitChain(target, enemies, allies, timeline, cycle);
                if (!target.IsAlive)
                {
                    BattleRecorder.RecordDeath(timeline, cycle, target);
                    TryExecute(target, AbilityTrigger.OnDeath, enemies, allies, timeline, cycle);
                    TryExecuteAll(enemies, AbilityTrigger.OnAllyDeath, enemies, allies, timeline, cycle);
                    TryExecuteAll(allies, AbilityTrigger.OnEnemyDeath, allies, enemies, timeline, cycle);
                }
                break;

            case AbilityEffect.Pull:
                var side = (target.IsPlayer == caster.IsPlayer) ? allies : enemies;
                int idx = -1;
                for (int i = 0; i < side.Length; i++) if (side[i] == target) { idx = i; break; }
                if (idx > 0)
                {
                    for (int i = idx; i > 0; i--) side[i] = side[i - 1];
                    side[0] = target;
                }
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, 0);
                break;

            case AbilityEffect.Faint:
                if (target.FaintImmune) break;
                target.IsFainted = true;
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, 0);
                break;

            case AbilityEffect.EarnGold:
                caster.Source.GoldStack += value;
                // Debug.Log($"<color=#CE93D8>[Ability] {caster.Source.Data.Name} 골드 스택 +{value} → {caster.Source.GoldStack}</color>");
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, value);
                break;

            case AbilityEffect.Smash:
                int smashDamage = target.CurrHealth;
                target.CurrHealth = 0;
                BattleRecorder.RecordAbility(timeline, cycle, caster, target, ability.Effect, smashDamage);
                TryExecute(target, AbilityTrigger.OnHit, enemies, allies, timeline, cycle);
                TryExecuteAllyHitChain(target, enemies, allies, timeline, cycle);
                BattleRecorder.RecordDeath(timeline, cycle, target);
                TryExecute(target, AbilityTrigger.OnDeath, enemies, allies, timeline, cycle);
                TryExecuteAll(enemies, AbilityTrigger.OnAllyDeath, enemies, allies, timeline, cycle);
                TryExecuteAll(allies, AbilityTrigger.OnEnemyDeath, allies, enemies, timeline, cycle);
                break;

            default:
                // Debug.Log($"<color=#CE93D8>[Ability] {ability.Effect} 미구현</color>");
                break;
        }
    }
}
