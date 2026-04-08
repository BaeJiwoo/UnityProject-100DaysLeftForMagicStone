using UnityEngine;

public class AllyStrategies : MonoBehaviour
{
    // ==========================================
    // [1] 동료 행동(공격) 전략 인터페이스
    // ==========================================
    public interface IAllyActionStrategy
    {
        void Execute(BaseAllyAI self, Transform target);
    }

    // ==========================================
    // [2] 원거리 공격 전략 구현체
    // ==========================================
    public class AllyRangedStrategy : IAllyActionStrategy
    {
        private GameObject projectilePrefab;
        private Transform firePoint;

        // 생성자를 통해 원거리 동료가 가진 총알과 총구 위치를 전달받습니다.
        public AllyRangedStrategy(GameObject prefab, Transform firePoint)
        {
            this.projectilePrefab = prefab;
            this.firePoint = firePoint;
        }

        public void Execute(BaseAllyAI self, Transform target)
        {
            // 애니메이션 재생
            if (self.anim != null) self.anim.SetTrigger("Attack");

            // 총알 발사
            if (projectilePrefab != null && firePoint != null)
            {
                GameObject bulletObj = GameObject.Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                AllyProjectile bullet = bulletObj.GetComponent<AllyProjectile>();

                if (bullet != null)
                {
                    Vector2 direction = (target.position - firePoint.position).normalized;
                    bullet.Setup(direction, self.attackPower); // BaseAllyAI의 공격력을 가져와 세팅
                }
            }
        }
    }
}