using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;
public class ProjectileBehavior : MonoBehaviour
{
    [Header("투사체 기본 스펙")]
    public float manaCost = 10f;
    public float fireDelay = 0.2f;
    public float speed = 15f;
    public float damage = 10f;
    public float lifeTime = 3f;

    // ==========================================
    // [추가] 관통 및 넉백 설정
    [Header("관통 및 타격 설정")]
    [Tooltip("총알이 최대로 관통할 수 있는 적의 수 (1이면 단일 타겟)")]
    public int maxPierceCount = 1;
    public float knockbackForce = 3f; // 적을 밀어내는 힘

    private int _currentHits = 0;
    // 동일한 적(콜라이더 2개 등)을 중복해서 때리는 것을 방지하기 위한 명단
    private HashSet<GameObject> _hitEnemies = new HashSet<GameObject>();
    // ==========================================

    [Header("탄퍼짐 설정 (도 단위)")]
    public float spreadAngleHip = 15f;
    public float spreadAngleAim = 2f;

    [Header("조준 특성")]
    [Range(0f, 1f)]
    public float aimSlowdownRatio = 0.5f;
    public float rotationOffset = 0f;

    [Header("시각 효과")]
    public GameObject explosionFXPrefab;

    private Rigidbody2D rb;
    private IObjectPool<GameObject> _pool;
    private float _timer;
    private bool _isReleased = false;

    void Awake() => rb = GetComponent<Rigidbody2D>();
    public void SetPool(IObjectPool<GameObject> pool) => _pool = pool;

    void OnEnable()
    {
        _timer = 0f;
        rb.angularVelocity = 0f;
        _isReleased = false;

        // [추가] 총알이 새로 발사될 때 타격 명단과 횟수 초기화
        _currentHits = 0;
        _hitEnemies.Clear();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= lifeTime)
        {
            ReturnToPool(true);
            return;
        }
        RotateInDirection();
    }

    void RotateInDirection()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        }
    }

    public void Launch(Vector2 direction)
    {
        rb.linearVelocity = direction * speed;
        RotateInDirection();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isReleased) return;

        // 1. 플레이어 자신이나, 아군 총알끼리는 무시하고 통과
        if (collision.CompareTag("Player") || collision.CompareTag("Projectile")) return;

        // 2. 부딪힌 대상이 '적 본체' 이거나 '적 총알'일 경우
        if (collision.CompareTag("Enemy") || collision.CompareTag("EnemyProjectile"))
        {
            // 이미 때린 적/총알이면 무시 (관통 총알 중복 타격 방지)
            if (_hitEnemies.Contains(collision.gameObject)) return;

            _hitEnemies.Add(collision.gameObject); // 명단에 추가
            _currentHits++; // 타격(관통) 횟수 증가

            // [A] 적 본체를 맞췄을 때
            if (collision.CompareTag("Enemy"))
            {
                BaseAI enemy = collision.GetComponent<BaseAI>();
                if (enemy != null)
                {
                    Vector2 knockbackDir = rb.linearVelocity.normalized;
                    enemy.TakeDamage(damage, knockbackDir, knockbackForce);
                }
            }
            // [B] 적 총알을 맞췄을 때 (요격!)
            else if (collision.CompareTag("EnemyProjectile"))
            {
                EnemyProjectile enemyBullet = collision.GetComponent<EnemyProjectile>();
                if (enemyBullet != null)
                {
                    // 적 총알에 데미지 전달 (넉백은 필요 없으므로 데미지만 줍니다)
                    enemyBullet.TakeDamage(damage);
                }
            }

            // 설정한 최대 관통 수에 도달했다면 플레이어 총알 삭제(회수)
            if (_currentHits >= maxPierceCount)
            {
                ReturnToPool(true);
            }
        }
        else
        {
            // 3. 적/적총알이 아닌 다른 Trigger(예: 바닥의 코인, 적의 탐지 범위 등)는 무시하고 통과
            if (collision.isTrigger) return;

            // 4. 진짜 물리적인 벽이나 바닥(!isTrigger)에 맞았을 때는 즉시 삭제
            ReturnToPool(true);
        }
    }

    private void ReturnToPool(bool spawnFX = false)
    {
        if (_isReleased) return;
        _isReleased = true;

        if (spawnFX && explosionFXPrefab != null)
        {
            Instantiate(explosionFXPrefab, transform.position, transform.rotation);
        }

        rb.linearVelocity = Vector2.zero;
        if (_pool != null) _pool.Release(gameObject);
        else Destroy(gameObject);
    }
}