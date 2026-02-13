using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    private float startPos, length;
    public GameObject cam;
    public float parallaxEffect; // 카메라 대비 배경이 움직여야 하는 속도

    void Start()
    {
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // 카메라 움직임에 따른 배경 이동 거리 계산  
        float distance = cam.transform.position.x * parallaxEffect; // 0: 카메라 추적, 1: 움직임 없음, 0.5: 절반 이동
        float movement = cam.transform.position.x * (1 - parallaxEffect);

        transform.position = new Vector3(startPos + distance, transform.position.y, transform.position.z);

        if (movement > startPos +  length * 0.5f)
        {
            startPos += length;
        }
        else if (movement < startPos - length * 0.5f)
        {
            startPos -= length;
        }
    }
}
