using System.Buffers.Text;
using UnityEngine;

// [1] 근접 공격 조건: 사거리 내에 들어왔는지 & 쿨타임이 돌았는지 체크
public class MeleeAttackCondition : IActionCondition
{
    public bool CanExecute(BaseAI self, Transform target)
    {
        float distance = 0f;
        Collider2D targetCol = target.GetComponent<Collider2D>();
        //Collider2D targetCol = MagicStoneManager.Instance.StoneCollider;

        // 타겟의 콜라이더 표면(가장자리)까지의 거리 계산
        if (targetCol != null)
        {
            Vector2 edgePoint = targetCol.ClosestPoint(self.transform.position);
            distance = Vector2.Distance(self.transform.position, edgePoint);
        }
        else
        {
            distance = Vector2.Distance(self.transform.position, target.position);
        }

        // 사거리 이내이고, 쿨타임이 지났다면 true 반환
        return distance <= self.attackRange && Time.time >= self.lastAttackTime + self.attackCooldown;
    }
}

// [2] 근접 공격 실행: 데미지를 주고 마지막 공격 시간 갱신
public class MeleeAttackStrategy : IActionStrategy
{
    public void Execute(BaseAI self, Transform target)
    {
        // 공격 시간 갱신
        self.lastAttackTime = Time.time;

        // 타겟에게 데미지 전달
        target.SendMessage("TakeDamage", self.attackPower, SendMessageOptions.DontRequireReceiver);
    }
}