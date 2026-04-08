using UnityEngine;

public class MercenarySpawner : MonoBehaviour
{
    public Transform[] spawnPoints;

    void Start()
    {
        SpawnEquippedMercenaries();
    }

    void SpawnEquippedMercenaries()
    {
        int spawnIndex = 0;

        // 세이브 데이터(DataManager)에서 장착된 용병만 찾습니다.
        foreach (MercenarySaveData saveData in DataManager.Instance.mercenarySaveList)
        {
            if (saveData.isEquipped && saveData.isUnlocked)
            {
                if (spawnIndex >= spawnPoints.Length) break;

                // 도감에서 이 용병의 ScriptableObject 데이터를 가져옵니다.
                MercenaryInfo info = DataManager.Instance.mercenaryDatabase.Find(x => x.mercID == saveData.mercID);

                if (info != null && info.prefab != null)
                {
                    // 1. 도감에 등록된 프리팹 소환
                    GameObject spawnedAlly = Instantiate(info.prefab, spawnPoints[spawnIndex].position, Quaternion.identity);

                    // 2. AI 스크립트를 가져와서 도감 정보(info)와 세이브 레벨(saveData.level)을 주입!
                    BaseAllyAI allyCtrl = spawnedAlly.GetComponent<BaseAllyAI>();
                    if (allyCtrl != null)
                    {
                        allyCtrl.InitializeData(info, saveData.level);
                    }

                    spawnIndex++;
                }
            }
        }
    }
}