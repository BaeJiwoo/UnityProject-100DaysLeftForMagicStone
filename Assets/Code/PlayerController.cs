using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float maxSpeed = 8f;      // 최대 이동 속도
                                     // public float jumpPower = 5f;  // 점프력

    [Header("가속/감속 설정")]
    public float acceleration = 500f; // 가속도 (클수록 빨리 최대 속도 도달)
    public float deceleration = 500f; // 감속도 (클수록 빨리 멈춤)

    // [변경] 인스펙터 설정 제거 -> 외부에서 값을 덮어씌움 (Public이지만 숨김)
    [HideInInspector] public float currentAimRatio = 1f;

    [HideInInspector] public bool isAiming = false;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 1. 이동 입력 (Player Input: "Move" 액션)
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // 2. 점프 입력 (Player Input: "Jump" 액션)
    /*void OnJump(InputValue value)
    {
        // [중요] Unity 6에서는 velocity 대신 linearVelocity를 사용합니다.
        // 간단한 바닥 체크: Y축 속도가 거의 0일 때만 점프 가능
        if (value.isPressed && Mathf.Abs(rb.linearVelocity.y) < 0.001f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
        }
    }*/

    void FixedUpdate()
    {
        // [핵심 로직] 조준 중이면 속도 감소 적용
        // 조준 중(isAiming) ? (기본속도 * 비율) : 그냥 기본속도
        float currentMaxSpeed = isAiming ? maxSpeed * currentAimRatio : maxSpeed;

        // 1. 목표 속도 계산
        float targetSpeed = moveInput.x * currentMaxSpeed;

        // 2. 가속/감속 여부 판단
        // 키를 입력 중이고(속도가 있고) && 현재 속도가 목표 속도보다 작을 때 등등 물리적 가속 처리
        float currentAccelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // 3. 부드러운 속도 변화 (가감속 적용)
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, currentAccelRate * Time.fixedDeltaTime);

        // 4. 최종 적용
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }
}
