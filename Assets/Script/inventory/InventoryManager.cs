using UnityEngine;
using UnityEngine.InputSystem; 

public class InventoryManager : MonoBehaviour
{
    // UI 표시 기능을 테스트 하기위한 스크립트

    private PlayerHUDController hudController;
    public int gold = 0; // 초기 골드 설정

    private void Start()
    {
        hudController = GetComponent<PlayerHUDController>();

        // 시작하자마자 현재 골드 수치를 UI에 표시
        if (hudController != null)
        {
            hudController.UpdateGoldUI(gold);
        }
    }

    private void Update()
    {
        // 마우스 좌클릭 시 골드 +1
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            AddGold(1);
        }

        // 마우스 우클릭 시 골드 -1
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            AddGold(-1);
        }
    }

    public void AddGold(int quantity)
    {
        gold += quantity;

        // 값이 변할 때마다 HUD 컨트롤러에 갱신을 요청합니다
        if (hudController != null)
        {
            hudController.UpdateGoldUI(gold);
        }
    }
}