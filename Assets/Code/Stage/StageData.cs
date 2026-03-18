using System.Collections.Generic;
using UnityEngine;

// [SpawnGroup] 기존 코드를 약간 수정 (SO는 씬의 Transform을 저장할 수 없어 위치 변수 제거)
[System.Serializable]
public class SpawnGroup
{
    public string groupName;
    public GameObject enemyPrefab;
    public int spawnCount = 1;
    public float delayBeforeStart = 2f;
    public float spawnInterval = 1f;
}

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game Data/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("스테이지 난이도 설정")]
    public float enemyHpMultiplier = 1.0f;      // 적 체력 증가 배율 (예: 1.5면 150%)
    public float enemyDamageMultiplier = 1.0f;  // 적 공격력 증가 배율

    // [추가] 배경 묶음 설정
    [Header("배경 설정")]
    [Tooltip("ParallaxController에 등록된 묶음(Bundle)의 인덱스 번호 (0부터 시작)")]
    public int backgroundBundleIndex = 0;

    [System.Serializable]
    public class SpawnerSetup
    {
        [Tooltip("이 데이터를 받을 스포너의 고유 ID (예: 1=왼쪽 스포너, 2=오른쪽 스포너)")]
        public int spawnerID;
        public List<SpawnGroup> spawnWaves;
    }

    [Header("스포너별 소환 설정")]
    public List<SpawnerSetup> spawnerSetups;
}