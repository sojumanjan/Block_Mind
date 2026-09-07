using UnityEngine;

// 열쇠로 여는 문. 여닫는 연출은 ScalingDoor가 담당한다.
// 이 클래스는 체크포인트에서 닫힌 상태로 되돌리는 부분만 갖는다.
public class KeyDoor : ScalingDoor
{
    // Awake 순서가 보장되지 않으므로 모든 Awake가 끝난 Start에서 구독한다
    private void Start()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate += CloseInstantly;
    }

    private void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate -= CloseInstantly;
    }

    // 체크포인트가 바뀌면 애니메이션 없이 닫힌 상태로 되돌린다
    private void CloseInstantly()
    {
        SetOpen(false, true);
    }
}
