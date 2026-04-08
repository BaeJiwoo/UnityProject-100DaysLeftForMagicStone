using System.Collections.Generic;
using UnityEngine;

public class AllyController : MonoBehaviour
{
    public enum PatrolTarget { MagicStone, Player }

    [Header("순찰 및 감지 설정")]
    public PatrolTarget patrolTargetType = PatrolTarget.MagicStone;

    public Transform magicStone;
    public Transform player;
    public float patrolRadius = 4f; // 보호대상의 경계 범위 (왕복 반경)
    public float detectRadius = 6f; // 아군의 사거리 (공격 범위)
    public float moveSpeed = 2f;

    // ==========================================
    // [추가] 순찰 중 대기(정지) 설정
    [Header("순찰 대기 설정")]
    public float minWaitTime = 1f; // 최소 대기 시간
    public float maxWaitTime = 3f; // 최대 대기 시간
    private float waitTimer = 0f;
    private bool isWaiting = false;
    // ==========================================

    [Header("공격 설정")]
    public float attackPower = 15f;
    public float fireRate = 0.5f;

    [Header("프리팹 및 위치")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    private Transform currentEnemy;
    private float nextFireTime = 0f;
    private SpriteRenderer sr;
    private Animator anim;

    // 좌우 왕복 방향을 체크하는 변수
    private bool movingRight = true;

    void Start()
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

    void Update()
    {
        // 1. 최우선 타겟 찾기
        FindPriorityEnemy();

        if (currentEnemy != null)
        {
            // [핵심 추가] 적을 발견하면 즉시 대기 상태를 풀고, 적이 있는 방향으로 순찰 방향을 갱신합니다.
            // 이렇게 하면 적 처치 후 마지막으로 교전한 방향으로 순찰을 이어갑니다.
            isWaiting = false;
            movingRight = currentEnemy.position.x > transform.position.x;

            // 적과의 실제 거리 계산
            float distanceToEnemy = Vector2.Distance(transform.position, currentEnemy.position);

            if (distanceToEnemy <= detectRadius)
            {
                // [상황 A] 적이 사거리 안에 들어왔을 때 -> 멈춰서 사격
                if (anim != null) anim.SetBool("isMoving", false);
                LookAtTarget(currentEnemy.position);
                Shoot();
            }
            else
            {
                // [상황 B] 적이 사거리는 밖이지만 경계 내일 때 -> 적을 향해 이동
                if (anim != null) anim.SetBool("isMoving", true);
                LookAtTarget(currentEnemy.position);
                transform.position = Vector2.MoveTowards(transform.position, currentEnemy.position, moveSpeed * Time.deltaTime);
            }
        }
        else
        {
            // [상황 C] 감지된 적이 없으면 -> 보호대상 주변 순찰 및 대기
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

    // [변경] 간헐적 정지가 추가된 순찰 로직
    void Patrol()
    {
        Transform center = GetPatrolCenter();
        if (center == null)
        {
            if (anim != null) anim.SetBool("isMoving", false);
            return;
        }

        // ==========================================
        // 1. 대기(정지) 상태일 때의 처리
        // ==========================================
        if (isWaiting)
        {
            if (anim != null) anim.SetBool("isMoving", false); // 멈춤 애니메이션
            waitTimer -= Time.deltaTime; // 타이머 감소

            if (waitTimer <= 0f)
            {
                isWaiting = false; // 대기 종료
                movingRight = !movingRight; // 대기가 끝난 후 반대 방향으로 전환
            }
            return; // 대기 중일 때는 아래 이동 코드를 실행하지 않음
        }

        // ==========================================
        // 2. 이동 상태일 때의 처리
        // ==========================================
        if (anim != null) anim.SetBool("isMoving", true);

        // 이동할 목표 X 좌표 설정
        float targetX = center.position.x + (movingRight ? patrolRadius : -patrolRadius);
        Vector2 targetPos = new Vector2(targetX, center.position.y);

        transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        LookAtTarget(targetPos);

        // 목표 지점(범위 끝)에 도달했다면 대기 모드로 돌입
        if (Vector2.Distance(transform.position, targetPos) < 0.1f)
        {
            isWaiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime); // 1~3초 사이 랜덤하게 휴식
        }
    }

    void Shoot()
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            if (anim != null) anim.SetTrigger("Attack");

            if (projectilePrefab != null && firePoint != null)
            {
                GameObject bulletObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

                // [오류 수정] SendMessage 방식 대신, 원래 작성하셨던 안전한 직접 호출 방식으로 복구했습니다!
                AllyProjectile bullet = bulletObj.GetComponent<AllyProjectile>();

                if (bullet != null)
                {
                    Vector2 direction = (currentEnemy.position - firePoint.position).normalized;
                    bullet.Setup(direction, attackPower);
                }
            }
        }
    }

        void LookAtTarget(Vector2 targetPos)
    {
        bool isLookingLeft = targetPos.x < transform.position.x;

        if (sr != null)
        {
            sr.flipX = isLookingLeft;
        }

        if (firePoint != null)
        {
            Vector3 currentPos = firePoint.localPosition;
            currentPos.x = isLookingLeft ? -Mathf.Abs(currentPos.x) : Mathf.Abs(currentPos.x);
            firePoint.localPosition = currentPos;
        }
    }

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