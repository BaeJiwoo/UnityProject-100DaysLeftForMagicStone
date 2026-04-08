using TMPro;
using UnityEngine;

public class FloatingDamage : MonoBehaviour
{
    public float moveSpeed = 2f;   // 위로 올라가는 속도
    public float destroyTime = 1f; // 사라지는 시간

    private TextMeshPro textMesh;
    private Color textColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        textColor = textMesh.color;
    }

    void Start()
    {
        // 지정된 시간 뒤에 오브젝트 삭제
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 1. 위로 이동
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 2. 서서히 투명해지기 (Fade Out)
        textColor.a -= Time.deltaTime / destroyTime;
        textMesh.color = textColor;
    }

    // 외부에서 데미지 숫자를 설정할 때 호출하는 함수
    public void Setup(float damageAmount)
    {
        if (textMesh != null)
        {
            textMesh.text = damageAmount.ToString("F0"); // 소수점 제외하고 정수만 표기
        }
    }
}
