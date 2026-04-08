using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

public class PlayerAttack : MonoBehaviour
{
    // [변경] DataManager에서 받아온 업그레이드 스탯을 저장할 수 있도록 필드 대폭 추가
    [System.Serializable]
    public class Weapon
    {
        public string name;
        public GameObject prefab;
        public Sprite icon; // UI에 띄울 아이콘

        [HideInInspector] public IObjectPool<GameObject> pool;

        // 런타임에 계산되어 캐싱될 업그레이드 스탯들
        [HideInInspector] public float cachedDamage;
        [HideInInspector] public float cachedSpeed;
        [HideInInspector] public float cachedManaCost;
        [HideInInspector] public float cachedFireDelay;
        [HideInInspector] public int cachedPierceCount;

        // 탄퍼짐 및 조준 관련
        [HideInInspector] public float cachedAimRatio;
        [HideInInspector] public float cachedSpreadHip;
        [HideInInspector] public float cachedSpreadAim;
    }

    [Header("설정")]
    // 이제 인스펙터에서 넣지 않고, 게임 시작 시 코드로 자동 채워집니다.
    public List<Weapon> weapons = new List<Weapon>();
    public Transform firePoint;
    public CinemachineCamera aimCamera;
    public PlayerController moveController;

    [Header("조준 카메라 설정")]
    public Transform cameraTarget;
    public float aimZoomSize = 5f;
    public float normalZoomSize = 7f;
    public float panDistance = 3f;
    public float zoomSpeed = 5f;

    [Header("무기 시각 효과")]
    public SpriteRenderer weaponRenderer;
    public float fadeOutSpeed = 5f;
    public float blinkSpeed = 20f;

    public int currentWeaponIndex = 0;
    private ManaSystem manaSystem;
    private Camera _mainCamera;
    private Animator anim;

    private bool _isFiring = false;
    private bool _isAiming = false;
    public float _lastFireTime = 0f;

    void Awake()
    {
        manaSystem = GetComponent<ManaSystem>();
        _mainCamera = Camera.main;
        anim = GetComponent<Animator>();

        if (moveController == null) moveController = GetComponent<PlayerController>();
        if (cameraTarget == null) cameraTarget = transform;
    }

    // [핵심 변경] Awake 대신 Start를 사용하여 DataManager가 먼저 세팅될 시간을 줍니다.
    void Start()
    {
        LoadEquippedWeapons();
    }

    // DataManager에서 장착된 무기만 쏙쏙 뽑아와서 세팅하는 함수
    void LoadEquippedWeapons()
    {
        weapons.Clear();

        // 1. DataManager의 세이브 데이터 중 '장착된' 무기만 찾습니다.
        foreach (var saveData in DataManager.Instance.weaponSaveList)
        {
            if (saveData.isEquipped && saveData.isUnlocked)
            {
                // 2. 도감(Database)에서 무기 원본 정보를 가져옵니다.
                WeaponInfo info = DataManager.Instance.weaponDatabase.Find(x => x.weaponID == saveData.weaponID);

                if (info != null)
                {
                    Weapon newWeapon = new Weapon();
                    newWeapon.name = info.weaponName;
                    newWeapon.prefab = info.projectilePrefab;
                    newWeapon.icon = info.weaponIcon; // 도감의 아이콘을 무기에 쥐어줌

                    // 3. 레벨에 따른 스탯 계산 및 캐싱
                    int levelMultiplier = saveData.level - 1;

                    // [플러스(+) 성장치 계산] 데미지, 탄속은 레벨에 비례해 더해줍니다.
                    newWeapon.cachedDamage = info.baseDamage + (levelMultiplier * info.damageGrowth);
                    newWeapon.cachedSpeed = info.baseSpeed + (levelMultiplier * info.speedGrowth);

                    // [마이너스(-) 성장치 계산] 딜레이, 마나, 탄퍼짐은 줄어들어야 하므로 빼줍니다.
                    // Mathf.Max를 사용하여 설정한 한계치 밑으로 떨어져 버그가 생기는 것을 방지합니다.
                    newWeapon.cachedFireDelay = Mathf.Max(info.baseFireDelay - (levelMultiplier * info.fireDelayReduction), info.minFireDelay);
                    newWeapon.cachedManaCost = Mathf.Max(info.baseManaCost - (levelMultiplier * info.manaCostReduction), info.minManaCost);

                    newWeapon.cachedSpreadHip = Mathf.Max(info.baseSpreadAngleHip - (levelMultiplier * info.spreadReduction), info.minSpreadAngle);
                    newWeapon.cachedSpreadAim = Mathf.Max(info.baseSpreadAngleAim - (levelMultiplier * info.spreadReduction), info.minSpreadAngle);

                    // 성장하지 않는 고정값들 
                    newWeapon.cachedPierceCount = info.basePierceCount;
                    newWeapon.cachedAimRatio = info.baseAimSlowdownRatio;

                    // 4. 오브젝트 풀링 세팅
                    newWeapon.pool = new ObjectPool<GameObject>(
                        createFunc: () =>
                        {
                            GameObject obj = Instantiate(newWeapon.prefab);
                            obj.GetComponent<ProjectileBehavior>().SetPool(newWeapon.pool);
                            return obj;
                        },
                        actionOnGet: (obj) =>
                        {
                            obj.SetActive(true);
                            obj.transform.position = firePoint != null ? firePoint.position : transform.position;
                        },
                        actionOnRelease: (obj) => obj.SetActive(false),
                        actionOnDestroy: (obj) => Destroy(obj),
                        defaultCapacity: 10, maxSize: 50
                    );

                    weapons.Add(newWeapon);
                }
            }
        }

        // 장착한 무기가 하나도 없을 경우의 예외 처리
        if (weapons.Count == 0)
        {
            Debug.LogWarning("장착된 무기가 없습니다! 기본 무기를 확인하세요.");
        }
    }

        

    void Update()
    {
        if (weapons.Count == 0) return; // 무기가 없으면 아무것도 안 함

        HandleWeaponVisibility();
        HandleCameraZoomAndPan();
        HandleWeaponSpecs();
        HandleFiring();
    }

    void HandleWeaponVisibility()
    {
        if (weaponRenderer == null) return;

        Color color = weaponRenderer.color;

        if (_isFiring)
        {
            Weapon currentWeapon = weapons[currentWeaponIndex];
            if (manaSystem.currentMana >= currentWeapon.cachedManaCost) color.a = 1f;
            else color.a = 0.65f + 0.35f * Mathf.Sin(Time.time * blinkSpeed);
        }
        else
        {
            color.a = Mathf.Lerp(color.a, 0f, Time.deltaTime * fadeOutSpeed);
        }

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
        _isFiring = Mouse.current.leftButton.isPressed;
        _isAiming = Mouse.current.rightButton.isPressed;

        if (anim != null) anim.SetBool("isAttacking", _isFiring);
        if (moveController != null) moveController.isAttacking = _isFiring;

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

    void OnSwitchWeapon(InputValue value)
    {
        if (weapons.Count <= 1) return; // 무기가 1개 이하면 교체 안 함

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

            float selectedSpread = _isAiming ? weapon.cachedSpreadAim : weapon.cachedSpreadHip;
            float randomAngle = Random.Range(-selectedSpread / 2f, selectedSpread / 2f);
            Vector2 finalDirection = Quaternion.Euler(0, 0, randomAngle) * baseDir;

            GameObject bullet = weapon.pool.Get();
            bullet.transform.position = firePoint != null ? firePoint.position : transform.position;

            ProjectileBehavior projectile = bullet.GetComponent<ProjectileBehavior>();

            // [핵심 변경] 발사 직전! DataManager에서 계산해둔 업그레이드 스탯을 주입합니다.
            if (projectile != null)
            {
                projectile.SetupWeaponStats(
                    weapon.cachedDamage,
                    weapon.cachedSpeed,
                    weapon.cachedFireDelay,
                    weapon.cachedManaCost,
                    weapon.cachedPierceCount
                );

                projectile.Launch(finalDirection);
            }
        }
    }
}
    /*[System.Serializable]
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
    }*/

