// 매니저 - 전체 체크포인트 중 딱 하나만 활성 상태를 유지
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Checkpoint currentActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ActivateCheckpoint(Checkpoint checkpoint, Vector3 pos, Collider2D player)
    {
        if (currentActive == checkpoint) return; // 이미 이게 활성 상태면 무시

        // 이전 체크포인트 비활성화
        if (currentActive != null)
            currentActive.SetVisualState(false);

        // 새 체크포인트 활성화
        currentActive = checkpoint;
        checkpoint.SetVisualState(true);

        // 플레이어 스폰 위치 갱신
        PlayerLifeManager lifeManager = player.GetComponent<PlayerLifeManager>();
        if (lifeManager != null)
            lifeManager.SetSpawnPosition(pos);
    }
}