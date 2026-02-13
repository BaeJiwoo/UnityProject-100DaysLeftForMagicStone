using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireCooldown = 0.15f;
    public float shootForce = 1f;

    float lastFireTime;
    PlayerAim aim;

    void Awake()
    {
        aim = GetComponent<PlayerAim>();
    }

    public void TryFire()
    {
        if (Time.time < lastFireTime + fireCooldown)
            return;

        lastFireTime = Time.time;
        Fire();
    }

    void Fire()
    {
        GameObject bullet =
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = bullet.transform.right * shootForce;
    }
}
