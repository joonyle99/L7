using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class BattleManager
{
    private BattleSlotController _playerSlotController;
    private BattleSlotController _enemySlotController;
    private BattleSimulator _battleSimulator;
    private BattlePlayer _battlePlayer;
    private Action _onBattleEnd;
    private Action<RoundOutcome> _onBattleEndAfter;

    private readonly Dictionary<HeroInstance, HeroView> _heroViewMap = new();

    private HeroInstance[] _playerBench;
    private HeroInstance[] _enemyBench;

    private BattleHero[] _playerSide;
    private BattleHero[] _enemySide;

    public void Initialize(
        BattleSlotController playerSlotController,
        BattleSlotController enemySlotController,
        BattleSimulator battleSimulator,
        BattlePlayer battlePlayer,
        Action onBattleEnd,
        Action<RoundOutcome> onBattleEndAfter)
    {
        _playerSlotController = playerSlotController;
        _enemySlotController = enemySlotController;
        _battleSimulator = battleSimulator;
        _battlePlayer = battlePlayer;
        _onBattleEnd = onBattleEnd;
        _onBattleEndAfter = onBattleEndAfter;
    }

    // ======== ... ========

    private void Spawn(HeroInstance[] bench, BattleSlotController slotController)
    {
        var flipX = slotController == _enemySlotController;
        for (int idx = 0; idx < bench.Length; idx++)
        {
            var hero = bench[idx];
            if (hero == null) continue;
            var heroView = slotController.SpawnHeroView(hero, idx, flipX);
            _heroViewMap[hero] = heroView;
        }
    }

    public HeroView Spawn(BattleHero hero)
    {
        var controller = hero.IsPlayer ? _playerSlotController : _enemySlotController;
        var heroView = controller.SpawnHeroView(hero.Source, hero.SlotIdx, flipX: !hero.IsPlayer);
        _heroViewMap[hero.Source] = heroView;
        return heroView;
    }

    public void Clear()
    {
        foreach (var heroView in _heroViewMap.Values)
        {
            if (heroView != null)
            {
                UnityEngine.Object.Destroy(heroView.gameObject);
            }
        }

        _heroViewMap.Clear();
    }

    // ======== ... ========

    public Transform GetSlot(bool isPlayer, int idx)
    {
        var slot = isPlayer ? _playerSlotController.GetSlot(idx) : _enemySlotController.GetSlot(idx);
        return slot;
    }

    public HeroView GetHeroView(HeroInstance hero)
    {
        _heroViewMap.TryGetValue(hero, out var heroView);
        return heroView;
    }

    // ======== ... ========

    public void StartBattle(HeroInstance[] playerBench, HeroInstance[] enemyBench)
    {
        Clear();

        _playerBench = playerBench;
        _enemyBench = enemyBench;
        
        Spawn(_playerBench, _playerSlotController);
        Spawn(_enemyBench, _enemySlotController);

        _playerSide = _playerBench.Select((hero, benchIdx) => (hero, benchIdx)).Where(x => x.hero != null).Select(x => new BattleHero(x.hero, x.benchIdx, isPlayer: true)).Reverse().ToArray();
        _enemySide = _enemyBench.Select((hero, benchIdx) => (hero, benchIdx)).Where(x => x.hero != null).Select(x => new BattleHero(x.hero, x.benchIdx, isPlayer: false)).ToArray();

        // Debug.Log($"<color=#81D4FA>[Battle] Player Bench ({_playerBench.Length}슬롯)</color>\n" +
        //     string.Join("\n", _playerBench.Select((h, i) => $"[{i}] {(h != null ? h.Data.Name : "빈 슬롯")}")));
        // Debug.Log($"<color=#FFCDD2>[Battle] Enemy Bench ({_enemyBench.Length}슬롯)</color>\n" +
        //     string.Join("\n", _enemyBench.Select((h, i) => $"[{i}] {(h != null ? h.Data.Name : "빈 슬롯")}")));

        // Debug.Log($"<color=#4FC3F7>[Battle] Player Side ({_playerSide.Length}명)</color>\n" +
        //     string.Join("\n", _playerSide.Select(h => $"  SlotIdx={h.SlotIdx}  <color=#81D4FA>{h.Source.Data.Name}</color>  ATK:<color=#FFCC80>{h.Source.Attack}</color>  HP:<color=#A5D6A7>{h.Source.Health}</color>")));
        // Debug.Log($"<color=#EF9A9A>[Battle] Enemy Side ({_enemySide.Length}명)</color>\n" +
        //     string.Join("\n", _enemySide.Select(h => $"  SlotIdx={h.SlotIdx}  <color=#FFCDD2>{h.Source.Data.Name}</color>  ATK:<color=#FFCC80>{h.Source.Attack}</color>  HP:<color=#A5D6A7>{h.Source.Health}</color>")));

        var capacity = _playerSlotController.SlotCount == _enemySlotController.SlotCount ? _playerSlotController.SlotCount : -1;
        var timeline = _battleSimulator.Simulate(_playerSide, _enemySide, capacity);

        _battlePlayer.Play(timeline, () =>
        {
            _onBattleEnd?.Invoke();
            _onBattleEndAfter?.Invoke(timeline.RoundOutcome);
        });
    }
}
