using Unity.Cinemachine;
using UnityEngine;

public class Room : MonoBehaviour
{
    private const float RoomWidth = 32f;
    private const float RoomHeight = 18f;

    [SerializeField] private Vector2 coordinate;

    public Vector2 Coordinate => coordinate;

    public bool IsVisited { get; private set; }

    public Door[] Doors { get; private set; }
    public ButtonZone[] ButtonZones { get; private set; }
    public Checkpoint Checkpoint { get; private set; }
    public CinemachineCamera Vcam { get; private set; }
    public Transform shadowSpawnPoint { get; private set; }

    private void Awake()
    {
        Doors = GetComponentsInChildren<Door>(true);
        ButtonZones = GetComponentsInChildren<ButtonZone>(true);
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
        pos.x = (coordinate.x - 1f) * RoomWidth;
        pos.y = coordinate.y * RoomHeight;
        transform.position = pos;
    }
}
