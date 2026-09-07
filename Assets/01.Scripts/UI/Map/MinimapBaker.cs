using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 방 내부 구조를 축소 텍스처로 굽는다. MapUI에서 분리한 부분.
//
// MonoBehaviour가 아닌 이유: MapUI의 [SerializeField] 값을 그대로 쓰려면 필드가 MapUI에 남아야 한다.
// 필드를 이쪽으로 옮기면 직렬화 경로가 바뀌어 인스펙터에서 맞춰둔 색이 전부 날아간다.
// 그래서 설정은 생성할 때 Palette로 받는다.
public class MinimapBaker
{
    // 굽기에 필요한 값 묶음. MapUI가 자기 필드로 채워서 넘긴다.
    public class Palette
    {
        public int pixelsPerTile = 1;
        public bool showObjects = true;

        public Color32 ground;
        public Color32 obstacle;
        public Color32 passable;
        public Color32 wire;
        public Color32 blockZone;

        public Color32 door;
        public Color32 button;
        public Color32 hazard;
        public Color32 checkpoint;
        public Color32 item;
        public Color32 generic;
    }

    private enum TileCategory { Ground, Obstacle, Passable, Wire, BlockZone }

    // 방 하나가 몇 타일인지 (32x18 유닛 / 타일 1유닛)
    private static readonly int TilesX = (int)Room.Width;
    private static readonly int TilesY = (int)Room.Height;

    private readonly Palette palette;
    private readonly int pixelsPerTile;

    public MinimapBaker(Palette palette)
    {
        this.palette = palette;
        pixelsPerTile = Mathf.Max(1, palette.pixelsPerTile);
    }

    // 씬의 모든 타일맵을 한 번만 훑어 방별 텍스처에 픽셀을 찍는다.
    // 타일맵이 어느 Room의 자식인지는 신뢰하지 않고, 타일의 월드 좌표로 소속 방을 계산한다.
    public Dictionary<Room, Sprite> Bake(Room[] rooms)
    {
        int width = TilesX * pixelsPerTile;
        int height = TilesY * pixelsPerTile;

        // 좌표 -> 방, 방 -> 픽셀 버퍼
        var byCoordinate = new Dictionary<Vector2Int, Room>();
        var buffers = new Dictionary<Room, Color32[]>();
        var groundColors = new Dictionary<Room, Color32>();

        foreach (Room room in rooms)
        {
            var key = new Vector2Int(Mathf.RoundToInt(room.Coordinate.x), Mathf.RoundToInt(room.Coordinate.y));
            byCoordinate[key] = room;

            buffers[room] = new Color32[width * height];   // 기본값은 투명

            // 스테이지 조회를 타일마다 하지 않도록 방 단위로 미리 구해둔다
            groundColors[room] = GroundColorOf(room);
        }

        PaintTilemaps(byCoordinate, buffers, groundColors, width, height);

        if (palette.showObjects)
            PaintObjects(rooms, buffers, width, height);

        return PackAtlas(rooms, buffers, width, height);
    }

    // ---------------------------------------------------------------- 타일

    private void PaintTilemaps(Dictionary<Vector2Int, Room> byCoordinate, Dictionary<Room, Color32[]> buffers,
                               Dictionary<Room, Color32> groundColors, int width, int height)
    {
        // Ground -> Wire/Passable -> Obstacle 순으로 덮어써서 위험 요소가 위에 보이도록
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
        System.Array.Sort(tilemaps, (a, b) => DrawOrderOf(a).CompareTo(DrawOrderOf(b)));

        foreach (Tilemap tilemap in tilemaps)
        {
            // 지형은 방의 스테이지에 따라 색이 달라지므로 방을 찾은 뒤에 결정한다.
            // 나머지 분류는 타일맵마다 고정이라 여기서 한 번만 구한다.
            TileCategory category = CategoryOf(tilemap);
            Color32 categoryColor = TileColorOf(category);

            foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(position)) continue;

                Room room;
                if (!byCoordinate.TryGetValue(Room.WorldToCoordinate(tilemap.GetCellCenterWorld(position)), out room)) continue;

                Color32 color = category == TileCategory.Ground ? groundColors[room] : categoryColor;

                // 픽셀 시작점은 타일 중심이 아니라 좌하단 코너로 잡아야 한다.
                // 중심(x.5)을 쓰면 pixelsPerTile이 2 이상일 때 마지막 타일이 텍스처 밖으로 반 칸 넘친다.
                Vector3 corner = tilemap.CellToWorld(position);
                Vector2 origin = RoomOrigin(room);

                int px = Mathf.RoundToInt((corner.x - origin.x) * pixelsPerTile);
                int py = Mathf.RoundToInt((corner.y - origin.y) * pixelsPerTile);

                FillRect(buffers[room], px, px + pixelsPerTile, py, py + pixelsPerTile, width, height, color);
            }
        }
    }

    // ---------------------------------------------------------------- 오브젝트

    // 타일 위에 오브젝트를 덮어 찍는다. 타일맵과 달리 이쪽은 Room 자식 구조가 신뢰할 수 있다.
    private void PaintObjects(Room[] rooms, Dictionary<Room, Color32[]> buffers, int width, int height)
    {
        foreach (Room room in rooms)
        {
            Color32[] buffer = buffers[room];

            foreach (SpriteRenderer renderer in room.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // 차원문은 클릭 가능한 UI 아이콘으로 따로 그린다.
                // 여기서도 찍으면 아이콘 밑에 픽셀 덩어리가 남아 지저분해진다.
                if (renderer.GetComponentInParent<Portal>() != null) continue;

                Bounds bounds = renderer.bounds;

                // 방보다 큰 스프라이트는 배경 장식으로 보고 건너뛴다 (셀을 통째로 덮어버린다)
                if (bounds.size.x > Room.Width || bounds.size.y > Room.Height) continue;

                PaintWorldBounds(buffer, room, bounds, width, height, ObjectColorOf(renderer, room));
            }

            // 레이저 광선은 LineRenderer를 못 믿으므로 직접 구간을 받아 선으로 찍는다
            foreach (LaserObstacle laser in room.GetComponentsInChildren<LaserObstacle>(true))
            {
                Vector2 origin, end;
                if (!laser.TryGetBeam(out origin, out end)) continue;

                PaintWorldLine(buffer, room, origin, end, width, height, palette.hazard);
            }
        }
    }

    // 월드 AABB를 방 로컬 픽셀 사각형으로 바꿔 칠한다. 아주 얇은 것도 최소 1픽셀은 남긴다.
    private void PaintWorldBounds(Color32[] buffer, Room room, Bounds bounds, int width, int height, Color32 color)
    {
        Vector2 origin = RoomOrigin(room);

        int x0 = Mathf.FloorToInt((bounds.min.x - origin.x) * pixelsPerTile);
        int x1 = Mathf.CeilToInt((bounds.max.x - origin.x) * pixelsPerTile);
        int y0 = Mathf.FloorToInt((bounds.min.y - origin.y) * pixelsPerTile);
        int y1 = Mathf.CeilToInt((bounds.max.y - origin.y) * pixelsPerTile);

        if (x1 <= x0) x1 = x0 + 1;
        if (y1 <= y0) y1 = y0 + 1;

        FillRect(buffer, x0, x1, y0, y1, width, height, color);
    }

    // 월드 선분을 픽셀 단위로 따라가며 칠한다. 기울어진 레이저도 그대로 표현된다.
    private void PaintWorldLine(Color32[] buffer, Room room, Vector2 from, Vector2 to, int width, int height, Color32 color)
    {
        Vector2 origin = RoomOrigin(room);

        Vector2 fromPixel = (from - origin) * pixelsPerTile;
        Vector2 toPixel = (to - origin) * pixelsPerTile;

        // 픽셀 하나도 건너뛰지 않도록 반 픽셀씩 전진
        int steps = Mathf.CeilToInt(Vector2.Distance(fromPixel, toPixel) * 2f);
        if (steps <= 0) steps = 1;

        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(fromPixel, toPixel, (float)i / steps);

            int x = Mathf.FloorToInt(point.x);
            int y = Mathf.FloorToInt(point.y);
            if (x < 0 || x >= width || y < 0 || y >= height) continue;   // 방 밖으로 나간 구간은 버린다

            buffer[y * width + x] = color;
        }
    }

    // ---------------------------------------------------------------- 아틀라스

    // 방마다 텍스처를 만들면 Image가 서로 다른 텍스처를 참조해 UI 배칭이 깨진다(방 1개 = 드로우 콜 1개).
    // 한 장에 슬롯을 나눠 담고 Sprite의 rect로 잘라 쓰면 전부 같은 텍스처가 되어 한 배치로 묶인다.
    // 슬롯 위치는 지도상 배치와 무관하므로 순서대로 채운다.
    private Dictionary<Room, Sprite> PackAtlas(Room[] rooms, Dictionary<Room, Color32[]> buffers, int width, int height)
    {
        var sprites = new Dictionary<Room, Sprite>();

        int columns = Mathf.CeilToInt(Mathf.Sqrt(rooms.Length));
        int rows = Mathf.CeilToInt(rooms.Length / (float)columns);

        var atlas = new Texture2D(columns * width, rows * height, TextureFormat.RGBA32, false);
        atlas.filterMode = FilterMode.Point;    // 보간이 없어 슬롯 사이 여백(padding)이 필요 없다
        atlas.wrapMode = TextureWrapMode.Clamp;

        for (int i = 0; i < rooms.Length; i++)
        {
            Room room = rooms[i];

            int slotX = (i % columns) * width;
            int slotY = (i / columns) * height;

            atlas.SetPixels32(slotX, slotY, width, height, buffers[room]);

            sprites[room] = Sprite.Create(
                atlas,
                new Rect(slotX, slotY, width, height),
                new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
        }

        // 두 번째 인자 true = CPU 사본 해제. 이후 GetPixels 계열은 못 쓴다.
        atlas.Apply(false, true);

        return sprites;
    }

    // ---------------------------------------------------------------- 색 판정

    // 스프라이트가 붙은 오브젝트부터 Room까지 부모를 거슬러 올라가며 아는 스크립트를 찾는다.
    // (레이저의 Mouth처럼 자식에 스프라이트만 있는 경우가 있다)
    private Color32 ObjectColorOf(SpriteRenderer renderer, Room room)
    {
        Transform cursor = renderer.transform;

        while (cursor != null)
        {
            // Door와 KeyDoor가 둘 다 ScalingDoor라서 한 번만 물어보면 된다
            if (cursor.GetComponent<ScalingDoor>() != null) return palette.door;
            if (cursor.GetComponent<ButtonZone>() != null) return palette.button;
            if (cursor.GetComponent<LaserObstacle>() != null) return palette.hazard;
            if (cursor.GetComponent<Checkpoint>() != null) return palette.checkpoint;
            if (cursor.GetComponent<Key>() != null || cursor.GetComponent<KeyHolderTrigger>() != null) return palette.item;
            if (cursor.GetComponent<AbilityBase>() != null) return palette.item;

            if (cursor == room.transform) break;
            cursor = cursor.parent;
        }

        // 아는 스크립트가 없으면 태그/레이어로 위험물 여부만 판별
        if (renderer.CompareTag("Obstacle")) return palette.hazard;
        if (LayerMask.LayerToName(renderer.gameObject.layer) == "Obstacle") return palette.hazard;

        return palette.generic;
    }

    // 레이어를 우선 보고, 레이어가 Default인 타일맵은 이름으로 판정한다.
    private int DrawOrderOf(Tilemap tilemap)
    {
        switch (CategoryOf(tilemap))
        {
            case TileCategory.Ground: return 0;
            case TileCategory.Wire: return 1;
            case TileCategory.Passable: return 2;
            case TileCategory.BlockZone: return 3;
            case TileCategory.Obstacle: return 4;
        }
        return 1;
    }

    private Color32 TileColorOf(TileCategory category)
    {
        switch (category)
        {
            case TileCategory.Obstacle: return palette.obstacle;
            case TileCategory.Passable: return palette.passable;
            case TileCategory.Wire: return palette.wire;
            case TileCategory.BlockZone: return palette.blockZone;
        }
        return palette.ground;
    }

    // 지형 색은 방이 참조하는 StageData가 소유한다.
    // 스테이지가 비어 있는 방만 기본 색으로 떨어진다.
    private Color32 GroundColorOf(Room room)
    {
        if (room == null || room.Stage == null) return palette.ground;

        return room.Stage.MinimapGroundColor;
    }

    private TileCategory CategoryOf(Tilemap tilemap)
    {
        string layer = LayerMask.LayerToName(tilemap.gameObject.layer);

        if (layer == "Obstacle") return TileCategory.Obstacle;
        if (layer == "PassableGround") return TileCategory.Passable;
        if (layer == "BlockZone") return TileCategory.BlockZone;
        if (layer == "Ground") return TileCategory.Ground;

        // 레이어가 지정되지 않은 타일맵은 이름으로 추정
        string name = tilemap.name;
        if (name.Contains("Obstacle")) return TileCategory.Obstacle;
        if (name.Contains("Passable")) return TileCategory.Passable;
        if (name.Contains("Wire")) return TileCategory.Wire;
        if (name.Contains("BlockZone")) return TileCategory.BlockZone;

        return TileCategory.Ground;
    }

    // ---------------------------------------------------------------- 공통

    // Room의 Transform은 방 중앙이므로 픽셀 계산의 기준점은 좌하단으로 옮겨야 한다
    private static Vector2 RoomOrigin(Room room)
    {
        Vector3 center = room.transform.position;

        return new Vector2(center.x - Room.Width * 0.5f, center.y - Room.Height * 0.5f);
    }

    private static void FillRect(Color32[] buffer, int x0, int x1, int y0, int y1, int width, int height, Color32 color)
    {
        x0 = Mathf.Clamp(x0, 0, width);
        x1 = Mathf.Clamp(x1, 0, width);
        y0 = Mathf.Clamp(y0, 0, height);
        y1 = Mathf.Clamp(y1, 0, height);

        for (int y = y0; y < y1; y++)
        {
            int rowStart = y * width;
            for (int x = x0; x < x1; x++)
                buffer[rowStart + x] = color;
        }
    }
}
