using UnityEditor;
using UnityEditor.SceneManagement;
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
            // 위치는 Transform, 이름은 GameObject에 들어 있어 서로 다른 오브젝트다.
            // Undo도 각각 기록해야 한 번의 Ctrl+Z로 둘 다 되돌아간다.
            Undo.RecordObject(room.transform, "Move Room To Coordinate");
            room.MoveToCoordinatePosition();
            PrefabUtility.RecordPrefabInstancePropertyModifications(room.transform);

            // 프리팹 에셋(또는 프리팹 모드)의 루트까지 Room(18, -4) 따위로
            // 바꿔버리면 곤란하므로 씬에 놓인 인스턴스만 리네임한다.
            if (IsSceneInstance(room.gameObject))
            {
                Undo.RecordObject(room.gameObject, "Rename Room To Coordinate");
                room.gameObject.name = RoomNameOf(room.Coordinate);
                PrefabUtility.RecordPrefabInstancePropertyModifications(room.gameObject);
            }
        }
    }

    private static string RoomNameOf(Vector2 coordinate)
    {
        return string.Format("Room({0}, {1})", Mathf.RoundToInt(coordinate.x), Mathf.RoundToInt(coordinate.y));
    }

    private static bool IsSceneInstance(GameObject go)
    {
        return !PrefabUtility.IsPartOfPrefabAsset(go) && PrefabStageUtility.GetPrefabStage(go) == null;
    }
}
