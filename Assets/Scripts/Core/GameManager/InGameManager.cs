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
    [SerializeField] private HeroSellZone _heroSellZone;
    [SerializeField] private UnlockEffect[] _unlockEffectPrefabs;
    [SerializeField] private int _startGold = 20;
    [SerializeField] private int _sellPrice = 1;
    [SerializeField] private int _startToken = 3;
    [SerializeField] private float _prepareDuration = 30f;
    private PrepareManager _prepareManager;
    private SummonBenchManager _summonBenchManager;
    private SquadBenchManager _squadBenchManager;
    private RelicBenchManager _relicBenchManager;
    private PrepareTimer _prepareTimer;

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
        _summonBenchManager.Initialize(_goldSystem.TrySpend, _uiController.SetSummonCostText);
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
            hero =>
            {
                _goldSystem.AddGold(_sellPrice * hero.Level + hero.GoldStack);
                hero.GoldStack = 0;
            },
            (hero, isDrag, worldPos) =>
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
            () =>
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
            () =>
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
            () =>
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

        if (curr == InGameState.Prepare)
        {
            _goldSystem.SetGold(_startGold);
            _summonBenchManager.ResetCost();
            _prepareKingController.StartCycle();
            SoundManager.Instance.PlayBgm(BgmType.Prepare);

            if (currRound == 2 || currRound == 3)
            {
                _prepareStage?.SetActive(false);
                _prepareCanvas?.gameObject.SetActive(false);

                var unlockEffect = Instantiate(_unlockEffectPrefabs[currRound - 2], Vector3.zero, Quaternion.identity);
                unlockEffect.Play(() =>
                {
                    _prepareStage?.SetActive(true);
                    _prepareCanvas?.gameObject.SetActive(true);

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

            var playerBench = _squadBenchManager.Bench.ToArray();
            var roundData = _roundManager.GetCurrRoundData();
            var enemyBench = roundData.enemyHeroes.Select(entry => entry.data != null ? new HeroInstance(entry.data, entry.level) : null).ToArray();

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
        _tokenSystem.ApplyRoundOutcome(roundOutcome);

        if (_tokenSystem.PlayerTokens > 0 && _tokenSystem.EnemyTokens > 0)
        {
            PlayRoundEndSequence(() =>
            {
                _roundManager.NextRound();
                Prepare();
            });
        }
    }

    private void PlayRoundEndSequence(Action onComplete)
    {
        StartCoroutine(RoundEndSequenceCoroutine(onComplete));
    }

    private IEnumerator RoundEndSequenceCoroutine(Action onComplete)
    {
        // TODO: 라운드 종료 연출
        yield return new WaitForSeconds(2f);
        onComplete?.Invoke();
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
}
