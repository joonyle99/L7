using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class InGameManager : MonoBehaviour
{
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private UIController _uiController;
    private GameStateController<InGameState> _gameStateController;
    private InputProvider _inputProvider;
    private GoldSystem _goldSystem;
    private TokenSystem _tokenSystem;

    [Space]

    [Header("0- Game Start")]
    [SerializeField] private AnimationEffect _gameStartEffectPrefab;

    [Space]

    [Header("1- Prepare Phase")]
    [SerializeField] private Canvas _prepareCanvas;
    [SerializeField] private GameObject _prepareStage;
    [SerializeField] private HeroDatabase _heroDatabase;
    [SerializeField] private RelicDatabase _relicDatabase;
    [SerializeField] private SummonTable _summonTable;
    [SerializeField] private SummonConfig _summonConfig;
    [SerializeField] private SquadConfig _squadConfig;
    [SerializeField] private GradeConfig _gradeConfig;
    [SerializeField] private RelicConfig _relicConfig;
    [SerializeField] private HeroSlotController _summonSlotController;
    [SerializeField] private HeroSlotController _squadSlotController;
    [SerializeField] private RelicSlotController _relicSlotController;
    [SerializeField] private PrepareKingController _prepareKingController;
    [SerializeField] private SummonTrail _summonTrailPrefab;
    [SerializeField] private StatBuffProjectile _statBuffProjectilePrefab;
    [SerializeField] private StatBuffPopup _statBuffPopupPrefab;
    [SerializeField] private HeroSellZone _heroSellZone;
    [SerializeField] private AnimationEffect[] _unlockEffectPrefabs;
    [SerializeField] private int _startGold = 20;
    [SerializeField] private int _sellPrice = 1;
    [SerializeField] private int _startToken = 3;
    [SerializeField] private float _prepareDuration = 30f;
    [SerializeField] private float _onSummonDelay = 0.6f;
    private PrepareManager _prepareManager;
    private SummonBenchManager _summonBenchManager;
    private SquadBenchManager _squadBenchManager;
    private RelicBenchManager _relicBenchManager;
    private PrepareTimer _prepareTimer;
    private Coroutine _onSummonCo;
    private Coroutine _onSoldCo;

    [Space]
    
    [Header("2 - Battle Phase")]
    [SerializeField] private Canvas _battleCanvas;
    [SerializeField] private GameObject _battleStage;
    [SerializeField] private CloudScroller[] _cloudScrollers;
    [SerializeField] private RoundTable _roundTable;
    [SerializeField] private BattleSlotController _playerSlotController;
    [SerializeField] private BattleSlotController _enemySlotController;
    [SerializeField] private BattleKingController _battleKingController;
    [SerializeField] private BattlePlayer _battlePlayer;
    [SerializeField] private RoundEndEffect _roundEndEffect;
    private BattleManager _battleManager;
    private RoundManager _roundManager;
    private BattleSimulator _battleSimulator;

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        _gameStateController.OnStateChanged -= OnStateChanged;
        _gameStateController.OnStateChanged -= _prepareTimer.OnStateChanged;
        _gameStateController.OnStateChanged -= _cameraController.OnStateChanged;
        _gameStateController.OnStateChanged -= _uiController.OnStateChanged;
        if (SoundManager.Instance != null) _gameStateController.OnStateChanged -= SoundManager.Instance.OnStateChanged;
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        
        _inputProvider?.Tick();
        _prepareManager?.Tick();
        // _prepareTimer?.Tick(deltaTime);
    }

    private void Initialize()
    {
        _gameStateController = new GameStateController<InGameState>();
        _cameraController.Initialize(null);
        _inputProvider = new InputProvider();
        _goldSystem = new GoldSystem(_startGold);
        _tokenSystem = new TokenSystem(_startToken);
        _summonBenchManager = new SummonBenchManager(_heroDatabase, _summonConfig, _summonTable);
        _squadBenchManager = new SquadBenchManager(_squadConfig);
        _relicBenchManager = new RelicBenchManager(_relicDatabase, _relicConfig);
        _prepareManager = new PrepareManager(_inputProvider);
        _prepareTimer = new PrepareTimer(_prepareDuration);
        _summonBenchManager.Initialize(_goldSystem.TrySpend, _uiController.SetSummonCostText, hero => OnHeroSummoned(hero));
        _squadBenchManager.Initialize();
        var infoPanel = _uiController.InfoPanel;
        _summonSlotController?.Initialize(
            _summonBenchManager,
            _gradeConfig,
            _summonTrailPrefab != null ? MakeSummonTrailEffect : null,
            (hero, worldPos) => { if (!_prepareManager.IsDragActive && !_prepareManager.SuppressHoverPanel) infoPanel.ShowHover(hero, worldPos); },
            () => { _prepareManager.SuppressHoverPanel = false; infoPanel.HideHover(); });
        _squadSlotController?.Initialize(
            _squadBenchManager,
            _gradeConfig,
            null,
            (hero, worldPos) => { if (!_prepareManager.IsDragActive && !_prepareManager.SuppressHoverPanel) infoPanel.ShowHover(hero, worldPos); },
            () => { _prepareManager.SuppressHoverPanel = false; infoPanel.HideHover(); });
        _relicSlotController.Initialize(_relicBenchManager);
        _prepareKingController.Initialize();
        _prepareManager.Initialize(
            _summonBenchManager, 
            _squadBenchManager, 
            _relicBenchManager, 
            _summonSlotController,
            _squadSlotController, 
            _relicSlotController, 
            _heroSellZone, 
            (hero, slotPos) => // onHeroSold
            {
                OnHeroSold(hero, slotPos);
                _goldSystem.AddGold(_sellPrice * hero.Level + hero.GoldStack);
                hero.GoldStack = 0;
            },
            (hero, isDrag, worldPos) => // onHeroSelected
            {
                if (hero == null) return;
                _heroSellZone.gameObject.SetActive(isDrag);
                _heroSellZone.SetPriceText(_sellPrice * hero.Level + hero.GoldStack);
                _squadSlotController.SetActivePoints(true, hero);
                if (!isDrag)
                {
                    infoPanel.ShowPinned(hero, worldPos);
                    SoundManager.Instance.PlaySfx(SfxType.UI_Select, 0.3f);
                }
            },
            () => // onHeroDeselected
            {
                _heroSellZone.gameObject.SetActive(false);
                _squadSlotController.SetActivePoints(false, null);
                infoPanel.Unpin();
            });
        _prepareTimer.Initialize(_uiController.SetTimerText, Battle);
        _heroSellZone.Initialize();
        _battleSimulator = new BattleSimulator();
        _battleManager = new BattleManager();
        _roundManager = new RoundManager(_roundTable);
        _playerSlotController.Initialize(_squadConfig.capacity, _gradeConfig);
        _enemySlotController.Initialize(_squadConfig.capacity, _gradeConfig);
        _battleManager.Initialize(_playerSlotController, _enemySlotController, _battleSimulator, _battlePlayer, RoundEnd, OnBattleEndAfter);
        _battlePlayer.Initialize(_battleManager, _battleKingController, _uiController.SetPlaybackState);
        _roundManager.Initialize(_uiController.SetRoundText);
        _uiController.Initialize(
            _startToken,
            () => // onSummonClicked
            {
                var summonResult = _summonBenchManager.TrySummon(_roundManager.CurrRound);
                if (summonResult == SummonResult.Success)
                {
                    SoundManager.Instance.PlaySfx(SfxType.UI_Summon);
                }
                else
                {
                    _uiController.ShowToast(summonResult == SummonResult.BenchFull ? "소환 슬롯이 가득찼습니다" : "골드가 부족합니다");
                    SoundManager.Instance.PlaySfx(SfxType.UI_Fail2);
                }
            },
            () => // onBattleClicked
            {
                var hasAnyHero = _squadBenchManager.Bench.Any(hero => hero != null);
                if (hasAnyHero)
                {
                    Battle();
                    SoundManager.Instance.PlaySfx(SfxType.UI_Click);
                }
                else
                {
                    _uiController.ShowToast("배치된 영웅이 없습니다");
                    SoundManager.Instance.PlaySfx(SfxType.UI_Fail2);
                }
            },
            () => { _battlePlayer.TogglePlayback(); _uiController.SetPlaybackState(_battlePlayer.IsPlaying); },
            () => { _battlePlayer.ToggleAuto(); _uiController.SetAutoState(_battlePlayer.IsAutoPlay); },
            () => { _battlePlayer.ToggleSpeed(); _uiController.SetSpeedState(_battlePlayer.IsSpeedUp); },
            _battlePlayer.ReceiveInput);
        _goldSystem.Initialize(_uiController.SetGoldText);
        _tokenSystem.Initialize(_uiController.SetTokens, _ => MatchEnd());
        if (SoundManager.Instance != null) _gameStateController.OnStateChanged += SoundManager.Instance.OnStateChanged;
        _gameStateController.OnStateChanged += _uiController.OnStateChanged;
        _gameStateController.OnStateChanged += _cameraController.OnStateChanged;
        _gameStateController.OnStateChanged += _prepareTimer.OnStateChanged;
        _gameStateController.OnStateChanged += OnStateChanged;
        _gameStateController.ChangeState(InGameState.Prepare);
    }

    // ========== 상태 전환 ==========

    public void Prepare() => _gameStateController.ChangeState(InGameState.Prepare);
    public void Battle()
    {
        _uiController.PlayLetterboxTransition(() =>
            _gameStateController.ChangeState(InGameState.Battle));
    }
    public void RoundEnd() => _gameStateController.ChangeState(InGameState.RoundEnd);
    public void MatchEnd() => _gameStateController.ChangeState(InGameState.MatchEnd);

    private void OnStateChanged(InGameState prev, InGameState curr)
    {
        _prepareStage?.SetActive(curr == InGameState.Prepare);
        _prepareCanvas?.gameObject.SetActive(curr == InGameState.Prepare);
        _battleStage?.SetActive(curr == InGameState.Battle || curr == InGameState.RoundEnd || curr == InGameState.MatchEnd);
        _battleCanvas?.gameObject.SetActive(curr == InGameState.Battle || curr == InGameState.RoundEnd || curr == InGameState.MatchEnd);

        var currRound = _roundManager.CurrRound;
        var roundData = _roundManager.GetCurrRoundData();
        var playerBench = _squadBenchManager.Bench.ToArray();
        var enemyBench = roundData.enemyHeroes.Select(entry => entry.data != null ? new HeroInstance(entry.data, entry.level) : null).ToArray();

        if (curr == InGameState.Prepare)
        {
            _goldSystem.SetGold(_startGold);
            _summonBenchManager.ResetCost();
            _prepareKingController.StartCycle();
            _uiController.SetEnemyBench(enemyBench);
            SoundManager.Instance.PlayBgm(BgmType.Prepare);

            Action<Action> prepareAnimationEffect = currRound == 1 ?
                callback => Instantiate(_gameStartEffectPrefab, Vector3.zero, Quaternion.identity).Play(callback)
                : currRound <= 3 ?
                callback => Instantiate(_unlockEffectPrefabs[currRound - 2], Vector3.zero, Quaternion.identity).Play(callback)
                : null;

            if (prepareAnimationEffect != null)
            {
                _prepareCanvas?.gameObject.SetActive(false);
                SetPrepareBlocked(true);

                prepareAnimationEffect(() =>
                {
                    _prepareCanvas?.gameObject.SetActive(true);
                    SetPrepareBlocked(false);

                    _uiController.PlayPendingTokenEffects();
                });
            }
            else
            {
                _uiController.PlayPendingTokenEffects();
            }
        }
        else if (curr == InGameState.Battle)
        {
            _summonBenchManager.ClearBench();
            foreach (var scroller in _cloudScrollers) scroller.ResetPosition();
            SoundManager.Instance.PlayBgm(BgmType.Battle, 0.1f);

            Debug.Log($"<color=#FF8C00>[Battle] ===== 라운드 {currRound} 시작 =====</color>");

            _uiController.SetPlaybackState(_battlePlayer.IsPaused);
            _uiController.SetAutoState(_battlePlayer.IsAutoPlay);
            _uiController.SetSpeedState(_battlePlayer.IsSpeedUp);

            _battleManager.StartBattle(playerBench, enemyBench);
        }
        else if (curr == InGameState.RoundEnd)
        {
            
        }
        else if (curr == InGameState.MatchEnd)
        {

        }
    }

    // ========== ... ==========

    private void OnBattleEndAfter(RoundOutcome roundOutcome)
    {
        // 토근 개수 처리
        _tokenSystem.ApplyRoundOutcome(roundOutcome);

        // 아직 라운드가 남아있으면 준비 페이즈로 이동
        if (_tokenSystem.PlayerTokens > 0 && _tokenSystem.EnemyTokens > 0)
        {
            _roundEndEffect.Play(roundOutcome, () =>
            {
                _roundManager.NextRound();
                Prepare();
            });
        }
        else
        {
            // TODO: 매치 종료 - 매치 연출 후, 로비로 이동
        }
    }

    // ========== 연출 ==========

    private bool MakeSummonTrailEffect(HeroInstance heroInstance, Vector3 endWorldPos, Action onComplete)
    {
        var trail = Instantiate(_summonTrailPrefab);
        _gradeConfig.TryGetGradeVisual(heroInstance.Data.Grade, out var visual);
        var startWorldPos = _uiController.GetSummonButtonWorldPos();
        trail.Launch(startWorldPos, endWorldPos, onComplete, visual.TrailColor);
        return true;
    }

    // ========== OnSummon / OnSold 어빌리티 ==========

    private void OnHeroSummoned(HeroInstance hero)
    {
        if (_onSummonCo != null) return;

        var effects = new List<(HeroInstance target, AbilityEffect effect, int value)>();
        PrepareAbilityExecutor.TryExecuteOnSummon(hero, _squadBenchManager.Bench,
            (target, effect, value) => effects.Add((target, effect, value)));

        if (effects.Count == 0) return;

        _onSummonCo = StartCoroutine(PrepareAbilityCo(effects, GetHeroWorldPos(hero), () => _onSummonCo = null));
    }

    private void OnHeroSold(HeroInstance hero, Vector3 slotPos)
    {
        if (_onSoldCo != null) return;

        var effects = new List<(HeroInstance target, AbilityEffect effect, int value)>();
        PrepareAbilityExecutor.TryExecuteOnSold(hero, _squadBenchManager.Bench,
            (target, effect, value) => effects.Add((target, effect, value)));

        if (effects.Count == 0) return;

        _onSoldCo = StartCoroutine(PrepareAbilityCo(effects, slotPos, () => _onSoldCo = null));
    }

    // startPos이 있으면 프로젝타일 발사 후 팝업, 없으면 팝업만
    private IEnumerator PrepareAbilityCo(
        List<(HeroInstance target, AbilityEffect effect, int value)> effects,
        Vector3 startPos,
        Action onDone)
    {
        SetPrepareBlocked(true);

        yield return new WaitForSeconds(_onSummonDelay);

        int remaining = effects.Count;

        foreach (var (target, effect, value) in effects)
        {
            var endPos = GetHeroWorldPos(target);
            var capturedTarget = target;
            var capturedEffect = effect;
            var capturedValue = value;
            var capturedEnd = endPos;

            var arrived = false;
            var projectile = Instantiate(_statBuffProjectilePrefab);
            projectile.Launch(startPos, endPos, effect, () => arrived = true);
            yield return new WaitUntil(() => arrived);

            var popup = Instantiate(_statBuffPopupPrefab, endPos, Quaternion.identity);
            popup.Launch(capturedEffect, capturedValue, () =>
            {
                remaining--;
                if (remaining == 0)
                {
                    SetPrepareBlocked(false);
                    onDone?.Invoke();
                }
            });

            RefreshHeroView(capturedTarget, punch: true);

            var isBuff = capturedEffect is AbilityEffect.IncreaseAttack or AbilityEffect.IncreaseHealth or AbilityEffect.IncreaseAttackHealth;
            if (isBuff) EffectManager.Instance.Play(VfxType.Ability, capturedEnd);
        }
    }

    // ========== ... ==========

    private void SetPrepareBlocked(bool blocked)
    {
        _prepareManager.IsLocked = blocked;
        _uiController.SetPrepareBlocked(blocked);
    }

    // ========== ... ==========

    private Vector3 GetHeroWorldPos(HeroInstance hero)
    {
        for (int i = 0; i < _summonBenchManager.Bench.Length; i++)
            if (_summonBenchManager.Bench[i] == hero) return _summonSlotController.GetItemWorldPosAt(i);
        for (int i = 0; i < _squadBenchManager.Bench.Length; i++)
            if (_squadBenchManager.Bench[i] == hero) return _squadSlotController.GetItemWorldPosAt(i);
        return Vector3.zero;
    }

    private void RefreshHeroView(HeroInstance hero, bool punch = false)
    {
        for (int i = 0; i < _squadBenchManager.Bench.Length; i++)
        {
            if (_squadBenchManager.Bench[i] == hero)
            {
                _squadSlotController.GetViewAt(i)?.Refresh(hero.Attack, hero.Health, punch);
                return;
            }
        }
        for (int i = 0; i < _summonBenchManager.Bench.Length; i++)
        {
            if (_summonBenchManager.Bench[i] == hero)
            {
                _summonSlotController.GetViewAt(i)?.Refresh(hero.Attack, hero.Health, punch);
                return;
            }
        }
    }
}
