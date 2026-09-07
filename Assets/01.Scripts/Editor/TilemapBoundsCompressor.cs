using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 타일맵 bounds를 압축한다. Tools/Block Mind 메뉴에서 직접 실행한다.
//
// bounds는 타일을 칠하면 커지지만 지워도 줄어들지 않는다(Unity가 매번 전수 조사를 피하려고 그렇게 설계했다).
// MapUI가 미니맵을 구울 때 cellBounds를 한 칸씩 훑기 때문에, 부푼 bounds가 그대로 로딩 시간이 된다.
// 실제로 3,315만 칸을 순회해 타일 1.9만 개를 찾고 있었다.
//
// 씬 저장마다 자동으로 돌렸더니 매번 진행 표시줄이 떠서 작업을 방해했다. 그래서 수동 실행으로 바꿨다.
// bounds는 타일을 지울 때마다 다시 쌓이므로, 맵을 손본 뒤나 빌드 전에 한 번 돌려주면 된다.
//
// 반드시 씬 인스턴스만 압축한다. 프리팹 에셋을 압축하면 타일이 유실된다 -
// 인스턴스가 bounds는 프리팹에서 상속받고 타일은 자기 오버라이드로 갖는 반쪽 상태라서,
// 프리팹 bounds가 0이 되면 인스턴스의 타일이 bounds 밖으로 밀려나 접근 불가가 된다.
public static class TilemapBoundsCompressor
{
    // 이 칸수 이하는 압축해도 얻는 게 없어 건너뛴다
    private const long SkipThreshold = 1000;

    [MenuItem("Tools/Block Mind/타일맵 bounds 압축")]
    private static void CompressActiveSceneFromMenu()
    {
        Compress(SceneManager.GetActiveScene());
    }

    private static void Compress(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        long before = 0, after = 0;
        int compressed = 0, restored = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                BoundsInt bounds = tilemap.cellBounds;
                long cells = (long)bounds.size.x * bounds.size.y * bounds.size.z;
                before += cells;

                if (cells <= SkipThreshold) { after += cells; continue; }

                // 압축이 타일을 건드리지 않았는지 확인할 수 있도록 먼저 기록해 둔다
                var positions = new List<Vector3Int>();
                var tiles = new List<TileBase>();
                foreach (Vector3Int position in bounds.allPositionsWithin)
                {
                    if (!tilemap.HasTile(position)) continue;
                    positions.Add(position);
                    tiles.Add(tilemap.GetTile(position));
                }

                tilemap.CompressBounds();

                if (IsIntact(tilemap, positions, tiles))
                {
                    compressed++;
                }
                else
                {
                    // 한 칸이라도 달라지면 되돌린다. 로딩 속도보다 맵 데이터가 우선이다.
                    for (int i = 0; i < positions.Count; i++)
                        tilemap.SetTile(positions[i], tiles[i]);

                    restored++;
                    Debug.LogWarning("[타일맵 압축] " + GetPath(tilemap.transform) + " 압축 후 타일이 달라져 되돌렸습니다.", tilemap);
                }

                BoundsInt now = tilemap.cellBounds;
                after += (long)now.size.x * now.size.y * now.size.z;
            }
        }

        Debug.Log("[타일맵 압축] 순회 셀 " + before.ToString("N0") + " -> " + after.ToString("N0")
            + " (압축 " + compressed + "개" + (restored > 0 ? ", 되돌림 " + restored + "개" : "") + ")");
    }

    private static bool IsIntact(Tilemap tilemap, List<Vector3Int> positions, List<TileBase> tiles)
    {
        for (int i = 0; i < positions.Count; i++)
            if (tilemap.GetTile(positions[i]) != tiles[i]) return false;

        int count = 0;
        foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(position)) count++;

        return count == positions.Count;
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;

        return path;
    }
}
