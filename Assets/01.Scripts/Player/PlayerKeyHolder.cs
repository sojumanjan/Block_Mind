using UnityEngine;

public class PlayerKeyHolder : SingletonBehaviour<PlayerKeyHolder>
{
    private Key heldKey;

    [Header("사운드")]
    [SerializeField] SoundData getKeySound;

    public bool HasKey => heldKey != null;

    // Awake 순서가 보장되지 않으므로 모든 Awake가 끝난 Start에서 구독한다
    private void Start()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate += ResetKey;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate -= ResetKey;
    }

    public void AddKey(Key key)
    {
        heldKey = key;
        AudioManager.Instance.Play(getKeySound, key.transform.position);
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