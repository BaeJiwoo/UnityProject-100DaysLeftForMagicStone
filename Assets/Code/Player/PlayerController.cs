using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // [수정] 인스펙터에서 숨겨서 DataManager의 값만 강제로 사용하게 만듭니다.
    [Header("이동 설정 (DataManager에서 자동 적용됨)")]
    [HideInInspector] public float maxSpeed;
    //public float jumpPower = 15f;

    [Header("가속/감속 설정")]
    public float acceleration = 60f;
    public float deceleration = 60f;

    [Header("조준 시스템 연동 (PlayerAttack에서 제어)")]
    [HideInInspector] public float currentAimRatio = 1f;
    [HideInInspector] public bool isAiming = false;
    [HideInInspector] public bool isAttacking = false; // 공격 상태 공유받음

    private Rigidbody2D rb;
    private Vector2 moveInput;
   // private bool isGrounded;

    // 애니메이션 및 시선 제어용
    private Animator anim;
    private SpriteRenderer sr;
    private Camera _mainCamera;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        _mainCamera = Camera.main;
    }

    void Start()
    {
        // [수정] DataManager가 존재하면 무조건 DataManager의 수치를 덮어씌웁니다.
        if (DataManager.Instance != null)
        {
            maxSpeed = DataManager.Instance.playerMaxSpeed;
        }
        else
        {
            // DataManager가 없는 씬에서 단독 테스트를 할 때를 위한 기본값 설정
            maxSpeed = 8f;
        }
    }

    void Update()
    {
        HandleAnimationAndFlip();
    }

    // 1. 시선(Flip) 및 애니메이션 제어
    void HandleAnimationAndFlip()
    {
        // --- [애니메이션 제어] ---
        // 입력이 있으면 이동 중(true), 없으면 정지(false)
        bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;
        if (anim != null)
        {
            anim.SetBool("isMoving", isMoving);
        }

        // --- [시선(좌우 반전) 제어] ---
        if (isAttacking)
        {
            // 공격 중: 마우스 위치를 바라봄 (뒤로 걸으면서 쏘기 가능)
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (mousePos.x < transform.position.x)
                sr.flipX = true;  // 왼쪽
            else
                sr.flipX = false; // 오른쪽
        }
        else
        {
            // 비공격 중: 이동하는 방향을 바라봄
            if (moveInput.x > 0.1f)
                sr.flipX = false; // 오른쪽 이동 -> 오른쪽 봄
            else if (moveInput.x < -0.1f)
                sr.flipX = true;  // 왼쪽 이동 -> 왼쪽 봄
        }
    }

    // 2. 이동 입력
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // 3. 점프 입력
    /*void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
        }
    }*/

    // 4. 물리 이동 적용
    void FixedUpdate()
    {
       // isGrounded = false; // 매 프레임 초기화 (OnCollisionStay2D에서 갱신)

        float currentMaxSpeed = isAiming ? maxSpeed * currentAimRatio : maxSpeed;
        float targetSpeed = moveInput.x * currentMaxSpeed;

        float currentAccelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, currentAccelRate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    // 5. 바닥 감지 (Layer 없이 각도로 계산)
    /*void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.7f) // 위를 향하는 표면(바닥)
            {
                isGrounded = true;
                return;
            }
        }
    }*/
}