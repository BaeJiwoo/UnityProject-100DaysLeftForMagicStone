using UnityEngine;
using System.Collections;
public class Coin : MonoBehaviour
{
    [Header("코인 설정")]
    public float lifeTime = 5f;
    public float blinkDuration = 2f;
    public float blinkSpeed = 0.1f;

    [Header("획득 설정")]
    [Tooltip("이 원 안에 플레이어가 들어오면 획득됩니다.")]
    public float pickupRadius = 2f; // 기본값을 조금 더 넓혔습니다.

    private int _coinValue;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool isCollected = false;

    private Transform playerTransform;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Setup(int value)
    {
        _coinValue = value;

        if (rb != null)
        {
            Vector2 jumpForce = new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 8f));
            rb.AddForce(jumpForce, ForceMode2D.Impulse);
        }

        StartCoroutine(LifeTimeRoutine());
    }

    void Update()
    {
        // 1. 플레이어를 아직 못 찾았다면 계속해서 찾습니다. (오류 방지 핵심)
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                return; // 플레이어가 없다면 아래 거리 계산을 하지 않고 넘어갑니다.
            }
        }

        // 2. 거리 계산 및 획득 판정
        if (!isCollected)
        {
            // Vector2.Distance를 사용하여 Z축(깊이) 차이로 인한 버그를 무시하고 평면 거리만 잽니다.
            float distance = Vector2.Distance(transform.position, playerTransform.position);

            if (distance <= pickupRadius)
            {
                CollectCoin();
            }
        }
    }

    private void CollectCoin()
    {
        isCollected = true;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.coins += _coinValue;
            Debug.Log($"코인 획득! +{_coinValue} G / 총액: {DataManager.Instance.coins} G");
        }

        Destroy(gameObject);
    }

    private IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(lifeTime - blinkDuration);

        float elapsed = 0f;
        bool isVisible = true;

        while (elapsed < blinkDuration)
        {
            isVisible = !isVisible;
            sr.color = isVisible ? Color.white : new Color(1f, 1f, 1f, 0.3f);

            yield return new WaitForSeconds(blinkSpeed);
            elapsed += blinkSpeed;
        }

        Destroy(gameObject);
    }

    // [추가] 유니티 에디터에서 코인의 획득 반경을 눈으로 볼 수 있게 해줍니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}