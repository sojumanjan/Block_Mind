using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Room room = (Room)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("좌표 위치로 이동"))
        {
            Undo.RecordObject(room.transform, "Move Room To Coordinate");
            room.MoveToCoordinatePosition();
        }
    }
}
