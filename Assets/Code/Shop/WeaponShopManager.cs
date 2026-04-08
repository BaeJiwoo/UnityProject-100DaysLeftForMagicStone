using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

public class WeaponShopManager : MonoBehaviour
{
    [Header("우측 상세 정보 UI")]
    public Image weaponIconImage;
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text statText;

    [Header("재화 UI")]
    public TMP_Text coinText;

    [Header("버튼 UI")]
    public Button upgradeButton;
    public TMP_Text upgradePriceText;

    public Button equipButton;
    public TMP_Text equipButtonText;

    [Header("상점 설정")]
    public int maxEquipCount = 3;

    [Header("하단 장착 슬롯 UI")]
    public Image[] equippedSlots;

    [Header("씬 이동 설정")]
    public string nextSceneName = "BattleScene";

    private WeaponInfo selectedInfo;
    private WeaponSaveData selectedSave;

    private void Start()
    {
        UpdateEquippedSlots();

        // ==========================================
        // [수정] 상점 진입 시 '장착된 무기'를 먼저 찾아서 화면에 띄웁니다.
        // ==========================================
        var firstEquipped = DataManager.Instance.weaponSaveList.FirstOrDefault(w => w.isEquipped);

        if (firstEquipped != null)
        {
            SelectWeapon(firstEquipped.weaponID); // 장착된 무기가 있으면 그걸 보여줌
        }
        else if (DataManager.Instance.weaponDatabase.Count > 0)
        {
            SelectWeapon(DataManager.Instance.weaponDatabase[0].weaponID); // 만약 오류로 장착 무기가 없다면 도감 첫번째 무기 표시
        }
    }

    public void SelectWeapon(string targetWeaponID)
    {
        selectedInfo = DataManager.Instance.weaponDatabase.FirstOrDefault(x => x.weaponID == targetWeaponID);
        selectedSave = DataManager.Instance.weaponSaveList.FirstOrDefault(x => x.weaponID == targetWeaponID);

        if (selectedInfo != null && selectedSave != null) UpdateUI();
        else Debug.LogError($"[무기 상점 오류] {targetWeaponID} 데이터를 찾을 수 없습니다!");
    }

    private int GetNextUpgradeCost(WeaponInfo info, int currentLevel)
    {
        return Mathf.RoundToInt(info.upgradeCostBase * Mathf.Pow(info.costMultiplier, currentLevel - 1));
    }

    private void UpdateUI()
    {
        if (selectedSave == null) return;

        if (coinText != null) coinText.text = $"{DataManager.Instance.coins} G";

        if (selectedInfo.weaponIcon != null) weaponIconImage.sprite = selectedInfo.weaponIcon;
        nameText.text = selectedInfo.weaponName;

        int levelMultiplier = selectedSave.level - 1;

        float currentAtk = selectedInfo.baseDamage + (levelMultiplier * selectedInfo.damageGrowth);
        float currentSpeed = selectedInfo.baseSpeed + (levelMultiplier * selectedInfo.speedGrowth);
        float currentDelay = Mathf.Max(selectedInfo.baseFireDelay - (levelMultiplier * selectedInfo.fireDelayReduction), selectedInfo.minFireDelay);
        float currentMana = Mathf.Max(selectedInfo.baseManaCost - (levelMultiplier * selectedInfo.manaCostReduction), selectedInfo.minManaCost);
        float currentSpread = Mathf.Max(selectedInfo.baseSpreadAngleHip - (levelMultiplier * selectedInfo.spreadReduction), selectedInfo.minSpreadAngle);

        statText.text = $"ATK : {currentAtk:F1}\nSpeed : {currentSpeed:F1}\nDelay : {currentDelay:F2}s\nMana : {currentMana:F1}\nSpread : {currentSpread:F1}°";

        if (!selectedSave.isUnlocked)
        {
            levelText.text = "Locked";
            upgradePriceText.text = $"{selectedInfo.unlockCost} G\nBuy";
            equipButton.gameObject.SetActive(false);
        }
        else
        {
            levelText.text = $"Lv. {selectedSave.level}";

            int currentUpgradeCost = GetNextUpgradeCost(selectedInfo, selectedSave.level);
            upgradePriceText.text = $"{currentUpgradeCost} G\nUpgrade";

            equipButton.gameObject.SetActive(true);
            equipButtonText.text = selectedSave.isEquipped ? "Unequip" : "Equip";
        }
    }

    private void UpdateEquippedSlots()
    {
        var equippedWeapons = DataManager.Instance.weaponSaveList.Where(w => w.isEquipped && w.isUnlocked).ToList();

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (i < equippedWeapons.Count)
            {
                WeaponInfo info = DataManager.Instance.weaponDatabase.FirstOrDefault(x => x.weaponID == equippedWeapons[i].weaponID);
                if (info != null && info.weaponIcon != null)
                {
                    equippedSlots[i].sprite = info.weaponIcon;
                    equippedSlots[i].color = Color.white;
                }
            }
            else
            {
                equippedSlots[i].sprite = null;
                equippedSlots[i].color = new Color(0, 0, 0, 0.5f);
            }
        }
    }

    public void OnUpgradeClicked()
    {
        if (selectedSave == null) return;

        int cost = selectedSave.isUnlocked ? GetNextUpgradeCost(selectedInfo, selectedSave.level) : selectedInfo.unlockCost;

        if (DataManager.Instance.coins >= cost)
        {
            DataManager.Instance.coins -= cost;

            if (!selectedSave.isUnlocked) selectedSave.isUnlocked = true;
            else selectedSave.level++;

            DataManager.Instance.SaveGame();
            UpdateUI();
        }
        else
        {
            Debug.Log("골드가 부족합니다!");
        }
    }

    // ==========================================
    // [수정] 장착 / 해제 버튼 클릭 (최소 1개 제한 로직)
    // ==========================================
    public void OnEquipClicked()
    {
        if (selectedSave == null || !selectedSave.isUnlocked) return;

        // 현재 장착된 무기의 총 개수를 미리 계산해 둡니다.
        int currentEquippedCount = DataManager.Instance.weaponSaveList.Count(w => w.isEquipped);

        if (selectedSave.isEquipped)
        {
            // [추가] 장착 해제 시도 시, 남은 무기가 1개 이하라면 해제를 차단합니다.
            if (currentEquippedCount <= 1)
            {
                Debug.LogWarning("최소 1개의 무기는 반드시 장착해야 합니다!");
                // TODO: 유저에게 보여줄 UI 경고 팝업을 띄우는 코드를 여기에 추가할 수 있습니다.
                return;
            }

            selectedSave.isEquipped = false;
        }
        else
        {
            // 장착 시도 시, 최대치를 넘는지 확인합니다.
            if (currentEquippedCount < maxEquipCount)
            {
                selectedSave.isEquipped = true;
            }
            else
            {
                Debug.Log($"무기는 최대 {maxEquipCount}개까지만 장착할 수 있습니다!");
                return;
            }
        }

        DataManager.Instance.SaveGame();
        UpdateUI();
        UpdateEquippedSlots();
    }

    public void OnNextStageClicked()
    {
        DataManager.Instance.SaveGame();
        SceneManager.LoadScene(nextSceneName);
    }
}