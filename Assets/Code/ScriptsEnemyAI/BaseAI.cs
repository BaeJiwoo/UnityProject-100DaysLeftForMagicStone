using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public enum AIState { Idle, Move, Action }

public abstract class BaseAI : MonoBehaviour
{
    [Header("스탯")]
    public float maxHealth = 50f;
    public float moveSpeed = 3f;
    protected float currentHealth;

    [Header("공격 설정")]
    public float attackPower = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    [HideInInspector] public float lastAttackTime = 0f;

    [Header("시각 효과 및 UI")]
    public SpriteRenderer sr;
    public GameObject healthBarCanvas;
    public Image healthFill;
    public GameObject floatingDamagePrefab;

    [Header("방어 및 피격 설정")]
    [Tooltip("넉백 저항력 (0 = 100% 다 밀려남, 1 = 절대 안 밀림)")]
    [Range(0f, 1f)]
    public float knockbackResistance = 0f;

    [Header("보상(코인) 설정")]
    public GameObject coinPrefab;

    [Tooltip("코인 드랍 확률 (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)]
    public float coinDropChance = 0.5f;

    [Tooltip("드랍될 코인의 최소~최대 개수")]
    public int minCoinCount = 1;
    public int maxCoinCount = 3;

    [Tooltip("코인 1개당 가치(가격)")]
    public int coinValuePerItem = 10;

    // 상태 관리 변수
    protected AIState currentState = AIState.Idle;
    protected bool isKnockedBack = false;
    protected bool isDead = false;

    // 전략 패턴 인터페이스 (자식 클래스에서 주입)
    protected IActionCondition actionCondition;
    protected IActionStrategy actionStrategy;

    protected virtual void Awake()
    {
        // 1. 객체가 씬에 생성되는 즉시(가장 먼저) 체력을 최대치로 채웁니다.
        currentHealth = maxHealth;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
       // currentHealth = maxHealth;
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);
       // if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    protected virtual void Update()
    {
        // 넉백 중이거나 죽었으면 상태 업데이트 중지
        if (isDead || isKnockedBack) return;

        UpdateState();
    }

    // [추가] 스포너가 적을 소환하자마자 호출할 스탯 초기화 함수
    public void ApplyStatMultipliers(float hpMult, float dmgMult)
    {
        // 1. 배율 적용
        maxHealth *= hpMult;
        attackPower *= dmgMult;

        // 2. 최대 체력이 변경되었으므로, 현재 체력도 꽉 채워줍니다. (매우 중요!)
        currentHealth = maxHealth;

        // 3. (선택) 체력바 UI가 있다면 시작할 때 100%로 갱신
        if (healthFill != null)
        {
            healthFill.fillAmount = 1f;
        }

        // [확인용 코드] 콘솔창에 몬스터 이름과 변동된 체력을 띄워줍니다!
        Debug.Log($"[{gameObject.name}] 스폰됨! |배율({hpMult}x) 적용 ➡ 최종 체력: {maxHealth}");
    }

    // FSM (상태 기계) 로직
    protected virtual void UpdateState()
    {
        // MagicStoneManager를 통해 타겟을 O(1)로 가져옴
        if (MagicStoneManager.Instance == null) return;
        Transform target = MagicStoneManager.Instance.StoneTransform;
        if (target == null) return;

        switch (currentState)
        {
            case AIState.Idle:
                // 타겟이 존재하므로 바로 이동 상태로 전환
                currentState = AIState.Move;
                break;

            case AIState.Move:
                // 조건이 충족되었는지 매 프레임 확인
                if (actionCondition != null && actionCondition.CanExecute(this, target))
                {
                    currentState = AIState.Action;
                }
                else
                {
                    MoveTowardsTarget(target);
                }
                break;

            case AIState.Action:
                // 조건이 충족되어 공격 등의 행동 실행
                if (actionStrategy != null)
                {
                    actionStrategy.Execute(this, target);
                }
                // 행동을 마친 후 다시 판별하기 위해 Idle 상태로 복귀
                currentState = AIState.Idle;
                break;
        }
    }

    protected virtual void MoveTowardsTarget(Transform target)
    {
        // 타겟 방향 바라보기
        sr.flipX = target.position.x < transform.position.x;

        // 이동
        Vector2 direction = (target.position - transform.position).normalized;
        transform.Translate(direction * moveSpeed * Time.deltaTime);
    }

    // ==========================================
    // 아래는 기존 코드와 동일한 공통 피격/사망/UI 로직입니다.
    // ==========================================

    public virtual void TakeDamage(float damage, Vector2 knockbackDir = default, float bulletKnockbackForce = 0f)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (healthBarCanvas != null && healthFill != null)
        {
            healthBarCanvas.SetActive(true);
            healthFill.fillAmount = currentHealth / maxHealth;
        }

        StartCoroutine(FlashRedRoutine());
        ShowFloatingDamage(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            float finalKnockback = bulletKnockbackForce * (1f - knockbackResistance);
            if (finalKnockback > 0f)
            {
                StartCoroutine(KnockbackRoutine(knockbackDir, finalKnockback));
            }
        }
    }

    protected IEnumerator KnockbackRoutine(Vector2 dir, float force)
    {
        isKnockedBack = true;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (isDead) yield break;
            transform.position += (Vector3)dir * force * Time.deltaTime / duration;
            elapsed += Time.deltaTime;
            yield return null;
        }
        isKnockedBack = false;
    }

    protected IEnumerator FlashRedRoutine()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    protected void ShowFloatingDamage(float damage)
    {
        if (floatingDamagePrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0, 1f, 0);
            GameObject textObj = Instantiate(floatingDamagePrefab, spawnPos, Quaternion.identity);
            textObj.SendMessage("Setup", damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // [추가] 적이 죽었음을 스테이지 매니저에 알림
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnEnemyDied();
        }

        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = -0.5f;
        }

        // [추가] 죽을 때 코인 드랍 함수 호출!
        DropCoin();

        StartCoroutine(FadeOutAndDestroy());
    }

    protected void DropCoin()
    {
        if (coinPrefab == null) return;

        // 1. 드랍 확률 체크
        if (Random.value <= coinDropChance)
        {
            // 2. 생성할 코인 개수 결정 (max에 +1을 해야 최대치까지 정상 포함됨)
            int dropCount = Random.Range(minCoinCount, maxCoinCount + 1);

            // 3. 결정된 개수만큼 반복해서 코인 생성
            for (int i = 0; i < dropCount; i++)
            {
                Vector3 spawnPos = transform.position + new Vector3(0, 0.5f, 0);
                GameObject spawnedCoin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);

                Coin coinScript = spawnedCoin.GetComponent<Coin>();
                if (coinScript != null)
                {
                    // 코인 1개의 가치를 넘겨줌
                    coinScript.Setup(coinValuePerItem);
                }
            }
        }
    }

    protected IEnumerator FadeOutAndDestroy()
    {
        float fadeDuration = 0.5f;
        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}