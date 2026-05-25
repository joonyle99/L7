using UnityEngine;
using DG.Tweening;
using JoonyleGameDevKit;
using System.Collections.Generic;

public enum BgmType
{
    Prepare = 0,
    Battle,
}

public enum SfxType
{
    Summon_1 = 0,
    Summon_2,
    Summon_3,
    Summon_4,
    Sell,

    Merge = 10,
    LevelUp,

    Fight = 20,
    Die,

    UI_Summon = 30,
    UI_Select,
    UI_Drop,
    UI_Fail1,
    UI_Fail2,

    UI_Click = 35,
}

[System.Serializable]
public struct BgmEntry
{
    public BgmType type;
    public AudioClip clip;
}

[System.Serializable]
public struct SfxEntry
{
    public SfxType type;
    public AudioClip clip;
}

public class SoundManager : Singleton<SoundManager>, IManager, IGameStateListener<OutGameState>, IGameStateListener<InGameState>
{
    public int Priority => 10;

    [SerializeField] private BgmEntry[] _bgmEntries;
    [SerializeField] private SfxEntry[] _sfxEntries;

    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private float _bgmFadeDuration = 0.35f;

    private Dictionary<BgmType, BgmEntry> _bgmMap;
    private Dictionary<SfxType, SfxEntry> _sfxMap;

    private float _bgmVolume;
    private Tween _bgmFadeTween;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        _bgmFadeTween?.Kill();
    }

    public void Initialize()
    {
        _bgmMap = new Dictionary<BgmType, BgmEntry>();
        foreach (var entry in _bgmEntries)
            _bgmMap.TryAdd(entry.type, entry);

        _sfxMap = new Dictionary<SfxType, SfxEntry>();
        foreach (var entry in _sfxEntries)
            _sfxMap.TryAdd(entry.type, entry);

        _bgmVolume = _bgmSource.volume;
    }

    public void OnStateChanged(OutGameState prevState, OutGameState currState)
    {
        
    }

    public void OnStateChanged(InGameState prevState, InGameState currState)
    {
        
    }

    public void PlayBgm(BgmType type, float volume = -1f)
    {
        if (!_bgmMap.TryGetValue(type, out var entry) || entry.clip == null) return;
        if (_bgmSource.clip == entry.clip) return;

        if (volume >= 0f) _bgmVolume = volume;

        FadeBgmTo(0f);
        _bgmFadeTween.OnComplete(() =>
        {
            _bgmSource.clip = entry.clip;
            _bgmSource.Play();
            FadeBgmTo(_bgmVolume);
        });
    }

    public void PlaySfx(SfxType type, float volume = 0.5f)
    {
        if (_sfxMap.TryGetValue(type, out var entry) && entry.clip != null)
        {
            _sfxSource.PlayOneShot(entry.clip, volume);
        }
    }

    private void FadeBgmTo(float targetVolume)
    {
        _bgmFadeTween?.Kill();
        _bgmFadeTween = _bgmSource.DOFade(targetVolume, _bgmFadeDuration).SetUpdate(true);
    }

    private void SetSfxPaused(bool paused)
    {
        var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var source in sources)
        {
            if (source == _bgmSource) continue;
            if (paused) source.Pause();
            else source.UnPause();
        }
    }
}
