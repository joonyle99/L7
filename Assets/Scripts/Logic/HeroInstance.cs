[System.Serializable]
public sealed class HeroInstance : ItemInstance<HeroData>
{
    public HeroInstance(HeroData data, int level = 1) : base(data)
    {
        Level = level;
        Attack = data.GetBaseAttack(level);
        Health = data.GetBaseHealth(level);
    }

    [field: UnityEngine.SerializeField] public int Level { get; private set; }
    [field: UnityEngine.SerializeField] public int Stack { get; private set; }
    [field: UnityEngine.SerializeField] public int Attack { get; set; }
    [field: UnityEngine.SerializeField] public int Health { get; set; }
    
    [field: UnityEngine.SerializeField] public int GoldStack { get; set; }

    public bool IsFullLevel => Level >= Data.MaxLevel;
    public bool CanMergeWith(HeroInstance src) => src != null && src.Data == Data && !src.IsFullLevel && !IsFullLevel;

    public void AbsorbStack(int amount)
    {
        // ** 중요 엣지 케이스 해결 **
        // src: Lv.2 Stack 1 (copy 4) -> Stack 4로 선형 변환
        // dst: Lv.2 Stack 1 (copy 4)
        // Stack 4 (src) + Stack 1 (dst)
        // Stack 5
        // src: Lv.2 Stack 5
        // loop 0 - Lv.2 Stack 5
        // loop 1 - Lv.3 Stack 3
        // Done
        // src: Lv.3 Stack 3
        // Clamp - src: Lv.3 Stack 2

        Stack += amount;

        while (Stack >= Data.MaxStack && Level < Data.MaxLevel)
        {
            Level++;
            Stack -= Data.MaxStack;
            Attack = Data.GetBaseAttack(Level);
            Health = Data.GetBaseHealth(Level);
        }
        
        Stack = Level >= Data.MaxLevel ? 0 : UnityEngine.Mathf.Min(Stack, Data.MaxStack);
    }
}
