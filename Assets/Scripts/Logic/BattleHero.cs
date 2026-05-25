using System;
using UnityEngine;

[Serializable]
public class BattleHero
{
    public HeroInstance Source; // 원본 참조 (능력, 데이터 조회용)
    public int SlotIdx; // Bench 상의 원래 슬롯 (0~4)
    public bool IsPlayer;

    public int CurrAttack; // 전투 중 변하는 값 (Source.CurrAttack에서 복사)
    public int CurrHealth; // 전투 중 변하는 값
    public bool IsFainted;   // 다음 기본 공격을 건너뜀
    public bool FaintImmune; // 기절 소모된 사이클엔 재발동 면역

    public bool IsAlive => CurrHealth > 0;

    public BattleHero(HeroInstance hero, int slotIdx, bool isPlayer)
    {
        Source = hero;
        SlotIdx = slotIdx;
        IsPlayer = isPlayer;
        CurrAttack = hero.Attack;
        CurrHealth = hero.Health;
    }
}
