using UnityEngine;

public class ManaSystem : MonoBehaviour
{
    [Header("마나 설정")]
    public float maxMana = 100f;      // 최대 마나
    public float currentMana;         // 현재 마나
    public float manaRegenRate = 5f;  // 초당 마나 회복량

    void Start()
    {
        currentMana = maxMana; // 시작할 때 꽉 채움
    }

    void Update()
    {
        // 마나 자동 회복 (최대치 안 넘게)
        if (currentMana < maxMana)
        {
            currentMana += manaRegenRate * Time.deltaTime;
            // 회복 후 최대치를 넘으면 최대치로 고정
            if (currentMana > maxMana) currentMana = maxMana;
        }
    }

    // 마나 사용 시도 (성공하면 true, 실패하면 false 반환)
    public bool UseMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            return true; // 사용 성공!
        }
        else
        {
            Debug.Log("마나가 부족합니다!");
            return false; // 사용 실패
        }
    }
}
