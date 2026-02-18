using UnityEngine;
using UnityEngine.UI;
public class ManaUI : MonoBehaviour
{
    [Header("연결 대상")]
    public Slider manaSlider;      // UI 슬라이더
    public ManaSystem playerMana;  // 플레이어의 마나 시스템

    void Update()
    {
        // 플레이어 마나 시스템이 연결되어 있다면
        if (playerMana != null && manaSlider != null)
        {
            // 현재 마나 비율 계산 (0 ~ 1 사이 값)
            // 예: 현재 50 / 최대 100 = 0.5 (절반)
            float ratio = playerMana.currentMana / playerMana.maxMana;

            // 슬라이더에 적용
            manaSlider.value = ratio;
        }
    }
}
