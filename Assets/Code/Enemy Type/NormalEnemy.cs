using UnityEngine;
using static ITargetFinder;

// 실제 게임 오브젝트(프리팹)에 부착될 근접 적 클래스입니다.
public class NormalEnemy : BaseAI
{
    protected override void Start()
    {
        // 1. 부모 클래스(BaseAI)의 Start()를 호출하여 체력과 UI를 초기화합니다.
        base.Start();

        // 2. 이 적이 사용할 '조건'과 '전략'을 주입(Inject)합니다.
        // 여기서는 근접 공격용 조건과 전략을 할당합니다.
        //this.targetFinder = new MagicStoneTargetFinder(); // 마법석 쫓아가!
        this.actionCondition = new MeleeAttackCondition();
        this.actionStrategy = new MeleeAttackStrategy();
    }
}