using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class BattleUIManager : MonoBehaviour
{
    [Header("시스템 연결")]
    public ManaSystem playerMana;  // 플레이어의 마나 시스템
    public MagicStone magicStone;  // 마법석 스크립트 (현재 체력을 실시간으로 가져오기 위함)

    [Header("UI 게이지 (슬라이더)")]
    public Slider manaSlider;

    [Header("UI 숫자 텍스트 (TextMeshPro)")]
    public TMP_Text manaText;          // 마력 텍스트 (예: 50 / 100)
    public TMP_Text magicStoneHpText;  // 마법석 체력 텍스트 (예: 800 / 1000)
    public TMP_Text coinText;          // 보유 코인 텍스트 (예: 1500 G)

    void Update()
    {
        // 1. 플레이어 마나 UI 업데이트 (슬라이더 + 텍스트)
        if (playerMana != null)
        {
            if (manaSlider != null)
            {
                manaSlider.value = playerMana.currentMana / playerMana.maxMana;
            }

            if (manaText != null)
            {
                // ToString("F0")를 사용하면 소수점을 잘라내고 깔끔한 정수로 보여줍니다.
                manaText.text = $"{playerMana.currentMana.ToString("F0")} / {playerMana.maxMana.ToString("F0")}";
            }
        }

        // 2. 마법석 체력 UI 텍스트 업데이트
        if (magicStone != null && magicStoneHpText != null)
        {
            // 실시간으로 깎이는 마법석의 체력을 표시합니다.
            magicStoneHpText.text = $"{magicStone.currentHealth.ToString("F0")} / {magicStone.maxHealth.ToString("F0")}";
        }

        // 3. 보유 코인 텍스트 업데이트 (전역 DataManager 활용)
        if (DataManager.Instance != null && coinText != null)
        {
            coinText.text = $"{DataManager.Instance.coins} G";
        }
    }
}
