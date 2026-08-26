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
    private Room room;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        room = GetComponentInParent<Room>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            roomCamera.Priority = activePriority; // 이 방 카메라를 최우선으로

            if (FollowingShadow.Instance != null)
                FollowingShadow.Instance.MoveShadowToNextRoom(room.shadowSpawnPoint);

            // 지도에 현재 방을 알림 (방문 처리도 MapUI에서 함께)
            if (MapUI.Instance != null)
                MapUI.Instance.SetCurrentRoom(room);

            // 스테이지 BGM 전환. 같은 곡이면 AudioManager가 무시하므로
            // 같은 스테이지 안에서 방을 넘나들어도 곡이 끊기지 않는다.
            if (room != null && room.Stage != null)
                AudioManager.PlayBgm(room.Stage.Bgm);

            // 방이 바뀌었으니 재생 중인 루프 사운드들이 볼륨을 다시 계산해야 한다
            AudioManager.NotifyPlayerRoomChanged();
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