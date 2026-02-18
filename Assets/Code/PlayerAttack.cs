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

        // [변경] 두 가지 퍼짐 값을 모두 저장
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

    // [삭제] aimSpreadRatio는 더 이상 쓰지 않음 (프리팹 개별 설정으로 대체)
    // public float aimSpreadRatio = 0.2f; 

    private int currentWeaponIndex = 0;
    private ManaSystem manaSystem;
    private Camera _mainCamera;

    private bool _isFiring = false;
    private bool _isAiming = false;
    private float _lastFireTime = 0f;

    void Awake()
    {
        manaSystem = GetComponent<ManaSystem>();
        _mainCamera = Camera.main;

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
        HandleCameraZoomAndPan();
        HandleWeaponSpecs();
        HandleFiring();
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
        if (Mouse.current.leftButton.isPressed)
        {
            Weapon currentWeapon = weapons[currentWeaponIndex];
            if (Time.time >= _lastFireTime + currentWeapon.cachedFireDelay)
            {
                TryShoot(currentWeapon);
                _lastFireTime = Time.time;
            }
        }

        // 우클릭 상태 체크 (Update에서 지속 확인)
        _isAiming = Mouse.current.rightButton.isPressed;
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
