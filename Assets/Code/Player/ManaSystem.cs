using UnityEngine;

public class ManaSystem : MonoBehaviour
{
    [Header("마나 상태 (DataManager에서 자동 적용됨)")]
    // [HideInInspector]를 붙이면 유니티 에디터 창에서 이 변수들이 사라집니다.
    // 이제 실수로 이중으로 수치를 입력하여 꼬이는 일이 발생하지 않습니다.
    [HideInInspector] public float maxMana;
    [HideInInspector] public float manaRegenRate;

    // 현재 마나는 인스펙터에서 실시간으로 깎이는 걸 확인하기 위해 남겨둡니다.
    public float currentMana;

    void Start()
    {
        // DataManager가 존재하면 무조건 DataManager의 수치를 덮어씌웁니다.
        if (DataManager.Instance != null)
        {
            maxMana = DataManager.Instance.playerMaxMana;
            manaRegenRate = DataManager.Instance.playerManaRegenRate;
        }
        else
        {
            // DataManager가 없는 씬에서 단독 테스트를 할 때를 위한 예외 방어 코드
            maxMana = 100f;
            manaRegenRate = 15f;
        }

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