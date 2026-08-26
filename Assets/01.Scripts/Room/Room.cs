using Unity.Cinemachine;
using UnityEngine;

public class Room : MonoBehaviour
{
    public const float Width = 32f;
    public const float Height = 18f;

    [SerializeField] private Vector2 coordinate;

    [Tooltip("이 방이 속한 스테이지 에셋. 미니맵 지형 색 등 스테이지 공통 속성을 여기서 가져온다")]
    [SerializeField] private StageData stage;

    public Vector2 Coordinate => coordinate;

    public StageData Stage => stage;

    public bool IsVisited { get; private set; }

    public Door[] Doors { get; private set; }
    public ButtonZone[] ButtonZones { get; private set; }
    public Portal[] Portals { get; private set; }
    public Checkpoint Checkpoint { get; private set; }
    public CinemachineCamera Vcam { get; private set; }
    public Transform shadowSpawnPoint { get; private set; }

    private void Awake()
    {
        Doors = GetComponentsInChildren<Door>(true);
        ButtonZones = GetComponentsInChildren<ButtonZone>(true);
        Portals = GetComponentsInChildren<Portal>(true);
        Checkpoint = GetComponentInChildren<Checkpoint>(true);
        Vcam = GetComponentInChildren<CinemachineCamera>(true);
        shadowSpawnPoint = GetComponentInChildren<ShadowSpawnPoint>().transform;
    }

    public void MarkVisited()
    {
        IsVisited = true;
    }

    public void MoveToCoordinatePosition()
    {
        Vector3 pos = transform.position;
        pos.x = (coordinate.x - 1f) * Width;
        pos.y = coordinate.y * Height;
        transform.position = pos;
    }
}
