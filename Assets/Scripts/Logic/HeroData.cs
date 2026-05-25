using UnityEngine;

public enum HeroGrade
{
    Normal, // 일반
    Rare, // 레어
    Epic, // 영웅
    Legendary, // 전설
    Myth, // 신화
}

[CreateAssetMenu(fileName = "HeroData", menuName = "Lucky/HeroData")]
public class HeroData : ItemData
{
    public int[] BaseAttacks; // 레벨별 기본 공격력 (index 0 = Lv1, 1 = Lv2, 2 = Lv3)
    public int[] BaseHealths; // 레벨별 기본 체력

    public HeroGrade Grade;
    public AbilityData Ability;
    public Sprite[] Sprites;
    public int MaxLevel = 3;
    public int MaxStack = 3;

    public int GetBaseAttack(int level) => BaseAttacks != null && BaseAttacks.Length > 0 ? BaseAttacks[UnityEngine.Mathf.Clamp(level - 1, 0, BaseAttacks.Length - 1)] : 0;
    public int GetBaseHealth(int level) => BaseHealths != null && BaseHealths.Length > 0 ? BaseHealths[UnityEngine.Mathf.Clamp(level - 1, 0, BaseHealths.Length - 1)] : 0;
}
