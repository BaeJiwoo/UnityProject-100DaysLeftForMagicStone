using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
public class MagicStone : MonoBehaviour
{
    [Header("스탯")]
    public float maxHealth = 1000f;
    //private float currentHealth;
    public float currentHealth { get; private set; }

    [Header("UI 설정")]
    public GameObject healthBarCanvas; // 체력바 캔버스
    public Image healthFill;           // 체력바 채우기 (빨간색)

    [Header("크리스탈 이미지 상태")]
    public SpriteRenderer sr;
    [Tooltip("체력 76% ~ 100%")] public Sprite sprite100;
    [Tooltip("체력 51% ~ 75%")] public Sprite sprite75;
    [Tooltip("체력 26% ~ 50%")] public Sprite sprite50;
    [Tooltip("체력 1% ~ 25%")] public Sprite sprite25;
    [Tooltip("체력 0% (파괴됨)")] public Sprite sprite0;

    [Header("시각 효과")]
    public GameObject floatingDamagePrefab;

    void Start()
    {
        // 시작할 때 DataManager에 저장된 체력을 불러옵니다.
        if (DataManager.Instance != null)
        {
            currentHealth = DataManager.Instance.magicStoneCurrentHP;
            maxHealth = DataManager.Instance.magicStoneMaxHP;
        }
        else
        {
            currentHealth = maxHealth; // DataManager가 없을 때(테스트용) 예외 처리
        }

        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (healthFill != null) healthFill.fillAmount = currentHealth / maxHealth;
        UpdateCrystalState();

        /*currentHealth = maxHealth;
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // 시작 시 체력바 업데이트 및 100% 이미지 적용
        if (healthFill != null) healthFill.fillAmount = 1f;
        UpdateCrystalState();*/
    }

    // 적의 AttackTarget() 내 SendMessage("TakeDamage", ...) 에서 자동으로 호출됩니다.
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return; // 이미 파괴되었다면 무시

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        ShowFloatingDamage(damage);
        // 체력바 UI 업데이트
        if (healthBarCanvas != null && healthFill != null)
        {
            healthBarCanvas.SetActive(true);
            healthFill.fillAmount = currentHealth / maxHealth;
        }

        // 체력 퍼센트에 맞춰 이미지 변경
        UpdateCrystalState();

        if (currentHealth <= 0)
        {
            DestroyCrystal();
        }
    }

    //데미지 텍스트를 생성하는 함수(적 스크립트와 동일한 원리)
    void ShowFloatingDamage(float damage)
    {
        if (floatingDamagePrefab != null)
        {
            // 크리스탈 머리 위로 생성 (크리스탈 크기가 크다면 Y값을 1.5f나 2f로 올려주세요)
            Vector3 spawnPos = transform.position + new Vector3(0, 1.5f, 0);
            GameObject textObj = Instantiate(floatingDamagePrefab, spawnPos, Quaternion.identity);

            // 데미지 수치 전달
            textObj.GetComponent<FloatingDamage>().Setup(damage);
        }
    }

    // 체력 비율(%)을 계산하여 스프라이트를 교체하는 함수
    void UpdateCrystalState()
    {
        float hpPercentage = currentHealth / maxHealth;

        if (hpPercentage > 0.75f)
        {
            if (sprite100 != null) sr.sprite = sprite100;
        }
        else if (hpPercentage > 0.50f)
        {
            if (sprite75 != null) sr.sprite = sprite75;
        }
        else if (hpPercentage > 0.25f)
        {
            if (sprite50 != null) sr.sprite = sprite50;
        }
        else if (hpPercentage > 0f)
        {
            if (sprite25 != null) sr.sprite = sprite25;
        }
        else
        {
            if (sprite0 != null) sr.sprite = sprite0;
        }
    }

    void DestroyCrystal()
    {
        Debug.Log("크리스탈이 파괴되었습니다! 게임 오버!");

        // 파괴되었을 때 오브젝트를 아예 삭제하기보다는
        // 0% (깨진) 이미지를 남겨두고 충돌체만 꺼버리는 것이 연출상 더 좋습니다.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // TODO: 여기에 게임 오버 창 띄우기 로직 추가
    }
}