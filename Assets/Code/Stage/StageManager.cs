using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("스테이지 데이터 목록")]
    public List<StageData> allStages;

    private EnemySpawner[] allSpawnersInScene;
    private int activeEnemiesCount = 0;
    private int activeSpawnersCount = 0;

    [Header("UI 설정")]
    public TMP_Text stageText;

    public GameObject stageClearPanel;

    [Header("디버그 설정 (테스트용)")]
    [Tooltip("체크하면 아래 지정한 스테이지 번호로 강제 시작합니다.")]
    public bool useDebugStage = false;

    [Tooltip("테스트할 스테이지 인덱스 (0 = 1스테이지, 1 = 2스테이지)")]
    public int debugStageIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (stageClearPanel != null) stageClearPanel.SetActive(false);

        allSpawnersInScene = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);

        // ==========================================
        // [수정] 코드가 두 번 중복 작성되어 있던 것을 하나로 깔끔하게 줄였습니다.
        StartCurrentStage();
        // ==========================================
    }

    public void StartCurrentStage()
    {
        // [추가] 디버그 모드가 켜져 있다면, DataManager의 현재 스테이지 값을 강제로 바꿉니다.
        if (useDebugStage && DataManager.Instance != null)
        {
            DataManager.Instance.currentStageIndex = debugStageIndex;
            Debug.LogWarning($"[디버그 모드 작동 중] 강제로 DAY {debugStageIndex + 1} 스테이지를 로드합니다!");
        }

        // DataManager에 저장된 진짜 스테이지 번호를 가져옵니다.
        int stageIndex = DataManager.Instance.currentStageIndex;

        if (stageIndex >= allStages.Count)
        {
            Debug.Log("모든 스테이지를 클리어했습니다!");
            return;
        }

        if (stageText != null)
        {
            stageText.text = "DAY" + (stageIndex + 1).ToString();
        }

        StageData currentStageData = allStages[stageIndex];

        // [추가] ParallaxController에게 이 스테이지의 배경 인덱스로 변경하라고 명령!
        if (ParallaxController.Instance != null)
        {
            ParallaxController.Instance.ChangeBundle(currentStageData.backgroundBundleIndex);
        }

        activeEnemiesCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        activeSpawnersCount = 0;

        
        //  currentStageIndex 대신 실제 번호인 stageIndex를 출력하도록 고쳤습니다!
        Debug.Log($"스테이지 {stageIndex + 1} 시작! (HP배율: {currentStageData.enemyHpMultiplier})");
        

        foreach (EnemySpawner spawner in allSpawnersInScene)
        {
            StageData.SpawnerSetup setup = currentStageData.spawnerSetups.Find(s => s.spawnerID == spawner.spawnerID);

            if (setup != null && setup.spawnWaves.Count > 0)
            {
                activeSpawnersCount++;
                spawner.StartWave(setup.spawnWaves, currentStageData.enemyHpMultiplier, currentStageData.enemyDamageMultiplier);
            }
        }

        if (activeSpawnersCount == 0 && activeEnemiesCount == 0)
        {
            CheckStageClear();
        }
    }

    public void OnEnemySpawned() { activeEnemiesCount++; }

    public void OnEnemyDied()
    {
        activeEnemiesCount--;
        CheckStageClear();
    }

    public void OnSpawnerFinished()
    {
        activeSpawnersCount--;
        CheckStageClear();
    }

    private void CheckStageClear()
    {
        if (activeSpawnersCount <= 0 && activeEnemiesCount <= 0)
        {
            Debug.Log("스테이지 클리어! 선택지 UI를 띄웁니다.");

            MagicStone stone = FindAnyObjectByType<MagicStone>();
            if (stone != null)
            {
                DataManager.Instance.magicStoneCurrentHP = stone.currentHealth;
            }

            DataManager.Instance.currentStageIndex++;

            if (stageClearPanel != null)
            {
                stageClearPanel.SetActive(true);
            }

            Time.timeScale = 0f;
            DataManager.Instance.SaveGame();
        }
    }

    public void GoToHotelScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("HotelScene");
    }

    public void GoToWeaponShopScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("WeaponShopScene");
    }

    public void GoToMercenaryScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MercenaryScene");
    }

    public void SkipAndNextBattle()
    {
        Time.timeScale = 1f;

        if (DataManager.Instance != null)
        {
            int bonusCoins = Mathf.RoundToInt(DataManager.Instance.coins * 1.5f);
            DataManager.Instance.coins = bonusCoins;
            Debug.Log($"스킵 보너스! 코인이 1.5배가 되어 총 {DataManager.Instance.coins} G가 되었습니다.");
        }

        SceneManager.LoadScene("BattleScene");
    }
}
