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

    // [추가] 좌우 왕복 방향을 체크하는 변수
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
            // 적과의 실제 거리 계산
            float distanceToEnemy = Vector2.Distance(transform.position, currentEnemy.position);

            if (distanceToEnemy <= detectRadius)
            {
                // [상황 A] 적이 내 사거리(detectRadius) 안에 들어왔을 때 -> 멈춰서 사격
                if (anim != null) anim.SetBool("isMoving", false);
                LookAtTarget(currentEnemy.position);
                Shoot();
            }
            else
            {
                // [상황 B] 적이 사거리 밖이지만 보호대상 경계(patrolRadius) 내에 들어왔을 때 -> 적을 향해 이동
                if (anim != null) anim.SetBool("isMoving", true);
                LookAtTarget(currentEnemy.position);
                transform.position = Vector2.MoveTowards(transform.position, currentEnemy.position, moveSpeed * Time.deltaTime);
            }
        }
        else
        {
            // [상황 C] 감지된 적이 없으면 -> 보호대상 주변 좌우 왕복 순찰
            Patrol();
        }
    }

    Transform GetPatrolCenter()
    {
        if (patrolTargetType == PatrolTarget.Player) return player;
        return magicStone;
    }

    // [변경] 우선순위가 높은 적을 찾는 로직으로 업그레이드
    void FindPriorityEnemy()
    {
        Transform center = GetPatrolCenter();
        if (center == null) return;

        // 1. 아군 주변(사거리)과 보호대상 주변(경계)의 모든 적을 찾음
        Collider2D[] allyHits = Physics2D.OverlapCircleAll(transform.position, detectRadius);
        Collider2D[] centerHits = Physics2D.OverlapCircleAll(center.position, patrolRadius);

        // 2. 두 배열을 하나로 합쳐서 중복을 제거 (HashSet 사용)
        HashSet<Collider2D> allHits = new HashSet<Collider2D>(allyHits);
        allHits.UnionWith(centerHits);

        float closestDistanceToCenter = Mathf.Infinity;
        Transform priorityEnemy = null;

        // 3. 찾은 모든 적들 중 "보호대상(Center)으로부터 가장 가까운 적"을 최우선 타겟으로 지정
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

    // [변경] 좌우 왕복 순찰 로직
    void Patrol()
    {
        Transform center = GetPatrolCenter();
        if (center == null)
        {
            if (anim != null) anim.SetBool("isMoving", false);
            return;
        }

        if (anim != null) anim.SetBool("isMoving", true);

        // 이동할 목표 X 좌표 설정 (오른쪽으로 갈 때는 center + 범위, 왼쪽일 때는 center - 범위)
        float targetX = center.position.x + (movingRight ? patrolRadius : -patrolRadius);

        // 자연스럽게 보호대상의 Y축 높이로 맞춰가며 이동
        Vector2 targetPos = new Vector2(targetX, center.position.y);

        transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        LookAtTarget(targetPos);

        // 목표 지점(범위 끝)에 도달했다면 반대 방향으로 전환
        if (Vector2.Distance(transform.position, targetPos) < 0.1f)
        {
            movingRight = !movingRight;
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
        // 타겟이 내 위치보다 왼쪽에 있는지 확인
        bool isLookingLeft = targetPos.x < transform.position.x;

        if (sr != null)
        {
            sr.flipX = isLookingLeft; // 코드 정리
        }

        // 총구(FirePoint) 위치 반전
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