using UnityEngine;

public class KeyHolderTrigger : MonoBehaviour
{
    [SerializeField] private KeyDoor door;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool consumeKey = true;  // 열고 나면 열쇠 소모 여부
    [SerializeField] private bool stayOpen = true;    // 한 번 열리면 계속 열린 채로

    [SerializeField] private GameObject keyHole;

    private bool isUnlocked = false;

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
