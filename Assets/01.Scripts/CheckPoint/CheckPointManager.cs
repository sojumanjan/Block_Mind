// 매니저 - 전체 체크포인트 중 딱 하나만 활성 상태를 유지
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Tooltip("게임 시작 시 활성 상태로 둘 체크포인트. 비워두면 첫 체크포인트를 밟기 전까지 활성 체크포인트가 없다.")]
    [SerializeField] private Checkpoint defaultCheckpoint;

    private Checkpoint currentActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (defaultCheckpoint == null)
        {
            Debug.LogWarning("defaultCheckpoint가 지정되지 않았습니다. 첫 체크포인트를 밟기 전에 사망하면 스폰 위치가 부정확합니다.", this);
            return;
        }

        currentActive = defaultCheckpoint;
        defaultCheckpoint.SetVisualState(true);

        // 시작 리스폰 지점도 이 체크포인트로 맞춘다.
        // 이걸 안 하면 그림자는 기본 체크포인트 방으로, 플레이어는 다른 좌표로 가서 서로 어긋난다.
        if (PlayerLifeManager.Instance != null)
            PlayerLifeManager.Instance.SetSpawnPosition(defaultCheckpoint.transform.position);
    }

    // 체크포인트에서 호출
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

    public Checkpoint GetCheckPoint()
    {
        return currentActive;
    }
}