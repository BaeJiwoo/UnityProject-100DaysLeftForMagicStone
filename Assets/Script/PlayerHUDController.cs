using UnityEngine;
using TMPro;

public class PlayerHUDController : MonoBehaviour
{
    [Header("Wave UI")]
    public TextMeshProUGUI waveText;

    [Header("Gold UI")]
    public TextMeshProUGUI goldText;

    /// 외부에서 웨이브 번호를 전달받아 UI를 갱신합니다.
    public void UpdateWaveUI(int currentWave)
    {
        if (waveText != null)
            waveText.text = $"WAVE {currentWave}";
    }

    /// 외부에서 현재 골드량을 전달받아 UI를 갱신합니다.
    public void UpdateGoldUI(int currentGold)
    {
        if (goldText != null)
            goldText.text = currentGold.ToString();
    }
}