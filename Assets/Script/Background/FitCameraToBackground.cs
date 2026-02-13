using Unity.Cinemachine;
using UnityEngine;

public class FitCameraToBackground : MonoBehaviour
{
    public Camera cam;
    private SpriteRenderer sr;

    private Vector3 baseScale;
    private Vector3 basePos;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();

        baseScale = transform.localScale;
        basePos = transform.position;
    }

    void LateUpdate()
    {
        if (!cam || !sr || !sr.sprite) return;

        // 1) 화면 크기(월드)
        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;

        // 2) 스프라이트 원본 크기(월드, scale=1)
        Vector2 s = sr.sprite.bounds.size;

        // 3) Cover: 빈 공간 없게 확대
        float scale = Mathf.Max(camW / s.x, camH / s.y);
        transform.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, baseScale.z);

        // 4) 센터링: 카메라가 보는 중심에 배경을 맞춤 (이게 핵심)
        Vector3 p = transform.position;
        p.y = cam.transform.position.y;   //  위 빈 공간 제거 핵심
        // 필요하면 x도 따라가게 가능: p.x = cam.transform.position.x;
        transform.position = p;
    }
}

