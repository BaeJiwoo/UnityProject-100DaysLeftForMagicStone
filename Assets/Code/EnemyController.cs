using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class EnemyController : MonoBehaviour
{
    public enum TargetType { Player, MagicStone, Ally }

    [Header("타겟 설정")]
    public TargetType currentTargetType = TargetType.Player;
    private Transform target;

    [Header("스탯")]
    public float maxHealth = 50f;
    public float moveSpeed = 3f;
    private float currentHealth;

    [Header("공격 설정")]
    public float attackPower = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    [Header("시각 효과 및 UI")]
    public SpriteRenderer sr;
    public GameObject healthBarCanvas;
    public Image healthFill;
    public GameObject floatingDamagePrefab;

    [Header("방어 및 피격 설정")] // [추가]
    [Tooltip("넉백 저항력 (0 = 100% 다 밀려남, 0.5 = 절반만 밀려남, 1 = 절대 안 밀림)")]
    [Range(0f, 1f)]
    public float knockbackResistance = 0f;

    // [추가] 넉백 상태 확인 변수
    private bool isKnockedBack = false;
    // [추가] 사망 상태 확인 변수
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        FindTarget();
    }

    void FindTarget()
    {
        string targetTag = "";
        switch (currentTargetType)
        {
            case TargetType.Player: targetTag = "Player"; break;
            case TargetType.MagicStone: targetTag = "MagicStone"; break;
            case TargetType.Ally: targetTag = "Ally"; break;
        }

        GameObject foundTarget = GameObject.FindGameObjectWithTag(targetTag);
        if (foundTarget != null) target = foundTarget.transform;
    }

    void Update()
    {

        // [추가] 이미 죽은 상태라면 아무 행동도 하지 않음
        if (isDead) return;

        if (target == null)
        {
            FindTarget();
            return;
        }

        // [추가] 넉백 당하는 중이 아닐 때만 타겟 바라보기 및 추적 실행
        if (!isKnockedBack)
        {
            sr.flipX = target.position.x < transform.position.x;

            // ==========================================
            // [핵심 변경] 중심점이 아닌 '타겟의 표면(가장자리)'까지의 거리 계산
            float distance = 0f;
            Collider2D targetCol = target.GetComponent<Collider2D>();

            if (targetCol != null)
            {
                // ClosestPoint: 내 위치에서 타겟 콜라이더 표면 중 가장 가까운 점을 찾아줍니다.
                Vector2 edgePoint = targetCol.ClosestPoint(transform.position);
                distance = Vector2.Distance(transform.position, edgePoint);
            }
            else
            {
                // 타겟에 콜라이더가 없다면 기존처럼 중심점 거리 사용
                distance = Vector2.Distance(transform.position, target.position);
            }
            // ==========================================

            if (distance > attackRange)
            {
                MoveTowardsTarget();
            }
            else
            {
                AttackTarget();
            }
        }
    }

    void MoveTowardsTarget()
    {
        Vector2 direction = (target.position - transform.position).normalized;
        transform.Translate(direction * moveSpeed * Time.deltaTime);
    }

    void AttackTarget()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            target.SendMessage("TakeDamage", attackPower, SendMessageOptions.DontRequireReceiver);
        }
    }

    // [변경] 매개변수로 넉백 방향과 힘을 추가로 받도록 수정
    // [변경] TakeDamage 함수 수정
    public void TakeDamage(float damage, Vector2 knockbackDir = default, float bulletKnockbackForce = 0f)
    {
        currentHealth -= damage;

        if (healthBarCanvas != null && healthFill != null)
        {
            healthBarCanvas.SetActive(true);
            healthFill.fillAmount = currentHealth / maxHealth;
        }

        StartCoroutine(FlashRedRoutine());
        ShowFloatingDamage(damage);

        // ==========================================
        // [수정된 부분] 체력이 0 이하가 되어 죽는다면 넉백을 실행하지 않고 바로 사망 처리
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 아직 살아있을 때만 넉백 실행
            float finalKnockback = bulletKnockbackForce * (1f - knockbackResistance);
            if (finalKnockback > 0f)
            {
                StartCoroutine(KnockbackRoutine(knockbackDir, finalKnockback));
            }
        }
        // ==========================================
    }


    // [추가] 넉백 효과를 주는 코루틴
    IEnumerator KnockbackRoutine(Vector2 dir, float force)
    {
        isKnockedBack = true; // 넉백 시작 (이동 정지)

        float duration = 0.15f; // 뒤로 밀려나는 시간 (짧을수록 타격감이 좋습니다)
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // [핵심 추가] 넉백으로 밀려나는 도중에 다른 총알을 맞고 죽었다면, 넉백 강제 종료!
            if (isDead) yield break;
            // 날아온 총알의 방향(dir)으로 force만큼 밀어냅니다.
            transform.position += (Vector3)dir * force * Time.deltaTime / duration;
            elapsed += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        isKnockedBack = false; // 넉백 종료 (다시 추적 시작)
    }

    IEnumerator FlashRedRoutine()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    void ShowFloatingDamage(float damage)
    {
        if (floatingDamagePrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0, 1f, 0);
            GameObject textObj = Instantiate(floatingDamagePrefab, spawnPos, Quaternion.identity);
            textObj.GetComponent<FloatingDamage>().Setup(damage);
        }
    }

    void Die()
    {
        // 이미 죽기 시작했다면 중복 실행 방지
        if (isDead) return;
        isDead = true;

        // 1. 체력바 숨기기
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);

        // 2. 다른 총알에 또 맞거나 길을 막지 않도록 콜라이더(충돌체) 끄기
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // [추가] 중력 반전 연출 (위로 떠오르기)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 넉백으로 밀려나던 속도나 떨어지던 속도를 완전히 0으로 멈춤
            rb.linearVelocity = Vector2.zero;

            // 중력을 마이너스로 바꾸어 위로 서서히 떠오르게 만듭니다.
            // (-0.5f ~ -1.5f 사이에서 하늘로 올라가는 속도를 취향껏 조절해 보세요!)
            rb.gravityScale = -0.5f;
        }

        // 3. 서서히 투명해지는 코루틴 시작
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        float fadeDuration = 0.5f; // 사라지는 데 걸리는 시간 (0.5초)
        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            // Lerp를 사용하여 알파(투명도)값을 1에서 0으로 부드럽게 줄임
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // 투명해지기가 완전히 끝나면 오브젝트 삭제
        Destroy(gameObject);
    }
    private void OnDrawGizmosSelected()
    {
        // 1. 선 색상을 빨간색으로 설정
        Gizmos.color = Color.red;

        // 2. 적의 현재 위치(transform.position)를 중심으로, attackRange 만큼의 반지름을 가진 원을 그립니다.
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
