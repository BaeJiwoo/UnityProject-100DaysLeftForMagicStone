using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("이 스포너의 고유 ID (StageData의 spawnerID와 매칭됩니다)")]
    public int spawnerID = 1;

    private float hpMultiplier;
    private float damageMultiplier;
    private float coinMultiplier;

    // 매니저로부터 이 스테이지에 해당하는 데이터를 받아 실행
    public void StartWave(List<SpawnGroup> waveData, float hpMult, float dmgMult, float coinMult)
    {
        hpMultiplier = hpMult;
        damageMultiplier = dmgMult;
        coinMultiplier = coinMult;

        StartCoroutine(SpawnRoutine(waveData));
    }

    public void StopSpawner()
    {
        StopAllCoroutines();
    }

    private IEnumerator SpawnRoutine(List<SpawnGroup> spawnWaves)
    {
        for (int i = 0; i < spawnWaves.Count; i++)
        {
            SpawnGroup currentGroup = spawnWaves[i];
            if (currentGroup.delayBeforeStart > 0)
                yield return new WaitForSeconds(currentGroup.delayBeforeStart);

            for (int j = 0; j < currentGroup.spawnCount; j++)
            {
                if (currentGroup.enemyPrefab != null)
                {
                    GameObject enemyObj = Instantiate(currentGroup.enemyPrefab, transform.position, Quaternion.identity);

                    // ========================================================
                    // [중요 1] 적이 생성되는 바로 이 타이밍에 호출되어야 합니다!
                    if (StageManager.Instance != null)
                        StageManager.Instance.OnEnemySpawned();
                    // ========================================================

                    BaseAI enemyAI = enemyObj.GetComponent<BaseAI>();
                    if (enemyAI != null)
                    {
                        // 스포너가 보관 중이던 배율(hpMultiplier, damageMultiplier)을 전달합니다.
                        enemyAI.ApplyStatMultipliers(hpMultiplier, damageMultiplier, coinMultiplier);
                    }
                }

                if (j < currentGroup.spawnCount - 1)
                    yield return new WaitForSeconds(currentGroup.spawnInterval);
            }
        }

        // ========================================================
        // [중요 2] 모든 for문(루프)이 완전히 끝난 뒤, 
        // 제일 마지막에 딱 한 번만 호출되어야 합니다!
        if (StageManager.Instance != null)
            StageManager.Instance.OnSpawnerFinished();
        // ========================================================
    }
}













    // [인스펙터에서 관리하기 쉽게 만든 커스텀 클래스]
    /* [System.Serializable]
     public class SpawnGroup
     {
         [Tooltip("인스펙터에서 알아보기 쉬운 메모용 이름")]
         public string groupName;

         [Tooltip("소환할 적의 프리팹")]
         public GameObject enemyPrefab;

         [Tooltip("소환할 적의 수")]
         public int spawnCount = 1;

         [Tooltip("이 그룹이 시작되기 전 대기 시간 (초)")]
         public float delayBeforeStart = 2f;

         [Tooltip("이 그룹 내에서 적들이 연속으로 나올 때의 간격 (초)")]
         public float spawnInterval = 1f;

         [Tooltip("특정 위치에서 소환하고 싶을 때 사용 (비워두면 스포너 위치에서 소환)")]
         public Transform customSpawnPoint;
     }

     [Header("스폰 웨이브 설정")]
     public List<SpawnGroup> spawnWaves; // 적 생성 순서 리스트

     [Header("스폰 옵션")]
     public bool autoStart = true;       // 시작하자마자 스폰할 것인가?
     public bool loopWaves = false;      // 모든 웨이브가 끝나면 처음부터 다시 반복할 것인가?

     private void Start()
     {
         if (autoStart)
         {
             StartSpawner();
         }
     }

     // 외부(트리거 등)에서 스포너를 작동시키고 싶을 때 호출하는 함수
     public void StartSpawner()
     {
         StartCoroutine(SpawnRoutine());
     }

     // 외부에서 스폰을 강제로 멈출 때 호출
     public void StopSpawner()
     {
         StopAllCoroutines();
     }

     // 시간에 따라 순차적으로 적을 생성하는 핵심 코루틴
     private IEnumerator SpawnRoutine()
     {
         do
         {
             // 리스트에 등록된 그룹을 순서대로 하나씩 실행
             for (int i = 0; i < spawnWaves.Count; i++)
             {
                 SpawnGroup currentGroup = spawnWaves[i];

                 // 1. 해당 그룹이 시작되기 전까지 대기 (타이밍 조절)
                 if (currentGroup.delayBeforeStart > 0)
                 {
                     yield return new WaitForSeconds(currentGroup.delayBeforeStart);
                 }

                 // 2. 설정된 마리 수(spawnCount)만큼 적 생성
                 for (int j = 0; j < currentGroup.spawnCount; j++)
                 {
                     // 생성 위치 결정 (커스텀 위치가 지정되어 있으면 그곳, 아니면 스포너 자기 위치)
                     Transform spawnPos = currentGroup.customSpawnPoint != null ? currentGroup.customSpawnPoint : transform;

                     // 적 생성
                     if (currentGroup.enemyPrefab != null)
                     {
                         Instantiate(currentGroup.enemyPrefab, spawnPos.position, Quaternion.identity);
                     }

                     // 마지막 적이 아니라면, 다음 적이 나오기 전까지 간격(Interval)만큼 대기
                     if (j < currentGroup.spawnCount - 1)
                     {
                         yield return new WaitForSeconds(currentGroup.spawnInterval);
                     }
                 }
             }

             // 루프 설정이 켜져 있으면 처음부터 다시 시작, 꺼져 있으면 반복문 종료
         } while (loopWaves);

         Debug.Log("모든 스폰이 종료되었습니다.");
     }*/
