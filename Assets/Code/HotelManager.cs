using TMPro;
using UnityEngine;

public class HotelManager : MonoBehaviour
{
    [Header("UI 연결: 보유 재화")]
    public TMP_Text coinText;

    [Header("UI 연결: 현재 능력치")]
    public TMP_Text speedStatText;
    public TMP_Text maxManaStatText;
    public TMP_Text manaRegenStatText;

    [Header("UI 연결: 업그레이드 비용")]
    public TMP_Text speedCostText;
    public TMP_Text maxManaCostText;
    public TMP_Text manaRegenCostText;

    [Header("업그레이드 비용 설정")]
    public int speedUpgradeCost = 100;
    public int maxManaUpgradeCost = 150;
    public int manaRegenUpgradeCost = 200;

    [Header("업그레이드 상승폭 설정")]
    public float speedIncreaseAmount = 0.5f;
    public float maxManaIncreaseAmount = 5f;
    public float manaRegenIncreaseAmount = 2f;

    void Start()
    {
        // 여관 씬이 시작될 때 UI를 한 번 갱신하여 최신 정보를 띄웁니다.
        UpdateUI();
    }

    public void UpgradeSpeed()
    {
        if (DataManager.Instance.coins >= speedUpgradeCost)
        {
            DataManager.Instance.coins -= speedUpgradeCost;
            DataManager.Instance.playerMaxSpeed += speedIncreaseAmount;

            // (선택) 업그레이드할 때마다 다음 구매 비용을 늘리고 싶다면 아래 주석을 해제하세요.
            // speedUpgradeCost += 50; 

            UpdateUI(); // 구매 완료 후 즉시 UI 최신화
        }
        else
        {
            Debug.Log("코인이 부족합니다!");
            // TODO: "코인이 부족합니다" 팝업창 띄우기 로직 추가 가능
        }
    }

    public void UpgradeMaxMana()
    {
        if (DataManager.Instance.coins >= maxManaUpgradeCost)
        {
            DataManager.Instance.coins -= maxManaUpgradeCost;
            DataManager.Instance.playerMaxMana += maxManaIncreaseAmount;

            // maxManaUpgradeCost += 50;

            UpdateUI();
        }
    }

    public void UpgradeManaRegen()
    {
        if (DataManager.Instance.coins >= manaRegenUpgradeCost)
        {
            DataManager.Instance.coins -= manaRegenUpgradeCost;
            DataManager.Instance.playerManaRegenRate += manaRegenIncreaseAmount;

            // manaRegenUpgradeCost += 50;

            UpdateUI();
        }
    }

    // ==========================================
    // UI에 현재 재화, 스탯, 비용을 표시하는 핵심 함수
    private void UpdateUI()
    {
        // DataManager가 씬에 없으면 에러가 나므로 예외 처리
        if (DataManager.Instance == null) return;

        // 1. 보유 코인 갱신
        if (coinText != null)
            coinText.text = "Coin: " + DataManager.Instance.coins.ToString() + " G";

        // 2. 현재 능력치 갱신 (ToString("F1")은 소수점 첫째 자리까지만 깔끔하게 보여줍니다)
        if (speedStatText != null)
            speedStatText.text = "Speed: " + DataManager.Instance.playerMaxSpeed.ToString("F1");
        if (maxManaStatText != null)
            maxManaStatText.text = "Max Mana: " + DataManager.Instance.playerMaxMana.ToString("F0");
        if (manaRegenStatText != null)
            manaRegenStatText.text = "Recovery Mana Speed: " + DataManager.Instance.playerManaRegenRate.ToString("F1");

        // 3. 업그레이드 비용 갱신
        if (speedCostText != null)
            speedCostText.text = speedUpgradeCost.ToString() + " G";
        if (maxManaCostText != null)
            maxManaCostText.text = maxManaUpgradeCost.ToString() + " G";
        if (manaRegenCostText != null)
            manaRegenCostText.text = manaRegenUpgradeCost.ToString() + " G";
    }
    // ==========================================

    public void GoToNextStage()
    {
        // [추가] 씬을 넘어가기(또는 게임을 끄기) 전에 변경된 능력치와 코인을 하드디스크에 확정 저장합니다.
        if (DataManager.Instance != null)
        {
            DataManager.Instance.SaveGame();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
    }
}
