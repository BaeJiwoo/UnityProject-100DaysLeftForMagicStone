using UnityEngine;

// [1] 원거리 공격 조건
public class RangedAttackCondition : IActionCondition
{
    public bool CanExecute(BaseAI self, Transform target)
    {
        float distance = 0f;
        Collider2D targetCol = target.GetComponent<Collider2D>();

        // [핵심 수정] BaseAI와 동일하게 콜라이더 표면(가장자리)까지의 거리를 계산합니다.
        if (targetCol != null)
        {
            Vector2 edgePoint = targetCol.ClosestPoint(self.transform.position);
            distance = Vector2.Distance(self.transform.position, edgePoint);
        }
        else
        {
            distance = Vector2.Distance(self.transform.position, target.position);
        }

        // 사거리 이내이고, 쿨타임이 지났다면 발사!
        return distance <= self.attackRange && Time.time >= self.lastAttackTime + self.attackCooldown;
    }
}

// ==========================================
// [2] 원거리 공격 실행 (Strategy)
// ==========================================
public class RangedAttackStrategy : IActionStrategy
{
    private GameObject projectilePrefab;
    private Transform firePoint;
    private float bulletSpeed;

    public RangedAttackStrategy(GameObject prefab, Transform firePoint, float speed)
    {
        this.projectilePrefab = prefab;
        this.firePoint = firePoint;
        this.bulletSpeed = speed;
    }

    public void Execute(BaseAI self, Transform target)
    {
        // 1. 공격 시간 갱신
        self.lastAttackTime = Time.time;

        if (projectilePrefab != null)
        {
            // 2. 발사 위치 설정
            Vector3 spawnPos = firePoint != null ? firePoint.position : self.transform.position;

            // 3. 총알 생성
            GameObject bullet = GameObject.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            // 4. 타겟을 향한 방향 계산 (타겟의 중심점을 향해 날아감)
            Vector2 direction = (target.position - spawnPos).normalized;

            // 5. 생성된 총알에 데이터(방향, 데미지, 속도) 세팅
            EnemyProjectile proj = bullet.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                // [핵심 변경] 총알의 Setup 함수에 self.targetType(누구를 노리는가?)과 
                // self.gameObject(누가 쐈는가?)를 추가로 넘겨줍니다.
                proj.Setup(direction, self.attackPower, bulletSpeed, self.targetType, self.gameObject);
            }
        }
    }
}