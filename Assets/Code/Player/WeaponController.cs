using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("연결")]
    public Transform weaponVisual; // 총 이미지 (뒤집기용)

    private Camera _mainCamera;

    private Vector3 originalScale; // [수정] 원래 크기를 기억할 변수 추가

    void Awake()
    {
        _mainCamera = Camera.main;

        // 게임 시작 시 설정된 크기를 기억함
        originalScale = weaponVisual.localScale;
    }

    void Update()
    {
        RotateGun();
    }

    void RotateGun()
    {
        // 1. 마우스 위치 가져오기 (스크린 좌표 -> 월드 좌표)
        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // 2. 방향 벡터 계산 (마우스 - 내 위치)
        Vector2 direction = mousePos - (Vector2)transform.position;

        // 3. 각도 계산 (Atan2 사용)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 4. 회전 적용 (Pivot을 돌림)
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 5. 총 뒤집기 (왼쪽을 볼 때 총이 거꾸로 보이는 현상 방지)
        // 각도가 90도보다 크거나 -90도보다 작으면 (왼쪽을 보고 있으면)
        if (Mathf.Abs(angle) > 90)
        {
            // 왼쪽 볼 때: Y축만 뒤집음 (원래 Y크기에 -1 곱하기)
            weaponVisual.localScale = new Vector3(originalScale.x, -originalScale.y, originalScale.z);
        }
        else
        {
            // 오른쪽 볼 때: 원래 크기 그대로 복구
            weaponVisual.localScale = originalScale;
        }
    }
}
