using UnityEngine;

public class MagicStoneManager : MonoBehaviour
{
    // 전역에서 접근 가능한 싱글톤 인스턴스
    public static MagicStoneManager Instance { get; private set; }

    // AI들이 목표로 삼을 위치
    public Transform StoneTransform { get; private set; }

    // [최적화] 표면 거리 계산을 위해 콜라이더를 미리 캐싱해 둡니다.
    public Collider2D StoneCollider { get; private set; }

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            StoneTransform = transform;
            StoneCollider = GetComponent<Collider2D>();
        }
        else
        {
            // 씬에 이미 Magic Stone이 있다면 중복 생성된 객체 파괴
            Destroy(gameObject);
        }
    }
}
