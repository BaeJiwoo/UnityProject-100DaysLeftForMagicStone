using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenuManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Button continueButton;
    public TMP_Text continueText;
    public TMP_Text stageNumberText;

    // CanvasGroup은 버튼과 그 안의 글자 투명도를 한 번에 조절해주는 아주 유용한 컴포넌트입니다.
    public CanvasGroup continueCanvasGroup;

    void Start()
    {
        CheckSaveData();
    }

    // 세이브 데이터를 확인하여 버튼 상태를 변경하는 함수
    private void CheckSaveData()
    {
        // PlayerPrefs에 "HasSave"라는 기록이 1로 저장되어 있는지 확인
        if (PlayerPrefs.GetInt("HasSave", 0) == 1)
        {
            // 플레이 기록이 있을 때
            int savedStage = PlayerPrefs.GetInt("StageIndex", 0) + 1; // 0번 인덱스가 1스테이지이므로 +1

            continueButton.interactable = true; // 버튼 클릭 활성화
            continueCanvasGroup.alpha = 1f;     // 불투명하게 (100% 보임)

            // 두 개의 텍스트를 각각 분리해서 표기합니다.
            if (continueText != null) continueText.text = "Continue";
            if (stageNumberText != null) stageNumberText.text = $"DAY {savedStage}";
        }
        else
        {
            // 플레이 기록이 없을 때
            continueButton.interactable = false; // 버튼 클릭 비활성화
            continueCanvasGroup.alpha = 0.5f;    // 반투명하게 (50% 보임)

            // 기록이 없으면 Continue만 남기고, DAY 표기는 아예 비워버립니다.
            if (continueText != null) continueText.text = "Continue";
            if (stageNumberText != null) stageNumberText.text = "";
        }
    }

    // [새로하기] 버튼 클릭 시
    public void OnClickNewGame()
    {
        // DataManager가 씬에 있다면 초기화 시킴
        if (DataManager.Instance != null)
        {
            DataManager.Instance.ResetData();
        }

        // 전투 씬으로 이동 (튜토리얼 씬이 있다면 거기로 이동해도 좋습니다)
        SceneManager.LoadScene("BattleScene");
    }

    // [이어하기] 버튼 클릭 시
    public void OnClickContinue()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.LoadGame();
        }

        // 전투 씬으로 이동
        SceneManager.LoadScene("BattleScene");
    }

    // [옵션] 버튼 클릭 시
    public void OnClickOptions()
    {
        Debug.Log("옵션 창 열기!");
        // TODO: 나중에 볼륨 조절 등의 옵션 패널을 SetActive(true) 하는 로직 추가
    }
}
