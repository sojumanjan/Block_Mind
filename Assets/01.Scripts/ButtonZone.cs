using UnityEngine;
using UnityEngine.Serialization;

public class ButtonZone : MonoBehaviour
{
    // Door, MovingDoor 등 ActivatableDevice를 상속한 것이면 무엇이든 여기에 넣는다.
    // 예전 connectedDoors 필드에 넣어둔 연결은 FormerlySerializedAs로 그대로 이어받는다.
    [FormerlySerializedAs("connectedDoors")]
    [SerializeField] private ActivatableDevice[] connectedDevices;

    private int blockCount = 0; // 현재 이 Zone에 들어와 있는 블럭 개수

    [SerializeField] Color inactiveColor;
    [SerializeField] Color activeColor;

    SpriteRenderer sr;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Block")) return;

        blockCount++;
        UpdateDoors();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Block")) return;

        blockCount--;
        UpdateDoors();
    }

    private void UpdateDoors()
    {
        bool isActivated = blockCount > 0;

        sr.color = isActivated ? activeColor : inactiveColor;

        foreach (ActivatableDevice device in connectedDevices)
        {
            if (device != null)
                device.SetActivated(isActivated);
        }
    }
}