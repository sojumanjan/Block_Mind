using Unity.Cinemachine;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private Vector2 coordinate;

    public Vector2 Coordinate => coordinate;

    public bool IsVisited { get; private set; }

    public Door[] Doors { get; private set; }
    public ButtonZone[] ButtonZones { get; private set; }
    public Checkpoint Checkpoint { get; private set; }
    public CinemachineCamera Vcam { get; private set; }

    private void Awake()
    {
        Doors = GetComponentsInChildren<Door>(true);
        ButtonZones = GetComponentsInChildren<ButtonZone>(true);
        Checkpoint = GetComponentInChildren<Checkpoint>(true);
        Vcam = GetComponentInChildren<CinemachineCamera>(true);
    }

    public void MarkVisited()
    {
        IsVisited = true;
    }

    public void MarkCleared()
    {
        IsCleared = true;
    }
}
