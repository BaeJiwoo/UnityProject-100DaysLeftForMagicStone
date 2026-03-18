using System.Buffers.Text;
using UnityEngine;

public interface IActionStrategy
{
    /// <summary>
    /// AI의 실제 행동(공격, 자폭 등)을 실행합니다.
    /// </summary>
    /// <param name="self">행동을 수행하는 주체 (적 AI 본체)</param>
    /// <param name="target">목표물 (Magic Stone)</param>
    void Execute(BaseAI self, Transform target);
}