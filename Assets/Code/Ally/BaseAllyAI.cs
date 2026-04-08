using System.Collections.Generic;
using UnityEngine;
using static AllyStrategies;

public class BaseAllyAI : MonoBehaviour
{
    public enum PatrolTarget { MagicStone, Player }
    // [핵심 추가] 스포너가 동료를 소환하자마자 호출해 줄 스탯 덮어쓰기 함수
    // ==========================================
    public void InitializeData(MercenaryInfo info, int currentLevel)
    {
        // 1. 변하지 않는 고정 정보 세팅
        this.patrolTargetType = info.patrolTargetType;
        this.moveSpeed = info.moveSpeed;
        this.minWaitTime = info.minWaitTime;
        this.maxWaitTime = info.maxWaitTime;

        // 2. 레벨에 비례하여 성장하는 수치들 계산 및 적용
        int levelMultiplier = currentLevel - 1;

        this.attackPower = info.baseAttackPower + (levelMultiplier * info.attackGrowth);
        this.patrolRadius = info.basePatrolRadius + (levelMultiplier * info.patrolRadiusGrowth);
        this.detectRadius = info.baseDetectRadius + (levelMultiplier * info.detectRadiusGrowth);

        // 연사 속도(딜레이)는 레벨이 오를수록 빼기(-) 처리를 합니다.
        float calculatedFireRate = info.baseFireRate - (levelMultiplier * info.fireRateReduction);

        // Mathf.Max를 사용하여 딜레이가 0이 되거나 마이너스로 내려가 에러가 나는 것을 방지합니다.
        this.fireRate = Mathf.Max(calculatedFireRate, info.minFireRate);

        Debug.Log($"[{info.mercName}] 셋업 완료! 레벨:{currentLevel} / 공격력:{this.attackPower} / 범위:{this.detectRadius} / 연사력:{this.fireRate}");
    }

    [Header("순찰 및 감지 설정")]
    public PatrolTarget patrolTargetType = PatrolTarget.MagicStone;

    public Transform magicStone;
    public Transform player;
    public float patrolRadius = 4f;
    public float detectRadius = 6f;
    public float moveSpeed = 2f;

    [Header("순찰 대기 설정")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    [Header("공격 설정")]
    public float attackPower = 15f;
    public float fireRate = 0.5f;

    // 자식 클래스 및 전략에서 접근할 수 있도록 public(또는 protected)으로 변경
    [HideInInspector] public Animator anim;
    protected Transform currentEnemy;
    protected float nextFireTime = 0f;
    protected SpriteRenderer sr;
    protected bool movingRight = true;

    // 장착될 전략 패턴 (자식 클래스에서 세팅)
    protected IAllyActionStrategy actionStrategy;

    protected virtual void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        if (magicStone == null)
        {
            GameObject stone = GameObject.FindGameObjectWithTag("MagicStone");
            if (stone != null) magicStone = stone.transform;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    protected virtual void Update()
    {
        FindPriorityEnemy();

        if (currentEnemy != null)
        {
            isWaiting = false;
            movingRight = currentEnemy.position.x > transform.position.x;

            float distanceToEnemy = Vector2.Distance(transform.position, currentEnemy.position);

            if (distanceToEnemy <= detectRadius)
            {
                if (anim != null) anim.SetBool("isMoving", false);
                LookAtTarget(currentEnemy.position);

                // 쿨타임 체크 후 공격 전략 실행!
                if (Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + fireRate;
                    if (actionStrategy != null)
                    {
                        actionStrategy.Execute(this, currentEnemy);
                    }
                }
            }
            else
            {
                if (anim != null) anim.SetBool("isMoving", true);
                LookAtTarget(currentEnemy.position);
                transform.position = Vector2.MoveTowards(transform.position, currentEnemy.position, moveSpeed * Time.deltaTime);
            }
        }
        else
        {
            Patrol();
        }
    }

    Transform GetPatrolCenter()
    {
        if (patrolTargetType == PatrolTarget.Player) return player;
        return magicStone;
    }

    void FindPriorityEnemy()
    {
        Transform center = GetPatrolCenter();
        if (center == null) return;

        Collider2D[] allyHits = Physics2D.OverlapCircleAll(transform.position, detectRadius);
        Collider2D[] centerHits = Physics2D.OverlapCircleAll(center.position, patrolRadius);

        HashSet<Collider2D> allHits = new HashSet<Collider2D>(allyHits);
        allHits.UnionWith(centerHits);

        float closestDistanceToCenter = Mathf.Infinity;
        Transform priorityEnemy = null;

        foreach (Collider2D col in allHits)
        {
            if (col.CompareTag("Enemy"))
            {
                float distanceToCenter = Vector2.Distance(center.position, col.transform.position);
                if (distanceToCenter < closestDistanceToCenter)
                {
                    closestDistanceToCenter = distanceToCenter;
                    priorityEnemy = col.transform;
                }
            }
        }
        currentEnemy = priorityEnemy;
    }

    void Patrol()
    {
        Transform center = GetPatrolCenter();
        if (center == null)
        {
            if (anim != null) anim.SetBool("isMoving", false);
            return;
        }

        if (isWaiting)
        {
            if (anim != null) anim.SetBool("isMoving", false);
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                movingRight = !movingRight;
            }
            return;
        }

        if (anim != null) anim.SetBool("isMoving", true);

        float targetX = center.position.x + (movingRight ? patrolRadius : -patrolRadius);
        Vector2 targetPos = new Vector2(targetX, center.position.y);

        transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        LookAtTarget(targetPos);

        if (Vector2.Distance(transform.position, targetPos) < 0.1f)
        {
            isWaiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    protected void LookAtTarget(Vector2 targetPos)
    {
        bool isLookingLeft = targetPos.x < transform.position.x;

        if (sr != null)
        {
            sr.flipX = isLookingLeft;
        }

        // 방향 전환 시 추가로 처리할 사항이 있다면 자식 클래스에서 오버라이드하여 사용합니다.
        OnFlipDirection(isLookingLeft);
    }

    // [핵심] 자식 클래스(원거리 동료 등)가 방향 전환 시 총구 방향을 바꿀 수 있게 열어둡니다.
    protected virtual void OnFlipDirection(bool isLookingLeft) { }

    private void OnDrawGizmosSelected()
    {
        Transform center = GetPatrolCenter();
        if (center != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center.position, patrolRadius);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}