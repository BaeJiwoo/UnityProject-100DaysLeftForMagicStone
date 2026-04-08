using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WeaponSlotUI
{
    public GameObject slotRoot; // 슬롯 전체 (무기가 없으면 아예 숨기기 위함)
    public Image weaponIcon;    // 무기 아이콘 이미지
    public Image cooldownGauge; // 쿨타임 게이지 이미지 (Fill Amount 사용)
}

public class BattleUIManager : MonoBehaviour
{
    [Header("시스템 연결")]
    public ManaSystem playerMana;  // 플레이어의 마나 시스템
    public MagicStone magicStone;  // 마법석 스크립트 (현재 체력을 실시간으로 가져오기 위함)
    public PlayerAttack playerAttack; // 👈 [추가] 플레이어의 공격 스크립트 연결

    [Header("UI 게이지 (슬라이더)")]
    public Slider manaSlider;

    [Header("UI 숫자 텍스트 (TextMeshPro)")]
    public TMP_Text manaText;          // 마력 텍스트 (예: 50 / 100)
    public TMP_Text magicStoneHpText;  // 마법석 체력 텍스트 (예: 800 / 1000)
    public TMP_Text coinText;          // 보유 코인 텍스트 (예: 1500 G)

    [Header("무기 슬롯 UI")]
    public WeaponSlotUI[] weaponSlots = new WeaponSlotUI[3];
    public float activeAlpha = 1f;    // 들고 있는 무기의 투명도 (100%)
    public float inactiveAlpha = 0.4f; // 안 들고 있는 무기의 투명도 (40%)

    [Range(0f, 1f)]
    [Tooltip("쿨타임이 도는 동안 게이지 이미지의 불투명도 (0.5 ~ 0.7 추천)")]
    public float gaugeOpacity = 0.6f;

    private bool _isWeaponInitialized = false; // 최초 1회 세팅을 위한 변수

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
        // 4. 무기 아이콘 및 쿨타임 UI 업데이트 실행
        HandleWeaponUI();
    }

    // [추가] 무기 UI 실시간 갱신 로직
    // ==========================================
    private void HandleWeaponUI()
    {
        if (playerAttack == null || playerAttack.weapons.Count == 0) return;

        if (!_isWeaponInitialized)
        {
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (i < playerAttack.weapons.Count)
                {
                    weaponSlots[i].slotRoot.SetActive(true);
                    weaponSlots[i].weaponIcon.sprite = playerAttack.weapons[i].icon;
                }
                else
                {
                    weaponSlots[i].slotRoot.SetActive(false);
                }
            }
            _isWeaponInitialized = true;
        }

        for (int i = 0; i < playerAttack.weapons.Count; i++)
        {
            if (i >= weaponSlots.Length) break;

            bool isActive = (i == playerAttack.currentWeaponIndex);

            // 1) 무기 아이콘 투명도 적용
            Color iconColor = weaponSlots[i].weaponIcon.color;
            iconColor.a = isActive ? activeAlpha : inactiveAlpha;
            weaponSlots[i].weaponIcon.color = iconColor;

            // 2) 쿨타임 비율 계산 (0: 쐈음 ~ 1: 장전완료)
            float cooldownRatio = 1f;

            if (isActive)
            {
                float delay = playerAttack.weapons[i].cachedFireDelay;
                float timeSinceLastFire = Time.time - playerAttack._lastFireTime;
                cooldownRatio = Mathf.Clamp01(timeSinceLastFire / delay);
            }

            weaponSlots[i].cooldownGauge.fillAmount = cooldownRatio;

            // ==========================================
            // [핵심 변경] 게이지 이미지 투명도(Alpha) 컨트롤
            // ==========================================
            Color gaugeColor = weaponSlots[i].cooldownGauge.color;

            if (cooldownRatio >= 1f)
            {
                // 장전 완료: 게이지를 완전히 투명하게 만들어 아이콘을 가리지 않게 함
                gaugeColor.a = 0f;
            }
            else
            {
                // 장전 중: 인스펙터에서 설정한 불투명도(gaugeOpacity)만큼만 덮어씌움
                gaugeColor.a = gaugeOpacity;
            }

            weaponSlots[i].cooldownGauge.color = gaugeColor;
        }
    }
}
