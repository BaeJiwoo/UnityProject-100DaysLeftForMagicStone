using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    Rigidbody2D rb;
    Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input.normalized * moveSpeed;
        moveInput.y = rb.linearVelocityY;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput ;
    }
}
