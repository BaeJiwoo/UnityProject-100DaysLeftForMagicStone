using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("효과 설정")]
    public GameObject hitFXPrefab; // 타겟이나 벽에 맞았을 때 생성할 이펙트 프리팹

    [Header("총알 체력 설정")]
    [Tooltip("총알의 체력입니다. 플레이어의 공격력이 이 수치 이상이어야 총알이 부서집니다.")]
    public float maxHealth = 10f;
    private float currentHealth;

    private Vector2 moveDir;
    private float effectAmount; // 데미지 또는 회복량
    private float speed;

    // 총알이 기억할 발사자 정보
    private TargetType myTargetType;
    private GameObject shooter;



    // Setup 함수에서 targetType과 shooter를 추가로 받습니다.
    public void Setup(Vector2 direction, float amount, float spd, TargetType targetType, GameObject shooterObj)
    {
        this.moveDir = direction;
        this.effectAmount = amount;
        this.speed = spd;
        this.myTargetType = targetType;
        this.shooter = shooterObj;
        this.currentHealth = maxHealth;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 허공으로 날아간 총알은 5초 뒤에 조용히(FX 없이) 사라집니다.
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 발사한 자기 자신이나 다른 총알은 무시하고 통과합니다.
        if (collision.gameObject == shooter || collision.CompareTag("EnemyProjectile")) return;

        // 2. 부딪힌 대상이 '내가 노리는 타겟'인지 확인합니다.
        bool isHitTarget = false;

        switch (myTargetType)
        {
            case TargetType.MagicStone:
                if (collision.CompareTag("MagicStone")) isHitTarget = true;
                break;
            case TargetType.Player:
                if (collision.CompareTag("Player")) isHitTarget = true;
                break;
            case TargetType.EnemyAlly:
                if (collision.CompareTag("Enemy")) isHitTarget = true;
                break;
        }

        // 3. 타겟을 맞췄다면 데미지(또는 힐)를 주고 파괴!
        if (isHitTarget)
        {
            collision.SendMessageUpwards("TakeDamage", effectAmount, SendMessageOptions.DontRequireReceiver);
            DestroyWithFX(); // [변경] 그냥 파괴하지 않고 이펙트를 생성하며 파괴
        }
        else
        {
            // 내가 노리는 타겟은 아니지만, 게임 내 주요 오브젝트에 부딪혔다면 통과(무시)
            if (collision.CompareTag("MagicStone") || collision.CompareTag("Player") || collision.CompareTag("Enemy") || collision.CompareTag("Ally"))
            {
                return;
            }

            // 4. 캐릭터가 아닌 진짜 물리 벽이나 바닥(!isTrigger)에 부딪혔을 때는 파괴
            if (!collision.isTrigger)
            {
                DestroyWithFX(); // [변경] 벽에 맞았을 때도 이펙트를 생성하며 파괴
            }
        }
    }

    // [핵심 추가] 플레이어의 공격을 받았을 때 체력이 깎이는 함수
    // (BaseAI의 TakeDamage와 이름/형식이 똑같아서 플레이어 총알이 쉽게 인식합니다!)
    // ==========================================
    public void TakeDamage(float damage, Vector2 knockbackDir = default, float bulletKnockbackForce = 0f)
    {
        currentHealth -= damage;

        // 체력이 0 이하가 되면 파괴 (원한다면 여기서 요격 성공 추가 점수를 줄 수도 있습니다)
        if (currentHealth <= 0)
        {
            DestroyWithFX();
        }
    }
    // ==========================================
    // [추가] 이펙트를 생성하고 자기 자신을 파괴하는 함수
    // ==========================================
    private void DestroyWithFX()
    {
        // 인스펙터에 이펙트 프리팹이 할당되어 있다면, 내 현재 위치에 생성
        if (hitFXPrefab != null)
        {
            Instantiate(hitFXPrefab, transform.position, Quaternion.identity);
        }

        // 그리고 나 자신은 파괴
        Destroy(gameObject);
    }
}