using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 타일맵 bounds를 씬 저장 시 자동으로 압축한다. Tools/Block Mind 메뉴로 수동 실행도 된다.
//
// bounds는 타일을 칠하면 커지지만 지워도 줄어들지 않는다(Unity가 매번 전수 조사를 피하려고 그렇게 설계했다).
// MapUI가 미니맵을 구울 때 cellBounds를 한 칸씩 훑기 때문에, 부푼 bounds가 그대로 로딩 시간이 된다.
//
// 방을 새로 만들 때마다 다시 쌓인다. Grid_Wire.prefab이 타일 0개인데 bounds가 2620x544라서
// 새 인스턴스가 그 값을 상속받는다. 방 하나 추가에 140만 칸이 따라온다. 그래서 자동화가 필요하다.
// (프리팹 쪽 bounds를 줄이면 인스턴스의 타일이 유실된다. 이유는 아래 주석 참고)
//
// 저장이 느려지지 않는 이유:
//   한 번 압축한 타일맵의 bounds를 기억해두고, 그 값에서 바뀌지 않았으면 아예 건드리지 않는다.
//   압축이 끝난 bounds는 이미 실제 타일의 최소 범위라서 다시 압축해도 변하지 않는다.
//   "칸 수 대 타일 수 비율"로 판정했더니 전선 레이어처럼 원래 넓게 퍼진 것이 매번 후보로 잡혀
//   비싼 전수 기록을 반복했다(그게 예전에 진행 표시줄이 뜨던 이유다).
//   기억한 값과 비교하는 방식은 계산이 사실상 없고 판정도 정확하다.
//
// 반드시 씬 인스턴스만 압축한다. 프리팹 에셋을 압축하면 타일이 유실된다 -
// 인스턴스가 bounds는 프리팹에서 상속받고 타일은 자기 오버라이드로 갖는 반쪽 상태라서,
// 프리팹 bounds가 0이 되면 인스턴스의 타일이 bounds 밖으로 밀려나 접근 불가가 된다.
[InitializeOnLoad]
public static class TilemapBoundsCompressor
{
    // 이 칸수 이하는 압축해도 얻는 게 없어 아예 보지 않는다.
    // 방 하나가 32x18이므로 cellSize 1.0이면 576칸, 0.1인 전선 레이어면 57,600칸이 정상이다.
    private const long MinCellsToConsider = 10000;

    // 압축을 마친 뒤의 bounds. 이 값 그대로면 더 줄일 게 없다는 뜻이다.
    // 도메인 리로드로 비워지면 저장 한 번에 전체를 다시 확인하고 다시 채운다.
    private static readonly Dictionary<Tilemap, BoundsInt> settledBounds = new Dictionary<Tilemap, BoundsInt>();

    static TilemapBoundsCompressor()
    {
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    private static void OnSceneSaving(Scene scene, string path)
    {
        Compress(scene, false);
    }

    [MenuItem("Tools/Block Mind/타일맵 bounds 압축")]
    private static void CompressActiveSceneFromMenu()
    {
        Compress(SceneManager.GetActiveScene(), true);
    }

    private static void Compress(Scene scene, bool alwaysLog)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        List<Tilemap> candidates = FindBloated(scene);

        if (candidates.Count == 0)
        {
            if (alwaysLog) Debug.Log("[타일맵 압축] 부푼 타일맵이 없습니다.");
            return;
        }

        long before = 0, after = 0;
        int compressed = 0, restored = 0;

        foreach (Tilemap tilemap in candidates)
        {
            BoundsInt bounds = tilemap.cellBounds;
            before += CellCount(bounds);

            // 압축이 타일을 건드리지 않았는지 확인하고 필요하면 되돌릴 수 있도록 먼저 기록해 둔다.
            // 후보만 여기 오므로 이 비용은 방을 새로 만든 직후에만 발생한다.
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

            // 압축을 마친 bounds를 기억해두면 다음 저장부터 이 타일맵은 통째로 건너뛴다
            settledBounds[tilemap] = tilemap.cellBounds;

            after += CellCount(tilemap.cellBounds);
        }

        Debug.Log("[타일맵 압축] " + candidates.Count + "개 처리, 순회 셀 "
            + before.ToString("N0") + " -> " + after.ToString("N0")
            + " (압축 " + compressed + "개" + (restored > 0 ? ", 되돌림 " + restored + "개" : "") + ")");
    }

    // 저장할 때마다 도는 부분이라 여기서는 bounds를 순회하지 않는다
    private static List<Tilemap> FindBloated(Scene scene)
    {
        var candidates = new List<Tilemap>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                BoundsInt bounds = tilemap.cellBounds;

                if (CellCount(bounds) < MinCellsToConsider) continue;

                // 지난번 압축 결과와 같으면 더 줄일 게 없다. 타일을 칠하거나 지우면 bounds가 달라져 다시 걸린다.
                BoundsInt settled;
                if (settledBounds.TryGetValue(tilemap, out settled) && settled == bounds) continue;

                candidates.Add(tilemap);
            }
        }

        return candidates;
    }

    private static bool IsIntact(Tilemap tilemap, List<Vector3Int> positions, List<TileBase> tiles)
    {
        for (int i = 0; i < positions.Count; i++)
            if (tilemap.GetTile(positions[i]) != tiles[i]) return false;

        BoundsInt bounds = tilemap.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0) return positions.Count == 0;

        return tilemap.GetTilesRangeCount(bounds.min, bounds.max - new Vector3Int(1, 1, 0)) == positions.Count;
    }

    private static long CellCount(BoundsInt bounds)
    {
        return (long)bounds.size.x * bounds.size.y * Mathf.Max(1, bounds.size.z);
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;

        return path;
    }
}
