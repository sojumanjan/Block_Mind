using UnityEngine;

public class PlayerKeyHolder : MonoBehaviour
{
    public static PlayerKeyHolder Instance;
    private Key heldKey;

    public bool HasKey => heldKey != null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Awake 순서가 보장되지 않으므로 모든 Awake가 끝난 Start에서 구독한다
    private void Start()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate += ResetKey;
    }

    private void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate -= ResetKey;
    }

    public void AddKey(Key key)
    {
        heldKey = key;
    }

    public void UseKey()
    {
        if (heldKey == null) return;

        heldKey.Consume();
        heldKey = null;
    }

    public void ResetKey()
    {
        if (heldKey != null) {
            heldKey.ResetLocation();
            heldKey = null;
        }
    }
}