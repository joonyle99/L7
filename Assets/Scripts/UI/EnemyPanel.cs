using UnityEngine;
using UnityEngine.UI;

public class EnemyPanel : UIPanel
{
    [SerializeField] private Image[] _heroIcons;

    public void SetEnemyBench(HeroInstance[] bench)
    {
        for (int idx = 0; idx < bench.Length; idx++)
        {
            var hero = bench[idx];
            _heroIcons[idx]?.gameObject.SetActive(hero != null);
            if (hero != null) _heroIcons[idx].sprite = hero.Data.Icon;
        }
    }
}
