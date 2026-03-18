using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("게임 진행 데이터")]
    public int currentStageIndex = 0;
    //public int coins = 0;

    [Header("마법석 데이터")]
    public float magicStoneCurrentHP = 1000f;
    public float magicStoneMaxHP = 1000f;

    [Header("재화")]
    public int coins = 1000; // 테스트용 초기 자금

    [Header("플레이어 능력치 (여관 업그레이드용)")]
    public float playerMaxSpeed = 8f;
    public float playerMaxMana = 100f;
    public float playerManaRegenRate = 15f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }

    // [추가] 1. 게임 저장하기 (여관이나 전투 종료 시 호출해주면 좋습니다)
    public void SaveGame()
    {
        PlayerPrefs.SetInt("HasSave", 1); // 세이브 파일 존재 여부 마커

        // 게임 진행 데이터
        PlayerPrefs.SetInt("StageIndex", currentStageIndex);

        // 마법석 데이터
        PlayerPrefs.SetFloat("MagicStoneCurrentHP", magicStoneCurrentHP);
        PlayerPrefs.SetFloat("MagicStoneMaxHP", magicStoneMaxHP);

        // 재화
        PlayerPrefs.SetInt("Coins", coins);

        // 플레이어 능력치
        PlayerPrefs.SetFloat("PlayerMaxSpeed", playerMaxSpeed);
        PlayerPrefs.SetFloat("PlayerMaxMana", playerMaxMana);
        PlayerPrefs.SetFloat("PlayerManaRegenRate", playerManaRegenRate);

        PlayerPrefs.Save(); // 디스크에 확정 저장
        Debug.Log("게임의 모든 데이터가 성공적으로 저장되었습니다!");
    }

    // [추가] 2. 게임 불러오기 (이어하기 누를 때 호출)
    public void LoadGame()
    {
        if (PlayerPrefs.GetInt("HasSave", 0) == 1)
        {
            // GetInt, GetFloat의 두 번째 인자는 "만약 저장된 값이 없을 경우 사용할 기본값"입니다.
            currentStageIndex = PlayerPrefs.GetInt("StageIndex", 0);

            magicStoneCurrentHP = PlayerPrefs.GetFloat("MagicStoneCurrentHP", 1000f);
            magicStoneMaxHP = PlayerPrefs.GetFloat("MagicStoneMaxHP", 1000f);

            coins = PlayerPrefs.GetInt("Coins", 1000);

            playerMaxSpeed = PlayerPrefs.GetFloat("PlayerMaxSpeed", 8f);
            playerMaxMana = PlayerPrefs.GetFloat("PlayerMaxMana", 100f);
            playerManaRegenRate = PlayerPrefs.GetFloat("PlayerManaRegenRate", 15f);

            Debug.Log("게임 데이터를 성공적으로 불러왔습니다!");
        }
    }

    // 3. 데이터 초기화 (새로하기 누를 때 호출)
    public void ResetData()
    {
        PlayerPrefs.DeleteAll(); // 기기에 저장된 모든 세이브 삭제

        // DataManager가 들고 있는 현재 값들도 모두 튜토리얼(초기) 상태로 되돌립니다.
        currentStageIndex = 0;

        magicStoneCurrentHP = 1000f;
        magicStoneMaxHP = 1000f;

        coins = 1000; // 테스트용 초기 자금 (실제 출시 때는 0으로 바꾸시면 됩니다)

        playerMaxSpeed = 8f;
        playerMaxMana = 100f;
        playerManaRegenRate = 15f;

        Debug.Log("게임 데이터가 초기화되었습니다. (새 게임 준비 완료)");
    }
    
}
