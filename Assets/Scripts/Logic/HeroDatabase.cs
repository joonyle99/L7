using System;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct GradeHeroEntry
{
    public HeroGrade grade;
    public List<HeroData> heroes;
}

[CreateAssetMenu(fileName = "HeroDatabase", menuName = "Lucky/HeroDatabase")]
public class HeroDatabase : ScriptableObject
{
    [SerializeField] private List<GradeHeroEntry> _gradeHeroEntries;

    private List<HeroData> _allHeroes;

    public List<HeroData> GetHeroesByGrade(HeroGrade grade)
    {
        foreach (var gradeHeroEntry in _gradeHeroEntries)
        {
            if (gradeHeroEntry.grade == grade)
            {
                return gradeHeroEntry.heroes;
            }
        }

        return null;
    }

    public List<HeroData> GetAllHeroes()
    {
        if (_allHeroes != null && _allHeroes.Count > 0)
        {
            return _allHeroes;
        }

        _allHeroes = new List<HeroData>();

        foreach (var entry in _gradeHeroEntries)
        {
            _allHeroes.AddRange(entry.heroes);
        }

        return _allHeroes;
    }
}
