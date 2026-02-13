using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 5;
    int currentHP;

    public event Action OnDeath;
    public event Action<int> OnDamaged;

    void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        OnDamaged?.Invoke(damage);

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        OnDeath?.Invoke();
        gameObject.SetActive(false); // or ¸®½ºÆù
    }
}
