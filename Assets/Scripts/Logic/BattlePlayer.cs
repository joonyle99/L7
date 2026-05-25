using System;
using DG.Tweening;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BattlePlayer : MonoBehaviour
{
    [SerializeField] private float _startBattleDelay = 0.5f;
    [SerializeField] private float _endBattleDelay = 2.0f;
    [SerializeField] private float _cycleInterval = 0.5f;
    [SerializeField] private float _cycleGapDuration = 0.2f;

    [Space]

    [Header("Animation")]
    [SerializeField] private float _windupOffset = 0.3f;
    [SerializeField] private float _clashOffset = 0.6f;
    [SerializeField] private float _windupDuration = 0.15f;
    [SerializeField] private float _windupShakeStrength = 0.025f;
    [SerializeField] private int _windupShakeVibrato = 30;
    [SerializeField] private float _clashDuration = 0.1f;
    [SerializeField] private float _hitPauseDuration = 0.15f;
    [SerializeField] private float _returnDuration = 0.25f;
    [SerializeField] private float _compactDuration = 0.25f;
    [SerializeField] private float _deathFlyX = 2.5f;
    [SerializeField] private float _deathFlyY = 0.5f;
    [SerializeField] private float _deathSpinAngle = 30f;
    [SerializeField] private float _deathFlyDuration = 0.4f;

    [Space]

    [SerializeField] private StoneProjectile _stonePrefab;
    [SerializeField] private StatBuffProjectile _statBuffPrefab;
    [SerializeField] private StatBuffPopup _statBuffPopupPrefab;
    [SerializeField] private DamagePopup _damagePopupPrefab;

    private bool _isPaused;
    private bool _isAutoPlay;
    private bool _isSpeedUp;

    public bool IsPaused => _isPaused;
    public bool IsAutoPlay => _isAutoPlay;
    public bool IsSpeedUp => _isSpeedUp;

    private float _lastTimeScale;

    private bool _isPlaying;
    public bool IsPlaying => _isPlaying;
    public Action<bool> _onPlayingChanged;

    private BattleManager _battleManager;
    private BattleKingController _kingController;
    private DamagePopupPool _damagePopupPool;

    private void SetPlaying(bool value)
    {
        _isPlaying = value;
        _onPlayingChanged?.Invoke(value);
    }

    private bool _inputPending;

    public void Initialize(BattleManager battleManager, BattleKingController kingController, Action<bool> onPlayingChanged)
    {
        _battleManager = battleManager;
        _kingController = kingController;
        _onPlayingChanged = onPlayingChanged;

        _isPaused = false;
        _isAutoPlay = false;
        _isSpeedUp = false;

        _lastTimeScale = 1f;

        _damagePopupPool = new DamagePopupPool(_damagePopupPrefab);
    }

    private void ResetBefore()
    {
        _isPaused = false;
        ApplyTimeScale();
    }
    private void ResetAfter()
    {
        SetPlaying(false);
        Time.timeScale = 1f;
    }

    // ======== ... ========

    public void TogglePlayback()
    {
        if (_isPlaying)
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : _lastTimeScale;
        }
        else
        {
            ReceiveInput();
        }
    }
    public void ToggleAuto()
    {
        if (_isPaused) return;
        _isAutoPlay = !_isAutoPlay;
    }
    public void ToggleSpeed()
    {
        if (_isPaused) return;
        _isSpeedUp = !_isSpeedUp;
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = _isSpeedUp ? 2f : 1f;
        _lastTimeScale = Time.timeScale;
    }

    // ======== 재생 ========

    public void Play(BattleTimeline timeline, Action onComplete)
    {
        StartCoroutine(PlayCoroutine(timeline, onComplete));
    }

    private IEnumerator PlayCoroutine(BattleTimeline timeline, Action onComplete)
    {
        ResetBefore();
        
        yield return new WaitForSeconds(_startBattleDelay);

        var evtIdx = 0;
        var evtList = timeline.Events; // 타임라인 전투 이벤트 목록
        var evtCount = evtList.Count;

        // 전투 시작 전 능력 이벤트 (OnBattleStart)
        for (int idx = 0; idx < evtCount; idx++)
        {
            var evt = evtList[idx];

            if (evt.Cycle >= 0) break; // 전투 시작 전 능력 이벤트만 재생
            
            if (evt.EventType == BattleEventType.Ability)
            {
                SetState(evt.Attacker, HeroViewState.Ability);
                SetPlaying(false);
                yield return WaitForAdvance();
                SetPlaying(true);
                yield return ProcessAbilityEvent(evt);
                SetState(evt.Attacker, HeroViewState.Normal);
            }
            else if (evt.EventType == BattleEventType.Death)
            {
                ProcessDeathEvent(evt);
            }
        }

        while (evtIdx < evtCount && evtList[evtIdx].Cycle < 0) evtIdx++; // 프리배틀 이벤트(cycle < 0) 건너뜀

        while (evtIdx < evtCount)
        {
            var cycle = evtList[evtIdx].Cycle;
            var attackers = GetCycleAttackers(evtList, evtIdx, cycle);

            var cycleEnd = evtIdx;
            while (cycleEnd < evtCount && evtList[cycleEnd].Cycle == cycle) cycleEnd++;

            var player = attackers[0];
            var enemy  = attackers[1];

            // 기절 여부 (Attack 이벤트의 AttackerFainted 플래그로 판단)
            var playerFainted = false;
            var enemyFainted  = false;
            for (int i = evtIdx; i < cycleEnd; i++)
            {
                if (evtList[i].EventType != BattleEventType.Attack) continue;
                if (evtList[i].Attacker.IsPlayer) playerFainted = evtList[i].AttackerFainted;
                else                              enemyFainted  = evtList[i].AttackerFainted;
            }

            // Compact (빈 슬롯 정리)
            yield return Compact(evtList, cycle);

            // Compact 이후 어빌리티 (OnMove 등 — Attack 이벤트 이전에 기록된 것)
            for (int i = evtIdx; i < cycleEnd; i++)
            {
                if (evtList[i].EventType == BattleEventType.Attack) break;
                if (evtList[i].EventType == BattleEventType.Ability)
                    yield return ProcessAbilityEvent(evtList[i]);
            }

            // Wind-up (기절된 영웅은 제외)
            if (!playerFainted) SetState(player, HeroViewState.Attack);
            if (!enemyFainted)  SetState(enemy,  HeroViewState.Attack);
            yield return WindUp(player, enemy, playerFainted, enemyFainted);

            // 입력 / 자동 대기
            SetPlaying(false);
            yield return WaitForAdvance();
            SetPlaying(true);

            // Attack (기절된 영웅은 제외)
            if (!playerFainted) _battleManager.GetHeroView(player.Source)?.SetSprite(HeroSpriteType.Attack);
            if (!enemyFainted)  _battleManager.GetHeroView(enemy.Source)?.SetSprite(HeroSpriteType.Attack);
            yield return Attack(player, enemy, playerFainted, enemyFainted);

            // 기본 공격 HP 갱신
            for (int i = evtIdx; i < cycleEnd; i++)
            {
                if (evtList[i].EventType == BattleEventType.Attack)
                    ProcessAttackEvent(evtList[i]);
            }

            // 히트 포즈
            yield return new WaitForSeconds(_hitPauseDuration);

            // 능력 (기본 공격 이후 — Attack 이벤트 이후에 기록된 것)
            var hasAbility = false;
            var passedAttack = false;
            for (int i = evtIdx; i < cycleEnd; i++)
            {
                if (evtList[i].EventType == BattleEventType.Attack) { passedAttack = true; continue; }
                if (!passedAttack) continue;
                if (evtList[i].EventType == BattleEventType.Ability)
                {
                    hasAbility = true;
                    yield return ProcessAbilityEvent(evtList[i]);
                }
            }
            if (hasAbility) yield return new WaitForSeconds(_hitPauseDuration);

            // 사망 처리
            var playerDied = false;
            var enemyDied = false;
            for (int i = evtIdx; i < cycleEnd; i++)
            {
                if (evtList[i].EventType != BattleEventType.Death) continue;
                if (evtList[i].Target.IsPlayer) playerDied = true;
                else enemyDied = true;
                ProcessDeathEvent(evtList[i]);
            }
            _kingController?.OnCycleDeaths(playerDied, enemyDied);

            evtIdx = cycleEnd;

            // 복귀
            if (!playerFainted) SetState(player, HeroViewState.Normal);
            if (!enemyFainted)  SetState(enemy,  HeroViewState.Normal);
            _battleManager.GetHeroView(player.Source)?.SetSprite(HeroSpriteType.Idle);
            _battleManager.GetHeroView(enemy.Source)?.SetSprite(HeroSpriteType.Idle);
            ReturnToHome(player, enemy, playerDied, enemyDied);
            yield return new WaitForSeconds(_cycleGapDuration);

            if (evtIdx >= evtCount) break;
        }

        yield return new WaitForSeconds(_endBattleDelay);

        onComplete?.Invoke();
        
        ResetAfter();
    }

    // ======== ... ========

    private List<BattleHero> GetCycleAttackers(List<BattleEvent> evts, int startIdx, int cycle)
    {
        var attackers = new List<BattleHero>();
        for (int i = startIdx; i < evts.Count && evts[i].Cycle == cycle; i++)
        {
            if (evts[i].EventType == BattleEventType.Attack && evts[i].Attacker != null)
                attackers.Add(evts[i].Attacker);
        }
        return attackers;
    }

    // ======== 컴팩트 애니메이션 ========

    private IEnumerator Compact(List<BattleEvent> evts, int cycle)
    {
        var seq = DOTween.Sequence();
        var hasMovement = false;

        foreach (var evt in evts)
        {
            if (evt.Cycle != cycle || evt.EventType != BattleEventType.Compact) continue;

            var view = _battleManager.GetHeroView(evt.Hero.Source);
            if (view == null) continue;

            var targetSlot = _battleManager.GetSlot(evt.Hero.IsPlayer, evt.ToSlotIdx);
            if (targetSlot == null) continue;

            view.transform.SetParent(targetSlot, worldPositionStays: true);
            seq.Join(view.transform.DOLocalMove(Vector3.zero, _compactDuration).SetEase(Ease.OutCubic));
            hasMovement = true;
        }

        if (hasMovement) yield return seq.WaitForCompletion();
    }

    // ======== 기본 공격 애니메이션 ========

    private IEnumerator WindUp(BattleHero player, BattleHero enemy, bool playerFainted = false, bool enemyFainted = false)
    {
        var playerView = _battleManager.GetHeroView(player.Source);
        var enemyView  = _battleManager.GetHeroView(enemy.Source);

        var seq = DOTween.Sequence();
        if (!playerFainted && playerView != null)
        {
            playerView.transform.DOKill();
            seq.Join(playerView.transform.DOLocalMoveX(-_windupOffset, _windupDuration).SetEase(Ease.OutCubic));
        }
        if (!enemyFainted  && enemyView  != null)
        {
            enemyView.transform.DOKill();
            seq.Join(enemyView.transform.DOLocalMoveX( _windupOffset, _windupDuration).SetEase(Ease.OutCubic));
        }
        if (seq.IsActive()) yield return seq.WaitForCompletion();
    }

    private IEnumerator Attack(BattleHero player, BattleHero enemy, bool playerFainted = false, bool enemyFainted = false)
    {
        var playerView = _battleManager.GetHeroView(player.Source);
        var enemyView  = _battleManager.GetHeroView(enemy.Source);

        var seq = DOTween.Sequence();
        if (!playerFainted && playerView != null)
        {
            playerView.transform.DOKill();
            seq.Join(playerView.transform.DOLocalMoveX( _clashOffset, _clashDuration).SetEase(Ease.InCubic));
        }
        if (!enemyFainted  && enemyView  != null)
        {
            enemyView.transform.DOKill();
            seq.Join(enemyView.transform.DOLocalMoveX(-_clashOffset, _clashDuration).SetEase(Ease.InCubic));
        }
        if (seq.IsActive()) yield return seq.WaitForCompletion();

        // TODO: 흔들리는..

        // 충돌 이펙트: 한 쪽이라도 공격했을 때만 재생
        if (!playerFainted || !enemyFainted)
        {
            var attackerView = !playerFainted ? playerView : enemyView;
            var defenderView = !playerFainted ? enemyView  : playerView;
            if (attackerView != null && defenderView != null)
            {
                var midPos = (attackerView.transform.position + defenderView.transform.position) * 0.5f;
                EffectManager.Instance.Play(VfxType.Attack, midPos);
                SoundManager.Instance.PlaySfx(SfxType.Fight, 0.4f);
            }
        }
    }

    private void ReturnToHome(BattleHero player, BattleHero enemy, bool playerDied = false, bool enemyDied = false)
    {
        if (!playerDied)
        {
            var view = _battleManager.GetHeroView(player.Source);
            if (view != null) { view.transform.DOKill(); view.transform.DOLocalMove(Vector3.zero, _returnDuration).SetEase(Ease.OutCubic); }
        }
        if (!enemyDied)
        {
            var view = _battleManager.GetHeroView(enemy.Source);
            if (view != null) { view.transform.DOKill(); view.transform.DOLocalMove(Vector3.zero, _returnDuration).SetEase(Ease.OutCubic); }
        }
    }

    // ======== 이벤트 처리 ========

    private void ProcessAttackEvent(BattleEvent evt)
    {
        _battleManager.GetHeroView(evt.Attacker.Source)?.Refresh(evt.AttackerAtk, evt.AttackerHp);
        _battleManager.GetHeroView(evt.Target.Source)?.Refresh(evt.TargetAtk, evt.TargetHP);
        ProcessHit(evt.Target, evt.TargetSlotIdx, evt.Damage);
    }

    private IEnumerator ProcessAbilityEvent(BattleEvent evt)
    {
        var attackerView = _battleManager.GetHeroView(evt.Attacker?.Source);
        var targetView   = _battleManager.GetHeroView(evt.Target?.Source);

        attackerView?.Refresh(evt.AttackerAtk, evt.AttackerHp);

        switch (evt.AbilityEffect)
        {
            case AbilityEffect.DealDamage:
                yield return LaunchStone(evt);
                targetView?.Refresh(evt.TargetAtk, evt.TargetHP);
                ProcessHit(evt.Target, evt.TargetSlotIdx, evt.Damage);
                break;

            case AbilityEffect.IncreaseAttack:
            case AbilityEffect.DecreaseAttack:
            case AbilityEffect.IncreaseHealth:
            case AbilityEffect.IncreaseAttackHealth:
                yield return LaunchStatBuff(evt);
                targetView?.Refresh(evt.TargetAtk, evt.TargetHP, punch: true);
                break;

            case AbilityEffect.BonusAttack:
                var player = evt.Attacker.IsPlayer ? evt.Attacker : evt.Target;
                var enemy = evt.Attacker.IsPlayer ? evt.Target : evt.Attacker;
                yield return Attack(player, enemy, playerFainted: !evt.Attacker.IsPlayer, enemyFainted: evt.Attacker.IsPlayer);
                targetView?.Refresh(evt.TargetAtk, evt.TargetHP);
                ProcessHit(evt.Target, evt.TargetSlotIdx, evt.Damage);
                break;

            case AbilityEffect.Smash:
                yield return LaunchStone(evt);
                targetView?.Refresh(evt.TargetAtk, evt.TargetHP);
                ProcessHit(evt.Target, evt.TargetSlotIdx, evt.Damage);
                break;

            case AbilityEffect.Pull:
                break; // 배열 순서 변경은 시뮬레이터에서 처리, 시각적 효과는 Compact 애니메이션으로 표현

            case AbilityEffect.Faint:
                break;

            case AbilityEffect.EarnGold:
                targetView?.Refresh(evt.TargetAtk, evt.TargetHP);
                break;
        }
    }

    private void ProcessDeathEvent(BattleEvent evt)
    {
        var heroView = _battleManager.GetHeroView(evt.Target.Source);
        if (heroView == null) return;

        var dir = evt.Target.IsPlayer ? -1f : 1f;
        var t = heroView.transform;
        t.DOKill();

        var targetPos = t.localPosition + new Vector3(_deathFlyX * dir, _deathFlyY, 0f);

        heroView.SetActiveDustEffect(true);
        SoundManager.Instance.PlaySfx(SfxType.Die, 0.4f);

        DOTween.Sequence()
            .Join(t.DOLocalMove(targetPos, _deathFlyDuration).SetEase(Ease.OutCubic))
            .Join(t.DOLocalRotate(new Vector3(0f, 0f, _deathSpinAngle * -dir), _deathFlyDuration).SetEase(Ease.OutCubic))
            .OnComplete(() =>
            {
                EffectManager.Instance.Play(VfxType.GoOff, heroView.transform.position, !evt.Target.IsPlayer);
                Destroy(heroView.gameObject);
            });
    }

    // ======== ... ========

    private void SetState(BattleHero hero, HeroViewState state)
    {
        _battleManager.GetHeroView(hero?.Source)?.SetViewState(state);
    }

    private void ProcessHit(BattleHero target, int slotIdx, int damage)
    {
        if (damage <= 0) return; // 기절 등으로 데미지 0이면 이펙트/팝업 생략

        var slot = _battleManager.GetSlot(target.IsPlayer, slotIdx);
        if (slot != null) _damagePopupPool.Spawn(damage, slot.position);

        var targetView = _battleManager.GetHeroView(target.Source);
        if (targetView != null)
        {
            EffectManager.Instance.Play(VfxType.Fragment, targetView.transform.position, target.IsPlayer);
            SoundManager.Instance.PlaySfx(SfxType.Fight, 0.4f);
        }
    }

    private IEnumerator LaunchStone(BattleEvent evt)
    {
        // var casterView = _battleManager.GetHeroView(evt.Attacker?.Source);
        // var targetView = _battleManager.GetHeroView(evt.Target?.Source);

        var casterSlot = _battleManager.GetSlot(evt.Attacker.IsPlayer, evt.AttackerSlotIdx);
        var targetSlot = _battleManager.GetSlot(evt.Target.IsPlayer, evt.TargetSlotIdx);
        var start = casterSlot != null ? casterSlot.position : Vector3.zero;
        var end   = targetSlot != null ? targetSlot.position : start;
        var arrived = false;
        var stone = Instantiate(_stonePrefab);
        stone.Launch(start, end, () => arrived = true);
        yield return new WaitUntil(() => arrived);
    }

    private IEnumerator LaunchStatBuff(BattleEvent evt)
    {
        // var casterView = _battleManager.GetHeroView(evt.Attacker?.Source);
        // var targetView = _battleManager.GetHeroView(evt.Target?.Source);

        var casterSlot = _battleManager.GetSlot(evt.Attacker.IsPlayer, evt.AttackerSlotIdx);
        var targetSlot = _battleManager.GetSlot(evt.Target.IsPlayer, evt.TargetSlotIdx);
        var start = casterSlot != null ? casterSlot.position : Vector3.zero;
        var end   = targetSlot != null ? targetSlot.position : start;

        var arrived = false;
        var projectile = Instantiate(_statBuffPrefab);
        projectile.Launch(start, end, evt.AbilityEffect, () => arrived = true);
        yield return new WaitUntil(() => arrived);

        var done = false;
        var popup = Instantiate(_statBuffPopupPrefab, end, Quaternion.identity);
        popup.Launch(evt.AbilityEffect, evt.Damage, () => done = true);
        // yield return new WaitUntil(() => done);
        
        var isBuff = evt.AbilityEffect is AbilityEffect.IncreaseAttack or AbilityEffect.IncreaseHealth or AbilityEffect.IncreaseAttackHealth;
        if (isBuff) EffectManager.Instance.Play(VfxType.Ability, end);
    }

    // ======== 입력 대기 ========
    
    public void ReceiveInput() => _inputPending = true;

    private IEnumerator WaitForInput()
    {
        _inputPending = false;
        yield return null;
        while (!_inputPending && !_isAutoPlay) yield return null;
    }

    private IEnumerator WaitForAdvance()
    {
        while (_isPaused) yield return null;

        if (_isAutoPlay)
        {
            yield return new WaitForSeconds(_cycleInterval);
        }
        else
        {
            yield return WaitForInput();
        }
    }
}
