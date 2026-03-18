using System.Buffers.Text;
using UnityEngine;

public interface IActionCondition 
{
    /// <summary>
    /// AI가 행동을 실행할 조건이 충족되었는지 확인합니다.
    /// </summary>
    /// <param name="self">판단을 수행하는 주체 (적 AI 본체)</param>
    /// <param name="target">목표물 (Magic Stone)</param>
    /// <returns>조건 충족 여부 (true면 행동 실행)</returns>
    bool CanExecute(BaseAI self, Transform target);
}
