using UnityEngine;

public class RangedEnemy : BaseAI
{
    [Header("원거리 공격 전용 설정")]
    public GameObject projectilePrefab; // 쏠 총알 프리팹
    public Transform firePoint;         // 총구 위치 (빈 오브젝트 할당)
    public float bulletSpeed = 5f;      // 총알 날아가는 속도

    protected override void Start()
    {
        // [핵심] 부모의 Start()를 먼저 호출해야 인스펙터에서 설정한 
        // 타겟(Player, MagicStone 등) 탐색기(TargetFinder)가 정상적으로 주입됩니다.
        base.Start();

        // 내 뱃속에 원거리 전용 조건과 전략 장착 (총알 데이터 전달)
        this.actionCondition = new RangedAttackCondition();
        this.actionStrategy = new RangedAttackStrategy(projectilePrefab, firePoint, bulletSpeed);
    }
}
