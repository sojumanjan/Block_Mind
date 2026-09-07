using System;
using System.Collections;
using UnityEngine;

public class PlayerLifeManager : SingletonBehaviour<PlayerLifeManager>
{

    public Vector2 respawnPoint;

    public event Action OnDie;


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
        transform.position = respawnPoint;
    }

    public void SetSpawnPosition(Vector3 spawnPoint)
    {
        respawnPoint = spawnPoint;
    }
}
