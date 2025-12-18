using UnityEngine;

public class MobileSkillCaster : MonoBehaviour
{
    public EnergySystem energy;
    public float skillCost = 20f;

     

    void Start()
    {
        if (energy == null) energy = FindObjectOfType<EnergySystem>();
    }

    // 給 UI Button OnClick 綁這個
    public void CastSkill()
    {
        if (energy.TryConsume(skillCost))
        {
            // TODO: 真正施放技能
            Debug.Log("Skill Cast!");
        }
        else
        {
            // 能量不足 或 封印中
            Debug.Log("Cannot cast (energy low or sealed).");
        }
    }
}
