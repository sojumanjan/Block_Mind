using UnityEngine;

public class ButtonZone : MonoBehaviour
{
    [SerializeField] private Door[] connectedDoors; // 연결된 문들 (인스펙터에서 드래그)

    private int blockCount = 0; // 현재 이 Zone에 들어와 있는 블럭 개수

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

        foreach (Door door in connectedDoors)
        {
            if (door != null)
                door.SetOpen(isActivated);
        }
    }
}