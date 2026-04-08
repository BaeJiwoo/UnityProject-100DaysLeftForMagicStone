using UnityEngine;

// 유니티 프로젝트 창 우클릭 메뉴에 생성 버튼을 만들어줍니다.
[CreateAssetMenu(fileName = "New Mercenary", menuName = "Game Data/Mercenary Info")]
public class MercenaryInfo : ScriptableObject
{
    [Header("기본 정보")]
    public string mercID;           // 고유 ID (예: "Archer", "Wizard")
    public string mercName;         // UI 표시 이름
    public GameObject prefab;       // 전투에 소환될 프리팹
    public Sprite portraitIcon;     // 상점 및 UI에 사용할 아이콘

    [Header("순찰 및 감지 설정 (1레벨 기준)")]
    public BaseAllyAI.PatrolTarget patrolTargetType = BaseAllyAI.PatrolTarget.MagicStone;
    public float basePatrolRadius = 10f; // 기본 순찰 범위
    public float baseDetectRadius = 8f;  // 기본 공격 범위
    public float moveSpeed = 4f;

    [Header("순찰 대기 설정")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("공격 및 밸런스 설정 (1레벨 기준)")]
    public float baseAttackPower = 15f; // 기본 공격력
    public float baseFireRate = 0.5f;   // 기본 연사 속도 (공격 간격 딜레이)

    // ==========================================
    // [핵심 추가] 레벨업에 따른 성장 수치 설정
    // ==========================================
    [Header("레벨업 성장 수치 (1업당)")]
    public float attackGrowth = 2f;         // 증가하는 공격력
    public float patrolRadiusGrowth = 0.5f; // 넓어지는 순찰 범위
    public float detectRadiusGrowth = 0.5f; // 넓어지는 공격 감지 범위
    public float fireRateReduction = 0.02f; // 줄어드는 공격 딜레이 (수치가 작을수록 빨리 쏨)
    public float minFireRate = 0.1f;        // 연사 속도 한계치 (너무 빨라져서 버그가 생기는 것 방지)

    [Header("상점 비용")]
    public int unlockCost = 1000;      // 최초 고용 비용
    public int upgradeCostBase = 250;  // 1레벨 -> 2레벨 기본 업그레이드 비용
    public float costMultiplier = 1.5f; // [추가] 레벨업 시 비용 배율 (1.5배씩 비싸짐)
                                        // 또는 일정 금액씩 더하고 싶다면: public int costIncrement = 500;
}