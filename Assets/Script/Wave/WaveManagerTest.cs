using UnityEngine;

public class WaveManagerTest : MonoBehaviour
{
    private PlayerHUDController hudController;
    private int currentWave = 1;

    void Start()
    {
        // 같은 오브젝트에 있는 HUD 컨트롤러를 자동으로 찾습니다
        hudController = GetComponent<PlayerHUDController>();

        // 첫 번째 웨이브 정보를 UI에 즉시 표시합니다
        if (hudController != null)
        {
            hudController.UpdateWaveUI(currentWave);
        }
    }

    void Update()
    {
        // 숫자 1 키를 누르면 웨이브가 1씩 증가
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentWave++;

            if (hudController != null)
            {
                hudController.UpdateWaveUI(currentWave);
                Debug.Log($"[테스트] 웨이브 상승: {currentWave}");
            }
        }
    }
}