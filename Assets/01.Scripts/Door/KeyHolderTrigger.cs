using UnityEngine;

public class KeyHolderTrigger : MonoBehaviour
{
    [SerializeField] private KeyDoor door;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool consumeKey = true;  // 열고 나면 열쇠 소모 여부
    [SerializeField] private bool stayOpen = true;    // 한 번 열리면 계속 열린 채로

    [SerializeField] private GameObject keyHole;

    private bool isUnlocked = false;

    // Awake 순서가 보장되지 않으므로 모든 Awake가 끝난 Start에서 구독한다
    private void Start()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate += ResetLock;
    }

    private void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate -= ResetLock;
    }

    // 문을 닫는 것은 KeyDoor가 스스로 한다.
    // 여기서는 이 트리거가 들고 있는 상태(잠금 여부, 열쇠구멍 표시)만 되돌린다.
    // 이걸 안 하면 문은 닫혔는데 isUnlocked가 true로 남아 다시 열 수 없게 된다.
    private void ResetLock()
    {
        isUnlocked = false;

        if (keyHole != null)
            keyHole.SetActive(true);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        if (isUnlocked) return;

        PlayerKeyHolder holder = other.gameObject.GetComponent<PlayerKeyHolder>();
        if (holder == null || !holder.HasKey) return;

        if (consumeKey)
            holder.UseKey();

        isUnlocked = true;
        door.SetOpen(true);
        keyHole.SetActive(false);
    }
}
