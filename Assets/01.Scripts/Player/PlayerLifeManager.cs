using System;
using System.Collections;
using UnityEngine;

public class PlayerLifeManager : MonoBehaviour
{
    public static PlayerLifeManager Instance;

    public Vector2 respawnPoint;

    public event Action OnDie;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        CrushDetector.Instance.OnCrushed += Crushed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Obstacle"))
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log("플레이어 장애물 닿아 사망이요");
        RespawnAtCheckpoint();
    }

    // 사망 처리에서 "되돌리는 부분"만 떼어낸 것.
    // ESC 메뉴의 "체크포인트에서 재시작"도 같은 경로를 써야 그림자 위치까지 맞는다.
    public void RespawnAtCheckpoint()
    {
        OnDie?.Invoke();
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
