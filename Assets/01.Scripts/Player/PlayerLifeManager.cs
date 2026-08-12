using System;
using System.Collections;
using UnityEngine;

public class PlayerLifeManager : MonoBehaviour
{
    public static PlayerLifeManager Instance;

    public Vector2 respawnPoint;

    public event Action OnDie;
    public event Action OnCheckPoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        CrushDetector.Instance.OnCrushed += Crushed;
        respawnPoint = new Vector2(-11, -7);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Obstacle"))
        {
            OnDie?.Invoke();
            Die();
        }
    }

    public void Die()
    {
        Debug.Log("플레이어 장애물 닿아 사망이요");
        transform.position = respawnPoint;
    }

    void Crushed()
    {
        Debug.Log("플레이어 끼임 사망이요");
        transform.position = respawnPoint;
    }

    public void SetSpawnPosition(Vector3 spawnPoint)
    {
        respawnPoint = spawnPoint;
    }
}
