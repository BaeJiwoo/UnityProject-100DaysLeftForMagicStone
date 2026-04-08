using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 1. 무기 세이브용 클래스 
[System.Serializable]
public class WeaponSaveData
{
    public string weaponID;
    public int level = 1;
    public bool isUnlocked = false;
    public bool isEquipped = false;
}

[System.Serializable]
public class WeaponSaveDataWrapper
{
    public List<WeaponSaveData> list = new List<WeaponSaveData>();
}

// ==========================================
// [1] 용병 세이브용 데이터 클래스 정의
// ==========================================
[System.Serializable]
public class MercenarySaveData
{
    public string mercID;           // 용병 고유 ID
    public int level = 1;           // 현재 레벨
    public bool isUnlocked = false; // 고용 여부
    public bool isEquipped = false; // 장착 여부
}

// JsonUtility는 List를 최상단에서 바로 직렬화하지 못하므로, 
// 리스트를 감싸주는 포장지(Wrapper) 클래스가 하나 필요합니다.
[System.Serializable]
public class MercenarySaveDataWrapper
{
    public List<MercenarySaveData> list = new List<MercenarySaveData>();
}
// ==========================================


public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("게임 진행 데이터")]
    public int currentStageIndex = 0;

    [Header("마법석 데이터")]
    public float magicStoneCurrentHP = 1000f;
    public float magicStoneMaxHP = 1000f;

    [Header("재화")]
    public int coins = 1000; // 테스트용 초기 자금

    [Header("플레이어 능력치")]
    public float playerMaxSpeed = 8f;
    public float playerMaxMana = 100f;
    public float playerManaRegenRate = 15f;

    // ==========================================
    // [추가] 용병 관련 데이터
    // ==========================================
    [Header("용병 도감 (고정 데이터)")]
    public List<MercenaryInfo> mercenaryDatabase; // 인스펙터에서 ScriptableObject 할당

    [Header("유저 용병 진행도 (세이브 데이터)")]
    public List<MercenarySaveData> mercenarySaveList = new List<MercenarySaveData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // ==========================================
            // [수정] 상점이 Start()에서 데이터를 찾기 전에, 
            // Awake() 단계에서 미리 모든 세팅을 끝내버립니다.
            // ==========================================

            // 1. 깡통 데이터(1레벨) 미리 채우기
            if (mercenarySaveList == null || mercenarySaveList.Count == 0) InitializeMercenarySaveData();
            if (weaponSaveList == null || weaponSaveList.Count == 0) InitializeWeaponSaveData();

            // 2. 혹시 예전에 저장해둔 세이브 파일이 있다면 덮어쓰기
            LoadGame();

            // 3. 도감에 새로 추가된 신규 무기/용병이 있다면 세이브에 끼워넣기 (동기화)
            SyncNewData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
      /*  // 게임 시작 시 세이브된 리스트가 비어있다면, 도감을 바탕으로 기본 틀을 생성합니다.
        if (mercenarySaveList == null || mercenarySaveList.Count == 0)
        {
            InitializeMercenarySaveData();
        }
        // 2. 무기 세이브 리스트가 비어있다면 초기화
        if (weaponSaveList == null || weaponSaveList.Count == 0)
        {
            InitializeWeaponSaveData();
        }*/
    }

    // [핵심 추가] 업데이트로 추가된 신규 무기/용병을 기존 세이브에 병합하는 함수
    // ==========================================
    private void SyncNewData()
    {
        // 1. 무기 동기화
        foreach (var info in weaponDatabase)
        {
            // 세이브 리스트를 뒤져서 도감에 있는 무기(info.weaponID)가 없다면?
            if (weaponSaveList.FirstOrDefault(x => x.weaponID == info.weaponID) == null)
            {
                // 레벨 1짜리 빈 세이브 슬롯을 새로 생성해서 끼워넣음
                weaponSaveList.Add(new WeaponSaveData { weaponID = info.weaponID });
                Debug.Log($"[데이터 동기화] 신규 무기 '{info.weaponName}'가 세이브 파일에 추가되었습니다.");
            }
        }

        // 2. 용병 동기화 (용병 쪽도 똑같이 에러가 날 수 있으니 미리 예방)
        foreach (var info in mercenaryDatabase)
        {
            if (mercenarySaveList.FirstOrDefault(x => x.mercID == info.mercID) == null)
            {
                mercenarySaveList.Add(new MercenarySaveData { mercID = info.mercID });
                Debug.Log($"[데이터 동기화] 신규 동료 '{info.mercName}'가 세이브 파일에 추가되었습니다.");
            }
        }
    }

    private void InitializeMercenarySaveData()
    {
        mercenarySaveList.Clear();
        if (mercenaryDatabase != null)
        {
            foreach (var info in mercenaryDatabase)
            {
                mercenarySaveList.Add(new MercenarySaveData { mercID = info.mercID });
            }
        }
    }

    // [추가] 무기 관련 데이터
    // ==========================================
    [Header("무기 도감 (고정 데이터)")]
    public List<WeaponInfo> weaponDatabase;

    [Header("유저 무기 진행도 (세이브 데이터)")]
    public List<WeaponSaveData> weaponSaveList = new List<WeaponSaveData>();



    private void InitializeWeaponSaveData()
    {
        weaponSaveList.Clear();
        if (weaponDatabase != null && weaponDatabase.Count > 0)
        {
            for (int i = 0; i < weaponDatabase.Count; i++)
            {
                WeaponInfo info = weaponDatabase[i];
                WeaponSaveData newData = new WeaponSaveData { weaponID = info.weaponID };

                // [추가] 리스트의 첫 번째 무기(인덱스 0)는 기본으로 지급하고 장착시킵니다.
                if (i == 0)
                {
                    newData.isUnlocked = true;
                    newData.isEquipped = true;
                }

                weaponSaveList.Add(newData);
            }
        }
    }

    // 1. 게임 저장하기
    public void SaveGame()
    {
        PlayerPrefs.SetInt("HasSave", 1);

        PlayerPrefs.SetInt("StageIndex", currentStageIndex);
        PlayerPrefs.SetFloat("MagicStoneCurrentHP", magicStoneCurrentHP);
        PlayerPrefs.SetFloat("MagicStoneMaxHP", magicStoneMaxHP);
        PlayerPrefs.SetInt("Coins", coins);
        PlayerPrefs.SetFloat("PlayerMaxSpeed", playerMaxSpeed);
        PlayerPrefs.SetFloat("PlayerMaxMana", playerMaxMana);
        PlayerPrefs.SetFloat("PlayerManaRegenRate", playerManaRegenRate);

        // [핵심 추가] 용병 리스트를 JSON 문자열로 변환하여 저장
        MercenarySaveDataWrapper wrapper = new MercenarySaveDataWrapper { list = mercenarySaveList };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("MercenaryDataList", json);

        // [추가] 무기 리스트 JSON 저장
        WeaponSaveDataWrapper weaponWrapper = new WeaponSaveDataWrapper { list = weaponSaveList };
        PlayerPrefs.SetString("WeaponDataList", JsonUtility.ToJson(weaponWrapper));

       

        PlayerPrefs.Save();
        Debug.Log("게임의 모든 데이터(용병 포함)가 성공적으로 저장되었습니다!");
    }

    // 2. 게임 불러오기
    public void LoadGame()
    {
        if (PlayerPrefs.GetInt("HasSave", 0) == 1)
        {
            currentStageIndex = PlayerPrefs.GetInt("StageIndex", 0);
            magicStoneCurrentHP = PlayerPrefs.GetFloat("MagicStoneCurrentHP", 1000f);
            magicStoneMaxHP = PlayerPrefs.GetFloat("MagicStoneMaxHP", 1000f);
            coins = PlayerPrefs.GetInt("Coins", 1000);
            playerMaxSpeed = PlayerPrefs.GetFloat("PlayerMaxSpeed", 8f);
            playerMaxMana = PlayerPrefs.GetFloat("PlayerMaxMana", 100f);
            playerManaRegenRate = PlayerPrefs.GetFloat("PlayerManaRegenRate", 15f);

            // [핵심 추가] JSON 문자열을 가져와서 다시 리스트로 변환
            string json = PlayerPrefs.GetString("MercenaryDataList", "");
            if (!string.IsNullOrEmpty(json))
            {
                MercenarySaveDataWrapper wrapper = JsonUtility.FromJson<MercenarySaveDataWrapper>(json);
                if (wrapper != null && wrapper.list != null)
                {
                    mercenarySaveList = wrapper.list;
                }
            }

            string weaponJson = PlayerPrefs.GetString("WeaponDataList", "");
            if (!string.IsNullOrEmpty(weaponJson))
            {
                WeaponSaveDataWrapper weaponWrapper = JsonUtility.FromJson<WeaponSaveDataWrapper>(weaponJson);
                if (weaponWrapper != null && weaponWrapper.list != null) weaponSaveList = weaponWrapper.list;
            }

            Debug.Log("게임 데이터를 성공적으로 불러왔습니다!");
        }
    }

    // 3. 데이터 초기화
    public void ResetData()
    {
        PlayerPrefs.DeleteAll();

        currentStageIndex = 0;
        magicStoneCurrentHP = 1000f;
        magicStoneMaxHP = 1000f;
        coins = 1000;
        playerMaxSpeed = 8f;
        playerMaxMana = 100f;
        playerManaRegenRate = 15f;

        // [추가] 용병 데이터도 튜토리얼 상태로 완전 초기화
        InitializeMercenarySaveData();

        Debug.Log("게임 데이터가 초기화되었습니다. (새 게임 준비 완료)");
    }

    // ==========================================
    // 4. [도우미 함수] 특정 용병의 현재 공격력 계산
    // ==========================================
    public float GetCurrentAttack(string id)
    {
        if (mercenaryDatabase == null) return 0f;

        MercenaryInfo info = mercenaryDatabase.FirstOrDefault(x => x.mercID == id);
        MercenarySaveData save = mercenarySaveList.FirstOrDefault(x => x.mercID == id);

        if (info != null && save != null)
        {
            return info.baseAttackPower + ((save.level - 1) * info.attackGrowth);
        }
        return 0f;
    }

    // [추가] 특정 무기의 현재 공격력 계산 함수
    public float GetWeaponCurrentDamage(string id)
    {
        WeaponInfo info = weaponDatabase.FirstOrDefault(x => x.weaponID == id);
        WeaponSaveData save = weaponSaveList.FirstOrDefault(x => x.weaponID == id);
        if (info != null && save != null) return info.baseDamage + ((save.level - 1) * info.damageGrowth);
        return 0f;
    }
}