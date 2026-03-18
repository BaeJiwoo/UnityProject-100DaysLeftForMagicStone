using UnityEngine;
using System.Collections;
// ==========================================
// [1] 자폭 조건 (Condition)
// ==========================================
public class SuicideCondition : IActionCondition
{
    public bool CanExecute(BaseAI self, Transform target)
    {
        float distance = 0f;
        Collider2D targetCol = MagicStoneManager.Instance.StoneCollider;

        if (targetCol != null)
        {
            Vector2 edgePoint = targetCol.ClosestPoint(self.transform.position);
            distance = Vector2.Distance(self.transform.position, edgePoint);
        }
        else
        {
            distance = Vector2.Distance(self.transform.position, target.position);
        }

        return distance <= self.attackRange;
    }
}

// ==========================================
// [2] 자폭 실행 전략 (Strategy)
// ==========================================
public class SuicideStrategy : IActionStrategy
{
    public void Execute(BaseAI self, Transform target)
    {
        // 실행 주체(self)가 SuicideEnemy인지 확인하고 폭발 타이머를 작동시킵니다.
        if (self is SuicideEnemy suicideEnemy)
        {
            suicideEnemy.StartDetonation(target);
        }
    }
}

// ==========================================
// [3] 실제 적 클래스 (SuicideEnemy)
// ==========================================
public class SuicideEnemy : BaseAI
{
    [Header("자폭 특수 설정")]
    public GameObject explosionFXPrefab;

    [Tooltip("타겟에 닿은 후 폭발하기까지 걸리는 시간")]
    public float detonationTime = 1.5f;

    [Tooltip("초기 깜빡임 속도 (점점 빨라짐)")]
    public float initialFlashSpeed = 0.2f;

    // 현재 폭발 준비 중인지 확인하는 변수
    private bool isDetonating = false;

    protected override void Start()
    {
        base.Start();
        // 조건과 전략 주입
        this.actionCondition = new SuicideCondition();
        this.actionStrategy = new SuicideStrategy();
    }

    protected override void Update()
    {
        // [핵심] 폭발 준비(깜빡임) 중이라면 추적과 상태 업데이트(FSM)를 멈춥니다!
        if (isDetonating) return;

        base.Update();
    }

    // Strategy에서 호출하여 코루틴을 시작하는 함수
    public void StartDetonation(Transform target)
    {
        // 중복 실행 방지 및 죽은 상태 예외 처리
        if (isDetonating || isDead) return;

        StartCoroutine(DetonationRoutine(target));
    }

    private IEnumerator DetonationRoutine(Transform target)
    {
        isDetonating = true;
        float elapsed = 0f;
        bool isRed = false;

        // detonationTime 동안 반복해서 깜빡입니다.
        while (elapsed < detonationTime)
        {
            // 깜빡이는 도중에 총알을 맞고 죽었다면 폭발을 강제 취소합니다!
            if (isDead) yield break;

            // 빨간색과 원래 색상(흰색)을 번갈아가며 적용
            sr.color = isRed ? Color.white : Color.red;
            isRed = !isRed;

            // [디테일 연출] 시간이 지날수록 깜빡이는 속도가 점점 빨라집니다.
            float currentFlashSpeed = Mathf.Lerp(initialFlashSpeed, 0.03f, elapsed / detonationTime);

            yield return new WaitForSeconds(currentFlashSpeed);
            elapsed += currentFlashSpeed;
        }

        // 시간이 다 되었을 때 아직 살아있다면 폭발!
        if (!isDead)
        {
            // 1. 폭발 FX 생성
            if (explosionFXPrefab != null)
            {
                Instantiate(explosionFXPrefab, transform.position, Quaternion.identity);
            }

            // 2. 타겟에 데미지 전달
            if (target != null)
            {
                target.SendMessage("TakeDamage", attackPower, SendMessageOptions.DontRequireReceiver);
            }

            // 원래 색으로 돌려놓기 (사망 연출을 위해)
            sr.color = Color.white;

            // 3. 자폭 즉사 처리 (9999 데미지)
            TakeDamage(maxHealth + 9999f);
        }
    }
}