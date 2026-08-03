using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    private float curHealth = 3;
    public float maxHealth = 3;

    public float invincibleTime = 2f;

    private bool isInvincible = false;

    public event Action OnDie;

    private void Awake()
    {
        Instance = this;
        curHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isInvincible) return;

        curHealth -= damage;

        if (curHealth <= 0)
        {
            Die();
            OnDie?.Invoke();
        }

        StartCoroutine(CountDownInvincible());
    }

    IEnumerator CountDownInvincible()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }

    private void Die()
    {

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Obstacle"))
        {
            TakeDamage(99999999);
        }
    }
}
