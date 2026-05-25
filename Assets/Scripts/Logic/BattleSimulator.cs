using UnityEngine;

public class BattleSimulator
{
    private const int MAX_CYCLES = 1000; // 무한루프 방지

    public BattleTimeline Simulate(BattleHero[] playerSide, BattleHero[] enemySide, int capacity)
    {
        var timeline = new BattleTimeline();

        // OnBattleStart
        AbilityExecutor.TryExecuteAll(playerSide, AbilityTrigger.OnBattleStart, playerSide, enemySide, timeline, BattleEvent.CYCLE_PREBATTLE);
        AbilityExecutor.TryExecuteAll(enemySide, AbilityTrigger.OnBattleStart, enemySide, playerSide, timeline, BattleEvent.CYCLE_PREBATTLE);

        var cycle = 0;

        while (HasAlive(playerSide) && HasAlive(enemySide) && cycle < MAX_CYCLES)
        {
            // Compact (빈 슬롯 정리 — 첫 사이클엔 초기 배치, 이후엔 전 사이클 사망 처리)
            var movedHeroes = BattleRecorder.RecordCompact(timeline, cycle, playerSide, enemySide, capacity);
            foreach (var (hero, dist) in movedHeroes)
            {
                UnityEngine.Debug.Log($"<color=#80CBC4>[Compact]</color> {hero.Source.Data.Name} 슬롯 이동 {dist}칸 → OnMove x{dist} (cycle={cycle})");
                var allies  = hero.IsPlayer ? playerSide : enemySide;
                var enemies = hero.IsPlayer ? enemySide  : playerSide;
                for (int cnt = 0; cnt < dist; cnt++) AbilityExecutor.TryExecute(hero, AbilityTrigger.OnMove, allies, enemies, timeline, cycle);
            }

            // 1:1 오토배틀러 규칙 - 맨 앞의 영웅끼리 싸운다
            var player = FindFirstAlive(playerSide);
            var enemy = FindFirstAlive(enemySide);

            // 기절 상태 스냅샷 후 해제 (소모된 쪽은 이번 사이클 재발동 면역)
            bool playerFainted = player.IsFainted; player.IsFainted = false; player.FaintImmune = playerFainted;
            bool enemyFainted  = enemy.IsFainted;  enemy.IsFainted  = false; enemy.FaintImmune  = enemyFainted;

            // 동시 공격을 위해 체력 적용 전에 스냅샷 (기절 시 0 데미지)
            int playerDamage = playerFainted ? 0 : player.CurrAttack;
            int enemyDamage  = enemyFainted  ? 0 : enemy.CurrAttack;

            if (playerFainted) UnityEngine.Debug.Log($"<color=#B0BEC5>[Simulator]</color> Cycle {cycle} — {player.Source.Data.Name} 기절, 공격 스킵");
            if (enemyFainted)  UnityEngine.Debug.Log($"<color=#B0BEC5>[Simulator]</color> Cycle {cycle} — {enemy.Source.Data.Name} 기절, 공격 스킵");

            UnityEngine.Debug.Log($"<color=#B0BEC5>[Simulator]</color> Cycle {cycle}\n" +
                $"  <color=#81D4FA>{player.Source.Data.Name}</color>  Curr HP:<color=#A5D6A7>{player.CurrHealth}</color> → Next HP:<color=#A5D6A7>{player.CurrHealth - enemyDamage}</color>  <color=#FFCC80>(Hit Damage: {enemyDamage})</color>\n" +
                $"  <color=#EF9A9A>{enemy.Source.Data.Name}</color>  Curr HP:<color=#A5D6A7>{enemy.CurrHealth}</color> → Next HP:<color=#A5D6A7>{enemy.CurrHealth - playerDamage}</color>  <color=#FFCC80>(Hit Damage: {playerDamage})</color>");

            // 동시에 체력 적용
            enemy.CurrHealth = Mathf.Max(0, enemy.CurrHealth - playerDamage);
            player.CurrHealth = Mathf.Max(0, player.CurrHealth - enemyDamage);

            // 기본 공격 이벤트 기록 (OnAttack 어빌리티보다 먼저 기록해야 BattlePlayer에서 올바른 순서로 재생됨)
            BattleRecorder.RecordAttack(timeline, cycle, player, enemy, playerDamage, playerFainted);
            BattleRecorder.RecordAttack(timeline, cycle, enemy, player, enemyDamage, enemyFainted);

            // OnAttack (기절 시 스킵)
            if (!playerFainted) AbilityExecutor.TryExecute(player, AbilityTrigger.OnAttack, playerSide, enemySide, timeline, cycle);
            if (!enemyFainted)  AbilityExecutor.TryExecute(enemy, AbilityTrigger.OnAttack, enemySide, playerSide, timeline, cycle);

            // OnHit (기절된 쪽의 공격으로 인한 OnHit는 발동 안 함)
            {
                if (!playerFainted)
                {
                    AbilityExecutor.TryExecute(enemy, AbilityTrigger.OnHit, enemySide, playerSide, timeline, cycle);
                    AbilityExecutor.TryExecuteAllyHitChain(enemy, enemySide, playerSide, timeline, cycle);
                }

                if (!enemyFainted)
                {
                    AbilityExecutor.TryExecute(player, AbilityTrigger.OnHit, playerSide, enemySide, timeline, cycle);
                    AbilityExecutor.TryExecuteAllyHitChain(player, playerSide, enemySide, timeline, cycle);
                }
            }
            
            // OnDeath, OnAllyDeath, OnEnemyDeath
            {
                if (!player.IsAlive)
                {
                    UnityEngine.Debug.Log($"<color=#FF5252>[Simulator] {player.Source.Data.Name} 사망</color> (cycle={cycle})");
                    BattleRecorder.RecordDeath(timeline, cycle, player);
                    AbilityExecutor.TryExecute(player, AbilityTrigger.OnDeath, playerSide, enemySide, timeline, cycle);
                    AbilityExecutor.TryExecuteAll(playerSide, AbilityTrigger.OnAllyDeath, playerSide, enemySide, timeline, cycle);
                    AbilityExecutor.TryExecuteAll(enemySide, AbilityTrigger.OnEnemyDeath, enemySide, playerSide, timeline, cycle);
                }

                if (!enemy.IsAlive)
                {
                    UnityEngine.Debug.Log($"<color=#FF5252>[Simulator] {enemy.Source.Data.Name} 사망</color> (cycle={cycle})");
                    BattleRecorder.RecordDeath(timeline, cycle, enemy);
                    AbilityExecutor.TryExecute(enemy, AbilityTrigger.OnDeath, enemySide, playerSide, timeline, cycle);
                    AbilityExecutor.TryExecuteAll(enemySide, AbilityTrigger.OnAllyDeath, enemySide, playerSide, timeline, cycle);
                    AbilityExecutor.TryExecuteAll(playerSide, AbilityTrigger.OnEnemyDeath, playerSide, enemySide, timeline, cycle);
                }
            }

            // OnCycleEnd
            AbilityExecutor.TryExecuteAll(playerSide, AbilityTrigger.OnCycleEnd, playerSide, enemySide, timeline, cycle);
            AbilityExecutor.TryExecuteAll(enemySide, AbilityTrigger.OnCycleEnd, enemySide, playerSide, timeline, cycle);

            cycle++;
        }

        timeline.RoundOutcome = DetermineOutcome(playerSide, enemySide);

        UnityEngine.Debug.Log($"<color=#FF8C00>[Simulator] 결과: {timeline.RoundOutcome}</color> | 총 이벤트 {timeline.Events.Count}개");

        return timeline;
    }
    
    private RoundOutcome DetermineOutcome(BattleHero[] playerSide, BattleHero[] enemySide)
    {
        bool playerAlive = HasAlive(playerSide);
        bool enemyAlive = HasAlive(enemySide);

        if (playerAlive && !enemyAlive) return RoundOutcome.Player_Win;
        if (!playerAlive && enemyAlive) return RoundOutcome.Enemy_Win;

        return RoundOutcome.Draw;
    }

    // ========= ... =========

    private bool HasAlive(BattleHero[] side)
    {
        foreach (var hero in side)
        {
            if (hero != null && hero.IsAlive)
            {
                return true;
            }
        }

        return false;
    }

    private BattleHero FindFirstAlive(BattleHero[] side)
    {
        foreach (var hero in side)
        {
            if (hero != null && hero.IsAlive)
            {
                return hero;
            }
        }

        return null;
    }
}
