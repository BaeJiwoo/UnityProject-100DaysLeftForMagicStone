using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

public class PlayerAttack : MonoBehaviour
{
    [System.Serializable]
    public class Weapon
    {
        public string name;
        public GameObject prefab;

        [HideInInspector] public float cachedManaCost;
        [HideInInspector] public IObjectPool<GameObject> pool;
        [HideInInspector] public float cachedFireDelay;
        [HideInInspector] public float cachedAimRatio;

        // 두 가지 퍼짐 값을 모두 저장
        [HideInInspector] public float cachedSpreadHip;
        [HideInInspector] public float cachedSpreadAim;
    }

    [Header("설정")]
    public List<Weapon> weapons;
    public Transform firePoint;
    public CinemachineCamera aimCamera;
    public PlayerController moveController;

    [Header("조준 카메라 설정")]
    public Transform cameraTarget;
    public float aimZoomSize = 5f;
    public float normalZoomSize = 7f;
    public float panDistance = 3f;
    public float zoomSpeed = 5f;

    // 무기 시각 효과 설정
    [Header("무기 시각 효과")]
    public SpriteRenderer weaponRenderer; // 무기 이미지 (WeaponVisual)
    public float fadeOutSpeed = 5f;       // 마우스 뗄 때 투명해지는 속도
    public float blinkSpeed = 20f;        // 마나 부족 시 깜빡이는 속도


    private int currentWeaponIndex = 0;
    private ManaSystem manaSystem;
    private Camera _mainCamera;
    private Animator anim;

    private bool _isFiring = false;
    private bool _isAiming = false;
    private float _lastFireTime = 0f;

    void Awake()
    {
        manaSystem = GetComponent<ManaSystem>();
        _mainCamera = Camera.main;
        anim = GetComponent<Animator>();

        if (moveController == null) moveController = GetComponent<PlayerController>();
        if (cameraTarget == null) cameraTarget = transform;

        foreach (var weapon in weapons)
        {
            ProjectileBehavior pBehavior = weapon.prefab.GetComponent<ProjectileBehavior>();
            if (pBehavior != null)
            {
                weapon.cachedManaCost = pBehavior.manaCost;
                weapon.cachedFireDelay = pBehavior.fireDelay;
                weapon.cachedAimRatio = pBehavior.aimSlowdownRatio;

                // [변경] 두 가지 탄퍼짐 값 읽어오기
                weapon.cachedSpreadHip = pBehavior.spreadAngleHip;
                weapon.cachedSpreadAim = pBehavior.spreadAngleAim;
            }
            else
            {
                // 기본값 예외처리
                weapon.cachedManaCost = 0f;
                weapon.cachedFireDelay = 0.5f;
                weapon.cachedSpreadHip = 15f;
                weapon.cachedSpreadAim = 2f;
            }

            weapon.pool = new ObjectPool<GameObject>(
                createFunc: () => {
                    GameObject obj = Instantiate(weapon.prefab);
                    obj.GetComponent<ProjectileBehavior>().SetPool(weapon.pool);
                    return obj;
                },
                actionOnGet: (obj) => {
                    obj.SetActive(true);
                    obj.transform.position = firePoint != null ? firePoint.position : transform.position;
                },
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 10, maxSize: 50
            );
        }
    }

    void Update()
    {
        
        HandleWeaponVisibility(); // 매 프레임 무기 투명도 관리 함수 실행
        HandleCameraZoomAndPan();
        HandleWeaponSpecs();
        HandleFiring();
        
    }

    // 무기 투명도 및 깜빡임 처리 로직
    void HandleWeaponVisibility()
    {
        if (weaponRenderer == null) return;

        // 현재 스프라이트의 색상(알파값 포함) 가져오기
        Color color = weaponRenderer.color;

        if (_isFiring) // 마우스를 누르고 있는 중
        {
            Weapon currentWeapon = weapons[currentWeaponIndex];

            // 마나가 충분한지 확인 (ManaSystem의 currentMana가 소모량보다 큰지)
            if (manaSystem.currentMana >= currentWeapon.cachedManaCost)
            {
                // 마나 충분: 즉시 완전 선명하게 (투명도 100%)
                color.a = 1f;
            }
            else
            {
                // 마나 부족: Sin 그래프를 이용해 투명도가 0.3 ~ 1.0 사이를 빠르게 오가게 함 (깜빡임 효과)
                color.a = 0.65f + 0.35f * Mathf.Sin(Time.time * blinkSpeed);
            }
        }
        else // 마우스를 뗀 상태
        {
            // 서서히 사라짐 (현재 알파값에서 0을 향해 부드럽게 감소)
            color.a = Mathf.Lerp(color.a, 0f, Time.deltaTime * fadeOutSpeed);
        }

        // 변경된 색상(알파값)을 스프라이트에 다시 적용
        weaponRenderer.color = color;
    }

    void HandleCameraZoomAndPan()
    {
        if (aimCamera == null) return;

        float targetSize = normalZoomSize;
        Vector3 targetLocalPos = Vector3.zero;

        if (_isAiming)
        {
            targetSize = aimZoomSize;
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0;
            Vector3 direction = (mousePos - transform.position);
            targetLocalPos = Vector3.ClampMagnitude(direction, panDistance);
        }

        aimCamera.Lens.OrthographicSize = Mathf.Lerp(aimCamera.Lens.OrthographicSize, targetSize, Time.deltaTime * zoomSpeed);

        if (cameraTarget != transform)
        {
            cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, targetLocalPos, Time.deltaTime * zoomSpeed);
        }
    }

    void HandleWeaponSpecs()
    {
        if (moveController != null && weapons.Count > 0)
        {
            float ratio = _isAiming ? weapons[currentWeaponIndex].cachedAimRatio : 1f;
            moveController.currentAimRatio = ratio;
            moveController.isAiming = _isAiming;
        }
    }

    void HandleFiring()
    {
        // 마우스 좌우클릭 상태 업데이트
        _isFiring = Mouse.current.leftButton.isPressed;
        _isAiming = Mouse.current.rightButton.isPressed;

        // 공격 상태(isAttacking)를 애니메이터와 이동 컨트롤러에 공유
        if (anim != null) anim.SetBool("isAttacking", _isFiring);
        if (moveController != null) moveController.isAttacking = _isFiring;

        // 쿨타임 발사 로직
        if (_isFiring)
        {
            Weapon currentWeapon = weapons[currentWeaponIndex];
            if (Time.time >= _lastFireTime + currentWeapon.cachedFireDelay)
            {
                TryShoot(currentWeapon);
                _lastFireTime = Time.time;
            }
        }
    }

    // Input System 이벤트 (사용 안 함, Update에서 직접 처리)
    void OnAim(InputValue value) { }
    void OnAttack(InputValue value) { }

    void OnSwitchWeapon(InputValue value)
    {
        float scrollY = value.Get<Vector2>().y;
        if (scrollY > 0) currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Count;
        else if (scrollY < 0) currentWeaponIndex = (currentWeaponIndex - 1 + weapons.Count) % weapons.Count;

        Debug.Log($"무기 변경: {weapons[currentWeaponIndex].name}");
    }

    void TryShoot(Weapon weapon)
    {
        if (manaSystem.UseMana(weapon.cachedManaCost))
        {
            Vector2 baseDir = firePoint != null ? firePoint.right : transform.right;

            // [핵심 로직 변경] 조준 여부에 따라 사용할 탄퍼짐 값을 선택
            float selectedSpread = _isAiming ? weapon.cachedSpreadAim : weapon.cachedSpreadHip;

            // 선택된 퍼짐 각도 내에서 랜덤 적용
            float randomAngle = Random.Range(-selectedSpread / 2f, selectedSpread / 2f);
            Vector2 finalDirection = Quaternion.Euler(0, 0, randomAngle) * baseDir;

            GameObject bullet = weapon.pool.Get();
            bullet.transform.position = firePoint != null ? firePoint.position : transform.position;
            bullet.GetComponent<ProjectileBehavior>().Launch(finalDirection);
        }
    }
}
