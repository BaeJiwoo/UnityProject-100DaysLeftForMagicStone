using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public enum AIState { Idle, Move, Action }

public abstract class BaseAI : MonoBehaviour
{
    [Header("스탯")]
    public float maxHealth = 50f;
    public float moveSpeed = 3f;
    [HideInInspector] public float currentHealth;

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

    

    [Tooltip("드랍될 코인의 최소~최대 개수")]
    public int minCoinCount = 1;
    public int maxCoinCount = 3;

    [Tooltip("코인 1개당 가치(가격)")]
    public int coinValuePerItem = 10;
    protected float currentCoinMultiplier = 1.0f;
    // 상태 관리 변수
    protected AIState currentState = AIState.Idle;
    protected bool isKnockedBack = false;
    [HideInInspector] public bool isDead = false;

    [Header("타겟 설정")]
    public TargetType targetType = TargetType.MagicStone;

    // 전략 패턴 인터페이스 (자식 클래스에서 주입)
    protected IActionCondition actionCondition;
    protected IActionStrategy actionStrategy;
    protected ITargetFinder targetFinder;       // [추가] 누구를 찾을 것인가?

    // [추후 애니메이션용 캐싱 변수]
    // protected Animator anim;

    protected virtual void Awake()
    {
        // 1. 객체가 씬에 생성되는 즉시(가장 먼저) 체력을 최대치로 채웁니다.
        currentHealth = maxHealth;

        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // anim = GetComponent<Animator>(); // 애니메이션 추가 시 주석 해제
    }

    protected virtual void Start()
    {
       // currentHealth = maxHealth;
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);
        // if (sr == null) sr = GetComponent<SpriteRenderer>();
        // [추가] 인스펙터에서 선택한 타겟 타입(Enum)에 따라 
        // 알맞은 타겟 탐색 전략을 자동으로 주입(장착)해 줍니다.
        switch (targetType)
        {
            case TargetType.MagicStone:
                targetFinder = new MagicStoneTargetFinder();
                break;
            case TargetType.Player:
                targetFinder = new PlayerTargetFinder();
                break;
            case TargetType.EnemyAlly:
                targetFinder = new AllyTargetFinder();
                break;
        }
    }

    protected virtual void Update()
    {
        // 넉백 중이거나 죽었으면 상태 업데이트 중지
        if (isDead || isKnockedBack) return;

        UpdateState();
    }

    // [추가] 스포너가 적을 소환하자마자 호출할 스탯 초기화 함수
    public void ApplyStatMultipliers(float hpMult, float dmgMult, float coinMult)
    {
        // 1. 배율 적용
        maxHealth *= hpMult;
        attackPower *= dmgMult;
        currentCoinMultiplier = coinMult; // 코인 배율 저장

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

    // [추가] 거리 계산을 쉽게 하기 위한 도우미 함수
    // ==========================================
    protected float GetDistanceFromTarget(Transform target)
    {
        Collider2D targetCol = target.GetComponent<Collider2D>();
        if (targetCol != null)
        {
            Vector2 edgePoint = targetCol.ClosestPoint(transform.position);
            return Vector2.Distance(transform.position, edgePoint);
        }
        return Vector2.Distance(transform.position, target.position);
    }

    // FSM (상태 기계) 로직
    protected virtual void UpdateState()
    {
        Transform target = null;
        if (targetFinder != null) target = targetFinder.GetTarget(this);

        // 1. 타겟이 없으면 무조건 대기
        if (target == null)
        {
            currentState = AIState.Idle;
            // if (anim != null) anim.SetBool("isMoving", false);
            return;
        }

        float distance = GetDistanceFromTarget(target);
        bool isInRange = distance <= attackRange;

        // 2. 상태 기계 로직
        switch (currentState)
        {
            case AIState.Idle:
                // [애니메이션] 멈춤 상태 재생
                // if (anim != null) anim.SetBool("isMoving", false);

                if (!isInRange)
                {
                    // 타겟이 범위 밖으로 나가면 다시 추적 시작
                    currentState = AIState.Move;
                }
                else if (actionCondition != null && actionCondition.CanExecute(this, target))
                {
                    // 범위 안이고, 공격 쿨타임이 준비되었으면 공격 실행!
                    currentState = AIState.Action;
                }
                // (범위 안인데 쿨타임 중이면 계속 Idle 상태로 제자리에 멈춰서 대기합니다)
                break;

            case AIState.Move:
                // [애니메이션] 걷기/뛰기 상태 재생
                // if (anim != null) anim.SetBool("isMoving", true);

                if (isInRange)
                {
                    // 범위 안으로 들어오면 이동을 멈추고 판단하기 위해 Idle로 전환
                    currentState = AIState.Idle;
                }
                else
                {
                    // 아직 멀었다면 타겟을 향해 이동
                    MoveTowardsTarget(target);
                }
                break;

            case AIState.Action:
                // [애니메이션] 공격 트리거 발동
                // if (anim != null) anim.SetTrigger("Attack");

                if (actionStrategy != null)
                {
                    actionStrategy.Execute(this, target);
                }

                // 공격을 마친 직후에는 쿨타임을 기다려야 하므로 강제로 Idle(대기)로 돌아감
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
        if (coinPrefab == null) return;

        // 1. 최소~최대 개수 사이에서 무작위로 하나를 뽑습니다. (각 숫자별 동일 확률)
        // (Random.Range의 최대값은 포함되지 않으므로 +1을 해줍니다)
        int dropCount = Random.Range(minCoinCount, maxCoinCount + 1);

        // 2. 만약 뽑힌 개수가 0 이하라면 코인을 만들지 않고 함수를 종료합니다.
        if (dropCount <= 0) return;

        // [추가] 스테이지 배율이 곱해진 최종 코인 1개의 가치를 계산합니다. (반올림 처리)
        int finalCoinValue = Mathf.RoundToInt(coinValuePerItem * currentCoinMultiplier);

        // 3. 당첨된 개수만큼 반복해서 코인 생성
        for (int i = 0; i < dropCount; i++)
        {
            Vector3 spawnPos = transform.position + new Vector3(0, 0.5f, 0);
            GameObject spawnedCoin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);

            Coin coinScript = spawnedCoin.GetComponent<Coin>();
            if (coinScript != null)
            {
                // [수정] 기본 가치가 아닌, 배율이 곱해진 최종 가치를 넘겨줌
                coinScript.Setup(finalCoinValue);
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