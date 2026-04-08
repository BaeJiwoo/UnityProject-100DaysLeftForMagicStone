using UnityEngine;
using static AllyStrategies;

public class RangedAlly : BaseAllyAI
{
    [Header("원거리 동료 전용 설정")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    protected override void Start()
    {
        base.Start(); // 부모의 Start()를 실행하여 기초 세팅 완료

        // 내 뱃속에 원거리 전용 행동(공격) 전략 장착
        this.actionStrategy = new AllyRangedStrategy(projectilePrefab, firePoint);
    }

    // 부모가 방향을 바꿀 때(좌/우 시선 변경) 이 함수를 자동으로 불러줍니다.
    // 여기서 원거리 동료만의 고유한 특징인 '총구 위치 변경'을 처리합니다.
    protected override void OnFlipDirection(bool isLookingLeft)
    {
        if (firePoint != null)
        {
            Vector3 currentPos = firePoint.localPosition;
            currentPos.x = isLookingLeft ? -Mathf.Abs(currentPos.x) : Mathf.Abs(currentPos.x);
            firePoint.localPosition = currentPos;
        }
    }
}