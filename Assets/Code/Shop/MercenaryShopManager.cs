using System.Linq;
using TMPro; // TextMeshPro 사용
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MercenaryShopManager : MonoBehaviour
{
    [Header("우측 상세 정보 UI")]
    public Image portraitImage;      // 동료 일러스트
    public TMP_Text nameText;        // 이름
    public TMP_Text levelText;       // 레벨
    public TMP_Text statText;        // 공격력 등 스탯 표시

    [Header("버튼 UI")]
    public Button upgradeButton;     // 고용 및 업그레이드 버튼
    public TMP_Text upgradePriceText;// 버튼 안의 가격 텍스트

    public Button equipButton;       // 장착/해제 버튼
    public TMP_Text equipButtonText; // "장착", "해제" 텍스트

    [Header("상점 설정")]
    public int maxEquipCount = 3;    // 최대 동료 장착 수 (스케치 기준 3명)

    [Header("하단 장착 슬롯 UI")]
    public Image[] equippedSlots; // 3개의 슬롯 이미지를 넣을 배열

    [Header("씬 이동 설정")]
    public string nextSceneName = "BattleScene"; // 이동할 전투 씬의 이름

    // 현재 상점 화면에서 클릭해서 보고 있는 용병의 정보
    private MercenaryInfo selectedInfo;
    private MercenarySaveData selectedSave;

    private void Start()
    {
        // 상점에 들어오면 도감의 첫 번째 용병을 기본으로 보여줍니다.
        if (DataManager.Instance.mercenaryDatabase.Count > 0)
        {
            SelectMercenary(DataManager.Instance.mercenaryDatabase[0].mercID);
        }
        // 처음 시작할 때 장착 슬롯 화면도 한번 그려줍니다.
        UpdateEquippedSlots();
    }

    // ==========================================
    // 1. 용병 선택 (좌측 목록 버튼을 누를 때 실행)
    // ==========================================
    public void SelectMercenary(string targetMercID)
    {
        // DataManager에서 고정 정보(도감)와 유저 진행도(세이브)를 모두 가져옵니다.
        selectedInfo = DataManager.Instance.mercenaryDatabase.FirstOrDefault(x => x.mercID == targetMercID);
        selectedSave = DataManager.Instance.mercenarySaveList.FirstOrDefault(x => x.mercID == targetMercID);

        if (selectedInfo != null && selectedSave != null)
        {
            UpdateUI(); // 화면 갱신
        }
        else
        {
            Debug.LogError($"[상점 오류] {targetMercID} 용병 데이터를 찾을 수 없습니다! 도감 세팅을 확인하세요.");
        }
    }

    // ==========================================
    // [추가] 현재 선택된 용병의 레벨에 따른 업그레이드 비용 계산
    // ==========================================
    private int GetNextUpgradeCost(MercenaryInfo info, int currentLevel)
    {
        // 배율 방식 (레벨이 오를수록 기하급수적으로 비싸짐)
        // 비용 = 기본비용 * (배율 ^ (현재레벨 - 1))
        return Mathf.RoundToInt(info.upgradeCostBase * Mathf.Pow(info.costMultiplier, currentLevel - 1));
    }

    // ==========================================
    // 2. 화면 갱신 (선택된 용병 정보 띄우기)
    // ==========================================
    private void UpdateUI()
    {
        if (selectedSave == null) return;

        // 1. 기본 정보 세팅
        if (selectedInfo.portraitIcon != null) portraitImage.sprite = selectedInfo.portraitIcon;
        nameText.text = selectedInfo.mercName;

        // 레벨에 따른 스탯 계산
        float currentAtk = selectedInfo.baseAttackPower + ((selectedSave.level - 1) * selectedInfo.attackGrowth);
        float currentRange = selectedInfo.baseDetectRadius + ((selectedSave.level - 1) * selectedInfo.detectRadiusGrowth);

        // [추가] 공격속도(연사 딜레이) 계산
        // 레벨이 오를수록 딜레이가 줄어들며, 최소 한계치(minFireRate) 밑으로는 내려가지 않도록 Mathf.Max로 방어합니다.
        float calculatedFireRate = selectedInfo.baseFireRate - ((selectedSave.level - 1) * selectedInfo.fireRateReduction);
        float currentFireRate = Mathf.Max(calculatedFireRate, selectedInfo.minFireRate);

        statText.text = $"ATK : {currentAtk}\nRange : {currentRange}\nAtk Delay : {currentFireRate:F2}";

        // 2. 고용 여부에 따른 UI 분기
        if (!selectedSave.isUnlocked)
        {
            levelText.text = "Locked";
            upgradePriceText.text = $"{selectedInfo.unlockCost} G\nHire";
            equipButton.gameObject.SetActive(false); // 고용 전에는 장착 버튼 숨김
        }
        else
        {
            levelText.text = $"Lv. {selectedSave.level}";

            // [적용됨] 위에서 만든 함수로 현재 비용 계산
            int currentUpgradeCost = GetNextUpgradeCost(selectedInfo, selectedSave.level);
            upgradePriceText.text = $"{currentUpgradeCost} G\nUpgrade";

            equipButton.gameObject.SetActive(true);
            equipButtonText.text = selectedSave.isEquipped ? "Unequip" : "Equip";
        }
    }

    // [추가] 하단 장착 슬롯 업데이트 함수
    // ==========================================
    private void UpdateEquippedSlots()
    {
        // 1. DataManager에서 '장착됨(isEquipped)' 상태인 용병들만 리스트로 뽑아옵니다.
        var equippedMercs = DataManager.Instance.mercenarySaveList.Where(m => m.isEquipped && m.isUnlocked).ToList();

        // 2. 3개의 슬롯을 하나씩 검사하며 이미지를 채워 넣습니다.
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (i < equippedMercs.Count)
            {
                // 장착된 동료가 있다면, 도감에서 정보를 찾아 초상화를 넣습니다.
                MercenaryInfo info = DataManager.Instance.mercenaryDatabase.FirstOrDefault(x => x.mercID == equippedMercs[i].mercID);
                if (info != null && info.portraitIcon != null)
                {
                    equippedSlots[i].sprite = info.portraitIcon;
                    equippedSlots[i].color = Color.white; // 이미지 뚜렷하게 (알파값 100%)
                }
            }
            else
            {
                // 빈 슬롯이라면 이미지를 비우고 투명하게(또는 까맣게) 처리합니다.
                equippedSlots[i].sprite = null;
                equippedSlots[i].color = new Color(0, 0, 0, 0.5f); // 예: 반투명한 검은색 빈칸 느낌
            }
        }
    }

    // ==========================================
    // 3. 업그레이드 (또는 고용) 버튼 클릭
    // ==========================================
    public void OnUpgradeClicked()
    {
        if (selectedSave == null) return;

        // [적용됨] 현재 레벨에 맞는 비용을 가져옵니다.
        int cost = selectedSave.isUnlocked ? GetNextUpgradeCost(selectedInfo, selectedSave.level) : selectedInfo.unlockCost;

        if (DataManager.Instance.coins >= cost)
        {
            DataManager.Instance.coins -= cost; // 돈 차감

            if (!selectedSave.isUnlocked)
            {
                selectedSave.isUnlocked = true; // 최초 고용 완료
            }
            else
            {
                selectedSave.level++; // 레벨업
            }

            DataManager.Instance.SaveGame(); // 🌟 데이터 영구 저장!
            UpdateUI(); // UI가 갱신되면서 다음 레벨의 비싼 가격이 바로 표시됩니다.

            // TODO: 성공 사운드 재생이나 파티클 이펙트 추가
        }
        else
        {
            Debug.Log("골드가 부족합니다!");
            // TODO: '골드 부족' 팝업창 띄우기
        }
    }

    // ==========================================
    // 4. 장착 / 해제 버튼 클릭
    // ==========================================
    public void OnEquipClicked()
    {
        if (selectedSave == null || !selectedSave.isUnlocked) return;

        if (selectedSave.isEquipped)
        {
            // 이미 장착 중이면 해제
            selectedSave.isEquipped = false;
        }
        else
        {
            // 새로 장착하려 할 때, 현재 장착된 용병이 몇 명인지 검사합니다.
            int currentEquippedCount = DataManager.Instance.mercenarySaveList.Count(m => m.isEquipped);

            if (currentEquippedCount < maxEquipCount)
            {
                selectedSave.isEquipped = true;
            }
            else
            {
                Debug.Log($"동료는 최대 {maxEquipCount}명까지만 데려갈 수 있습니다!");
                // TODO: '장착 인원 초과' 경고창 띄우기
                return;
            }
        }

        DataManager.Instance.SaveGame(); // 🌟 장착 상태 저장!
        UpdateUI();
        // [추가] 장착/해제를 누를 때마다 하단 슬롯 이미지도 즉시 다시 그려줍니다!
        UpdateEquippedSlots();
    }
    // ==========================================
    // [추가] 다음 스테이지로 이동 버튼 클릭
    // ==========================================
    public void OnNextStageClicked()
    {
        // 씬을 넘어가기 전에 혹시 모를 데이터를 위해 한 번 더 저장합니다.
        DataManager.Instance.SaveGame();

        // 다음 스테이지 인덱스로 올리고 싶다면 아래 주석을 해제하세요.
        // DataManager.Instance.currentStageIndex++; 

        SceneManager.LoadScene(nextSceneName);
    }
}