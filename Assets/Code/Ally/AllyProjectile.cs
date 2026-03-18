using UnityEngine;

public class AllyProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    public float speed = 10f;
    public float lifeTime = 3f;
    [Tooltip("1이면 단일 타겟, 2이상이면 관통")] // 툴팁 추가
    public int maxPierceCount = 1;
    public float knockbackForce = 2f;
    public GameObject explosionFXPrefab;

    private float _damage;
    private int _currentHits = 0;
    private Rigidbody2D rb;

    // ==========================================
    // [추가] 동시 타격 방지 스위치
    private bool _isDestroying = false;
    // ==========================================

    public void Setup(Vector2 direction, float attackDamage)
    {
        rb = GetComponent<Rigidbody2D>();
        _damage = attackDamage;
        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ==========================================
        // [추가] 이미 타격하여 사라지는 중이라면 모든 충돌 무시
        if (_isDestroying) return;
        // ==========================================

        if (collision.CompareTag("Player") || /*collision.CompareTag("Ally") ||*/
            collision.CompareTag("MagicStone") || collision.CompareTag("Projectile") || collision.isTrigger) return;

        if (collision.CompareTag("Enemy"))
        {
            //EnemyController enemy = collision.GetComponent<EnemyController>();
            BaseAI enemy = collision.GetComponent<BaseAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage, rb.linearVelocity.normalized, knockbackForce);
            }

            _currentHits++;

            // ==========================================
            // [수정] maxPierceCount가 1일 때 즉시 플래그를 켜서 동시 타격 막기
            if (_currentHits >= maxPierceCount)
            {
                _isDestroying = true; // 스위치 켬 (다음 충돌부터 return됨)
                ExplodeAndDestroy();
            }
            // ==========================================
        }
        else
        {
            _isDestroying = true; // 벽에 맞았을 때도 켬
            ExplodeAndDestroy();
        }
    }

    void ExplodeAndDestroy()
    {
        if (explosionFXPrefab != null)
        {
            Instantiate(explosionFXPrefab, transform.position, transform.rotation);
        }
        // 속도를 zero로 만들어 잔상 효과 방지
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Destroy(gameObject);
    }
}