using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 특정 라운드부터 해당 등급이 소환 풀에 해금됨
/// </summary>
[System.Serializable]
public struct GradeUnlockEntry
{
    public HeroGrade grade;
    public int unlockRound;
}

/// <summary>
/// 등급별 소환 확률 (예: 일반 30%, 레어 70%)
/// </summary>
[System.Serializable]
public struct GradeChanceEntry
{
    public HeroGrade grade;
    [Range(0f, 1f)] public float chance; // 0~1 사이 확률 (0.3 = 30%)
}

/// <summary>
/// 라운드별 소환 확률 테이블 (해당 라운드부터 이 확률 적용)
/// </summary>
[System.Serializable]
public struct GradeRoundEntry
{
    public int round; // 적용 시작 라운드
    public GradeChanceEntry[] gradeChanceEntries; // 등급별 확률 목록 (합계 1.0)
}

[CreateAssetMenu(fileName = "SummonTable", menuName = "Lucky/SummonTable")]
public class SummonTable : ScriptableObject
{
    [SerializeField] private GradeUnlockEntry[] _unlockEntries = {};
    [SerializeField] private GradeRoundEntry[] _roundEntries = {};
    [SerializeField] private int _maxRound = 3;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_roundEntries == null) return;

        foreach (var roundEntry in _roundEntries)
        {
            var chanceSum = 0f;
            var chanceEntries = roundEntry.gradeChanceEntries;

            foreach (var chance in chanceEntries)
            {
                chanceSum += chance.chance;
            }

            if (Mathf.Abs(chanceSum - 1f) > 0.01f)
            {
                Debug.LogWarning($"라운드 {roundEntry.round} 확률 합: {chanceSum} (1.0이어야 함)");
            }

            foreach (var chance in chanceEntries)
            {
                if (!IsUnlockedGrade(chance.grade, roundEntry.round))
                {
                    Debug.LogWarning($"라운드 {roundEntry.round} 항목에 아직 해금되지 않는 등급 포함: {chance.grade}");
                }
            }
        }
    }
#endif

    // =========== ... ===========

    public bool IsUnlockedGrade(HeroGrade grade, int round)
    {
        foreach (var entry in _unlockEntries)
        {
            if (entry.grade == grade)
            {
                return round >= entry.unlockRound;
            }
        }

        return false;
    }

    public HeroGrade[] GetUnlockedGrades(int round)
    {
        return _unlockEntries
            .Where(e => round >= e.unlockRound)
            .Select(e => e.grade)
            .ToArray();
    }

    // =========== ... ===========

    public GradeChanceEntry[] GetGradeChanceEntries(int currRound)
    {
        // currRound : 5
        // roundEntry.round : 6
        // -> 사용 불가능 (1 ~ 5까지 사용가능)
        
        GradeRoundEntry bestRoundEntry;

        // 최대 라운드에 도달 시, round 값이 가장 큰 엔트리를 사용한다
        if (currRound >= _maxRound)
        {
            bestRoundEntry = _roundEntries[0];

            foreach (var roundEntry in _roundEntries)
            {
                if (roundEntry.round > bestRoundEntry.round)
                    bestRoundEntry = roundEntry;
            }
        }
        else
        {
            bestRoundEntry = _roundEntries[0];

            // currRound 이하인 것 중 round 값이 가장 큰 엔트리 탐색
            var bestRound = int.MinValue;
            foreach (var roundEntry in _roundEntries)
            {
                if (currRound < roundEntry.round) continue;
                
                if (bestRound < roundEntry.round)
                {
                    bestRoundEntry = roundEntry;
                    bestRound = roundEntry.round;
                }
            }
        }

        var filtered = bestRoundEntry.gradeChanceEntries
            .Where(e => IsUnlockedGrade(e.grade, currRound))
            .ToArray();

        // Debug.Log($"[SummonTable] 라운드 {currRound} 해금 등급: {string.Join(", ", filtered.Select(e => e.grade))}");

        // 해금된 등급이 없거나 모든 chance가 0이면 정규화 불가 — 데이터 오류
        if (filtered == null || filtered.Length == 0) return null;
        var total = filtered.Sum(e => e.chance);
        if (total <= 0f) return null;

        return filtered.Select(e => new GradeChanceEntry
        {
            grade = e.grade,
            chance = e.chance / total
        }).ToArray();
    }
}
