using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    public static ParallaxController Instance { get; private set; }
    // 1. 단일 배경 레이어 데이터
    [System.Serializable]
    public class ParallaxLayer
    {
        [Tooltip("에디터에 배치한 중앙 원본 이미지")]
        public SpriteRenderer centerSprite;

        [Tooltip("1 = 카메라와 똑같이 이동(먼 하늘), 0 = 일반 이동(전경)")]
        public float parallaxMultiplier;

        [HideInInspector] public float startPosX;
        [HideInInspector] public float length;

        // [핵심] 스크립트가 관리할 3장의 패널 (0: 좌측, 1: 중앙 원본, 2: 우측)
        [HideInInspector] public Transform[] panels;
    }

    // 2. 스테이지별 배경 묶음(Bundle) 데이터
    [System.Serializable]
    public class BackgroundBundle
    {
        public string bundleName;
        public GameObject bundleRoot;
        public List<ParallaxLayer> layers;
    }

    [Header("카메라 설정")]
    public Camera cam;

    [Header("스테이지 배경 묶음")]
    public List<BackgroundBundle> bundles;
    public int currentBundleIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (cam == null) cam = Camera.main;

        InitializeAllBundles();
        // Awake에서는 ChangeBundle을 부르지 않습니다. (StageManager가 불러줄 예정)
    }

    // 시작할 때 모든 배경의 좌/우 복제본을 자동으로 생성합니다.
    void InitializeAllBundles()
    {
        foreach (var bundle in bundles)
        {
            foreach (var layer in bundle.layers)
            {
                if (layer.centerSprite != null)
                {
                    // 1. 원본 이미지의 실제 너비 측정 (Scale이 적용된 실제 월드 크기)
                    layer.length = layer.centerSprite.bounds.size.x;
                    layer.startPosX = layer.centerSprite.transform.position.x;

                    // 2. 3개의 패널 배열 생성
                    layer.panels = new Transform[3];

                    // 중앙 패널 (우리가 배치한 원본)
                    layer.panels[1] = layer.centerSprite.transform;

                    // 좌측 패널 (자동 복제)
                    GameObject leftClone = Instantiate(layer.centerSprite.gameObject, layer.centerSprite.transform.parent);
                    leftClone.name = layer.centerSprite.name + "_LeftClone";
                    layer.panels[0] = leftClone.transform;

                    // 우측 패널 (자동 복제)
                    GameObject rightClone = Instantiate(layer.centerSprite.gameObject, layer.centerSprite.transform.parent);
                    rightClone.name = layer.centerSprite.name + "_RightClone";
                    layer.panels[2] = rightClone.transform;
                }
            }
        }
    }

    public void ChangeBundle(int index)
    {
        if (index < 0 || index >= bundles.Count) return;

        // 모든 묶음 끄기 -> 선택한 묶음 켜기
        for (int i = 0; i < bundles.Count; i++)
        {
            if (bundles[i].bundleRoot != null)
            {
                bundles[i].bundleRoot.SetActive(i == index);
            }
        }
        currentBundleIndex = index;
    }

    void LateUpdate()
    {
        if (bundles.Count == 0 || bundles[currentBundleIndex].layers == null) return;

        BackgroundBundle activeBundle = bundles[currentBundleIndex];

        foreach (var layer in activeBundle.layers)
        {
            if (layer.panels == null || layer.panels.Length < 3) continue;

            // 카메라가 배경에 대해 '상대적으로' 얼마나 이동했는지
            float temp = (cam.transform.position.x * (1 - layer.parallaxMultiplier));

            // 실제 배경이 카메라를 따라가야 하는 거리 (패럴랙스)
            float dist = (cam.transform.position.x * layer.parallaxMultiplier);

            // [무한 스크롤 핵심] 상대적 이동 거리가 이미지 너비를 넘어가면 기준점 이동
            if (temp > layer.startPosX + layer.length)
            {
                layer.startPosX += layer.length;
            }
            else if (temp < layer.startPosX - layer.length)
            {
                layer.startPosX -= layer.length;
            }

            // 기준점(startPosX)을 중심으로 3장의 이미지를 항상 일렬로 정렬
            float originalY = layer.panels[1].position.y;
            float originalZ = layer.panels[1].position.z;

            // 좌, 중, 우 패널 위치 갱신
            layer.panels[0].position = new Vector3(layer.startPosX - layer.length + dist, originalY, originalZ);
            layer.panels[1].position = new Vector3(layer.startPosX + dist, originalY, originalZ);
            layer.panels[2].position = new Vector3(layer.startPosX + layer.length + dist, originalY, originalZ);
        }
    }
}
