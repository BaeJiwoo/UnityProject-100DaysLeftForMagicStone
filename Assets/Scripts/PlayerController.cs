using UnityEngine;

public class PlayerController : MonoBehaviour
{
    PlayerMovement movement;
    PlayerAim aim;
    PlayerAttack attack;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        aim = GetComponent<PlayerAim>();
        attack = GetComponent<PlayerAttack>();
    }

    void Update()
    {
        // 이동 입력
        Vector2 moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            0f
        );
        movement.SetMoveInput(moveInput);

        // 조준
        aim.UpdateAim();

        // 공격
        if (Input.GetMouseButton(0))
            attack.TryFire();
    }
}
