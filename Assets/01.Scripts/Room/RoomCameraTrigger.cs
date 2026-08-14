using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomCameraTrigger : MonoBehaviour
{
    [SerializeField] private CinemachineCamera roomCamera;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 10;
    [SerializeField] private Transform shadowSpawnPoint;

    private BoxCollider2D box;
    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            roomCamera.Priority = activePriority; // 이 방 카메라를 최우선으로

            if (FollowingShadow.Instance != null)
                FollowingShadow.Instance.MoveShadowToNextRoom(shadowSpawnPoint);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            roomCamera.Priority = inactivePriority; // 나가면 우선순위 원위치
        }
    }

    private void OnDrawGizmos() // 선택 안 해도 항상 보임
    {
        BoxCollider2D col = box != null ? box : GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0f); // 반투명 초록
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.offset, col.size);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(col.offset, col.size);
    }
}