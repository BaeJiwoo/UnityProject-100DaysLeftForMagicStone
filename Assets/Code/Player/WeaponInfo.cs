using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Game Data/Weapon Info")]
public class WeaponInfo : ScriptableObject
{
    [Header("기본 정보")]
    public string weaponID;
    public string weaponName;
    public Sprite weaponIcon;
    public GameObject projectilePrefab;

    [Header("기본 스탯 (1레벨 기준)")]
    public float baseDamage = 10f;
    public float baseSpeed = 15f;
    public float baseFireDelay = 0.2f;
    public float baseManaCost = 10f;
    public int basePierceCount = 1;
    public float baseLifeTime = 3f;       // [추가] 투사체 생존 시간

    [Header("탄퍼짐 및 조준 기본 스탯 (1레벨 기준)")]
    public float baseSpreadAngleHip = 15f; // [추가] 지향 사격 탄퍼짐
    public float baseSpreadAngleAim = 2f;  // [추가] 정조준 탄퍼짐
    [Range(0f, 1f)]
    public float baseAimSlowdownRatio = 0.5f; // [추가] 조준 시 이속 감소율
    public float rotationOffset = 0f;      // [추가] 스프라이트 회전 보정값

    // ==========================================
    // [핵심 추가] 업그레이드 성장 수치 (1레벨당)
    // ==========================================
    [Header("성장 수치 (증가하는 스탯)")]
    public float damageGrowth = 2f;        // 데미지 증가
    public float speedGrowth = 1f;         // 탄속 증가

    [Header("성장 수치 (감소하는 스탯 - 낮을수록 좋음)")]
    public float fireDelayReduction = 0.01f; // 연사 딜레이 감소 (더 빨리 쏨)
    public float minFireDelay = 0.05f;       // 연사 한계치 (너무 빨라짐 방지)

    public float manaCostReduction = 0.5f;   // 소모 마나 감소
    public float minManaCost = 1f;           // 최소 마나 소모 한계치

    public float spreadReduction = 0.5f;     // 탄퍼짐 각도 감소 (명중률 상승)
    public float minSpreadAngle = 0f;        // 최소 탄퍼짐 한계 (0이면 레이저처럼 일직선)

    [Header("상점 비용")]
    public int unlockCost = 500;
    public int upgradeCostBase = 200;
    public float costMultiplier = 1.5f;
}