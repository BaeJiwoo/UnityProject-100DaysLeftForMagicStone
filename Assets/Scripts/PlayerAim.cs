using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    public Transform aimPivot; // √—, ∏ˆ≈Î, »∏¿¸ ±‚¡ÿ

    public Vector2 AimDirection { get; private set; }

    public void UpdateAim()
    {
        Vector3 mouseWorld =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2 dir =
            (mouseWorld - aimPivot.position);

        dir.Normalize();
        AimDirection = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        aimPivot.rotation = Quaternion.Euler(0, 0, angle);
    }
}
