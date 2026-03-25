using System.Linq;
using UnityEngine;


// ==========================================
// [추가] 인스펙터에서 선택할 타겟 종류 (Enum)
// ==========================================
public enum TargetType
{
    MagicStone, // 마법석
    Player,     // 플레이어
    EnemyAlly        // 아군 (다른 적)
}

public interface ITargetFinder
{
    Transform GetTarget(BaseAI self);
}

// 1. 마법석 타겟
public class MagicStoneTargetFinder : ITargetFinder
{
    public Transform GetTarget(BaseAI self)
    {
        if (MagicStoneManager.Instance != null)
            return MagicStoneManager.Instance.StoneTransform;
        return null;
    }
}

// 2. 플레이어 타겟
public class PlayerTargetFinder : ITargetFinder
{
    private Transform cachedPlayer;
    public Transform GetTarget(BaseAI self)
    {
        if (cachedPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) cachedPlayer = player.transform;
        }
        return cachedPlayer;
    }
}

// 3. 아군(다른 적) 타겟 - 복잡한 로직 제외하고 살아있는 다른 아군 아무나 1명 선택 (임시)
public class AllyTargetFinder : ITargetFinder
{
    public Transform GetTarget(BaseAI self)
    {
        // 맵에 있는 모든 적 중에서 나(self)를 제외한 살아있는 적 첫 번째를 타겟으로 삼음
        BaseAI[] allEnemies = GameObject.FindObjectsByType<BaseAI>(FindObjectsSortMode.None);

        BaseAI targetAlly = allEnemies.FirstOrDefault(e => e != self && !e.isDead);

        return targetAlly != null ? targetAlly.transform : null;
    }
}