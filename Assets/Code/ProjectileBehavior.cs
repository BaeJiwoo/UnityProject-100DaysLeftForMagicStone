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

    [Header("탄퍼짐 설정 (도 단위)")]
    public float spreadAngleHip = 15f; // [변경] 비조준(지향사격) 시 퍼짐 정도
    public float spreadAngleAim = 2f;  // [변경] 정조준 시 퍼짐 정도 (0이면 일직선)

    [Header("조준 특성")]
    [Range(0f, 1f)]
    public float aimSlowdownRatio = 0.5f; // 조준 시 이동속도 배율

    [Header("스프라이트 방향 보정")]
    [Tooltip("화살촉이 위(▲)라면 -90, 오른쪽(▶)이면 0")]
    public float rotationOffset = 0f;

    private Rigidbody2D rb;
    private IObjectPool<GameObject> _pool;
    private float _timer;

    void Awake() => rb = GetComponent<Rigidbody2D>();
    public void SetPool(IObjectPool<GameObject> pool) => _pool = pool;

    void OnEnable()
    {
        _timer = 0f;
        rb.angularVelocity = 0f;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= lifeTime)
        {
            ReturnToPool();
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
        if (collision.CompareTag("Player") || collision.CompareTag("Projectile") || collision.isTrigger) return;
        if (collision.CompareTag("Enemy")) Debug.Log("적 명중!");
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        rb.linearVelocity = Vector2.zero;
        if (_pool != null) _pool.Release(gameObject);
        else Destroy(gameObject);
    }
}