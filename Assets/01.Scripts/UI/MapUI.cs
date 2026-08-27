using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// M키로 여는 전체 지도.
// 씬의 Room을 전부 수집해 Coordinate를 UI 격자로 그리고,
// 각 방의 타일맵을 읽어 방 내부 구조를 축소 텍스처로 굽는다.
// 방/타일을 추가해도 이 스크립트는 손댈 필요가 없다.
// 휠로 확대/축소, 확대된 상태에서 좌클릭 드래그로 이동.
public class MapUI : MonoBehaviour
{
    public static MapUI Instance;

    // 방 하나가 몇 타일인지 (32x18 유닛 / 타일 1유닛)
    private const int TilesX = (int)Room.Width;
    private const int TilesY = (int)Room.Height;

    [Header("참조")]
    [SerializeField] private GameObject panel;             // M키로 켜고 끄는 루트 (이 컴포넌트는 항상 활성인 곳에 둔다)
    [SerializeField] private RectTransform cellContainer;  // 셀들의 부모. 앵커/피벗 중앙
    [SerializeField] private RectTransform cellPrefab;     // Image 하나 붙은 셀

    [Header("셀 배치")]
    [Tooltip("방 하나의 크기(px). 방이 32x18 유닛이므로 16:9를 유지하면 비율이 보존된다.")]
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 36f);
    [Tooltip("칸 사이 간격. 0이면 방들이 완전히 붙어 전체 맵이 이어져 보인다")]
    [SerializeField] private Vector2 gap = Vector2.zero;

    [Header("타일 구조 표시")]
    [Tooltip("타일 1개를 몇 픽셀로 구울지. 1이면 방당 32x18 텍스처")]
    [SerializeField] private int pixelsPerTile = 1;
    [Tooltip("StageData가 지정되지 않은 방이 쓰는 기본 지형 색. 스테이지별 색은 각 StageData 에셋에서 설정한다")]
    [SerializeField] private Color groundTileColor = new Color(0.80f, 0.82f, 0.88f, 1f);
    [SerializeField] private Color obstacleTileColor = new Color(0.95f, 0.30f, 0.30f, 1f);
    [SerializeField] private Color passableTileColor = new Color(0.45f, 0.70f, 0.95f, 1f);
    [SerializeField] private Color wireTileColor = new Color(0.55f, 0.50f, 0.35f, 1f);
    [Tooltip("그림자만 막는 영역. 지형과 구분되어야 한다")]
    [SerializeField] private Color blockZoneTileColor = new Color(0.65f, 0.40f, 0.95f, 1f);

    [Header("오브젝트 표시 (문 / 버튼 / 레이저 등)")]
    [Tooltip("타일맵이 아닌 방 자식 오브젝트를 SpriteRenderer 범위로 찍는다")]
    [SerializeField] private bool showObjects = true;
    [SerializeField] private Color doorColor = new Color(0.40f, 0.85f, 0.50f, 1f);
    [SerializeField] private Color buttonColor = new Color(1.00f, 0.60f, 0.20f, 1f);
    [SerializeField] private Color hazardColor = new Color(1.00f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color checkpointColor = new Color(0.30f, 0.95f, 0.90f, 1f);
    [SerializeField] private Color itemColor = new Color(1.00f, 0.95f, 0.40f, 1f);
    [SerializeField] private Color genericObjectColor = new Color(0.70f, 0.60f, 0.85f, 1f);

    [Header("확대 / 축소")]
    [SerializeField] private float defaultZoom = 1f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 5f;
    [Tooltip("휠 한 칸당 배율 증가량. 0.15면 한 칸에 15% 확대")]
    [SerializeField] private float zoomStep = 0.15f;

    [Header("이동 (팬)")]
    [Tooltip("지도 바깥으로 더 밀 수 있는 여유를 방 개수로 지정")]
    [SerializeField] private int paddingRooms = 2;
    [Tooltip("끄면 무제한 이동")]
    [SerializeField] private bool clampToBounds = true;

    [Header("방 상태 표현")]
    [Tooltip("방문한 방의 틴트. 흰색이면 타일 색이 그대로 보인다")]
    [SerializeField] private Color visitedTint = Color.white;
    [SerializeField] private RectTransform currentRoomHighlight;   // 현재 방 테두리 (없어도 동작)

    [Header("플레이어 표시")]
    [SerializeField] private RectTransform playerMarker;   // 없어도 동작
    [SerializeField] private bool showPlayerMarker = true;

    [Header("고속이동 차원문")]
    [Tooltip("차원문 아이콘 프리팹. Image + Button이 붙어 있어야 한다")]
    [SerializeField] private RectTransform portalIconPrefab;
    [SerializeField] private Vector2 portalIconSize = new Vector2(12f, 12f);
    [SerializeField] private float teleportTime = 1f;
    [Tooltip("차원문 아이콘 색. 흰색이면 스프라이트 원본 색이 그대로 나온다. 모드와 무관하게 항상 같은 색을 쓴다")]
    [SerializeField] private Color portalIconColor = Color.white;
    [Tooltip("이 픽셀 이상 끌면 클릭이 아니라 드래그로 본다")]
    [SerializeField] private float dragThreshold = 6f;

    [Header("사운드")]
    [SerializeField] private SoundData mapOpenSound;
    [SerializeField] private SoundData mapCloseSound;
    [SerializeField] private SoundData portalInSound;
    [SerializeField] private SoundData portalOutSound;

    [Header("디버그")]
    [Tooltip("F1로 모든 방을 방문 처리한다. 빌드에 넣고 싶지 않으면 끈다")]
    [SerializeField] private bool enableDebugReveal = true;

    private readonly Dictionary<Room, Image> cells = new Dictionary<Room, Image>();
    private InputActions inputActions;

    private Room currentRoom;
    private Vector2 gridCenter;
    private Vector2 gridExtent;         // 격자의 가로/세로 칸 수 - 1 (좌표 최대 - 최소)

    private RectTransform panelRect;
    private float zoom = 1f;

    // 일반 보기(M)와 고속이동(차원문에서 F) 두 모드가 있다.
    // 차원문 선택은 Travel 모드에서만 가능하다.
    private enum MapMode { View, Travel }
    private MapMode mode = MapMode.View;

    private readonly Dictionary<Portal, Button> portalIcons = new Dictionary<Portal, Button>();
    private Portal originPortal;    // Travel 모드로 열 때 올라가 있던 차원문

    private bool isDragging;
    private bool dragMoved;         // 이번 누름이 임계값을 넘어 드래그가 되었나 (클릭과 구분)
    private Vector2 dragStartLocal;
    private Vector2 dragStartAnchored;

    // 지도가 열려 있는 동안에는 다른 마우스 입력을 막아야 한다 (MarkingManager에서 참조)
    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.UI.Map.performed += OnToggleMap;
        inputActions.UI.Interact.performed += OnInteract;
        inputActions.UI.DebugRevealMap.performed += OnDebugRevealMap;
    }

    private void OnDisable()
    {
        inputActions.UI.Map.performed -= OnToggleMap;
        inputActions.UI.Interact.performed -= OnInteract;
        inputActions.UI.DebugRevealMap.performed -= OnDebugRevealMap;
        inputActions.Disable();
    }

    private void Start()
    {
        if (panel != null)
            panelRect = panel.GetComponent<RectTransform>();

        BuildCells();

        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;

        HandleZoom();
        HandleDrag();
        UpdatePlayerMarker();   // 지도를 열어둔 채로 플레이어가 움직일 수 있으므로 매 프레임 갱신
    }

    // ---------------------------------------------------------------- 셀 생성

    // 씬의 Room을 전부 긁어와 셀을 만든다. 방 추가 시 자동 반영.
    private void BuildCells()
    {
        if (cellContainer == null || cellPrefab == null)
        {
            Debug.LogWarning("cellContainer 또는 cellPrefab이 지정되지 않았습니다.", this);
            return;
        }

        Room[] rooms = FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (rooms.Length == 0) return;

        // 지도를 컨테이너 중앙에 맞추기 위한 격자 중심
        Vector2 min = rooms[0].Coordinate;
        Vector2 max = rooms[0].Coordinate;
        foreach (Room room in rooms)
        {
            min = Vector2.Min(min, room.Coordinate);
            max = Vector2.Max(max, room.Coordinate);
        }
        gridCenter = (min + max) * 0.5f;
        gridExtent = max - min;

        Dictionary<Room, Sprite> structures = BakeRoomStructures(rooms);

        foreach (Room room in rooms)
        {
            RectTransform cell = Instantiate(cellPrefab, cellContainer);
            cell.name = "Cell " + room.name;
            cell.sizeDelta = cellSize;
            cell.anchoredPosition = CoordinateToLocal(room.Coordinate);

            Image image = cell.GetComponent<Image>();
            if (image == null) continue;

            Sprite sprite;
            if (structures.TryGetValue(room, out sprite))
                image.sprite = sprite;

            image.enabled = room.IsVisited;   // 지도를 처음 열기 전에도 미방문 방은 숨겨둔다
            cells[room] = image;
        }

        BuildPortalIcons(rooms);

        // 렌더 순서를 형제 순서로 정한다. 뒤에 있는 형제가 위에 그려진다.
        //   셀 < 현재 방 테두리 < 차원문 아이콘 < 플레이어 마커
        if (currentRoomHighlight != null)
        {
            currentRoomHighlight.sizeDelta = cellSize;
            currentRoomHighlight.SetAsLastSibling();
            currentRoomHighlight.gameObject.SetActive(false);
        }

        foreach (Button icon in portalIcons.Values)
            icon.transform.SetAsLastSibling();

        if (playerMarker != null)
            playerMarker.SetAsLastSibling();
    }

    // 격자 좌표 -> 컨테이너 로컬 좌표(px)
    private Vector2 CoordinateToLocal(Vector2 coordinate)
    {
        return (coordinate - gridCenter) * (cellSize + gap);
    }

    // 방 안의 월드 좌표 -> 그 방 셀 안의 컨테이너 로컬 좌표(px)
    // Room의 Transform은 방 중앙이므로 좌하단으로 옮겨 0~1 비율을 낸다.
    private Vector2 WorldToCellLocal(Room room, Vector3 worldPosition)
    {
        Vector3 roomCenter = room.transform.position;

        float u = Mathf.Clamp01((worldPosition.x - (roomCenter.x - Room.Width * 0.5f)) / Room.Width);
        float v = Mathf.Clamp01((worldPosition.y - (roomCenter.y - Room.Height * 0.5f)) / Room.Height);

        Vector2 cellOrigin = CoordinateToLocal(room.Coordinate) - cellSize * 0.5f;
        return cellOrigin + new Vector2(u * cellSize.x, v * cellSize.y);
    }

    // 차원문 아이콘은 구운 텍스처가 아니라 별도 UI여야 한다. 텍스처 픽셀은 클릭을 못 받는다.
    private void BuildPortalIcons(Room[] rooms)
    {
        if (portalIconPrefab == null || cellContainer == null) return;

        foreach (Room room in rooms)
        {
            if (room.Portals == null) continue;

            foreach (Portal portal in room.Portals)
            {
                RectTransform icon = Instantiate(portalIconPrefab, cellContainer);
                icon.name = "Portal " + room.name;
                icon.sizeDelta = portalIconSize;

                // 발밑을 기준점으로 두어 아이콘을 키워도 위로만 자라게 한다
                icon.pivot = new Vector2(0.5f, 0f);
                icon.anchoredPosition = WorldToCellLocal(room, portal.MapFootPosition);

                // 차원문 스프라이트를 그대로 아이콘으로 쓴다. 비율은 유지한다.
                Image iconImage = icon.GetComponent<Image>();
                if (iconImage != null)
                {
                    Sprite sprite = portal.MapIcon;
                    if (sprite != null)
                    {
                        iconImage.sprite = sprite;
                        iconImage.preserveAspect = true;
                    }
                }

                Button button = icon.GetComponent<Button>();
                if (button == null)
                {
                    Debug.LogWarning("portalIconPrefab에 Button이 없습니다. 선택할 수 없습니다.", this);
                    continue;
                }

                // Button의 ColorTint는 targetGraphic의 색을 자기가 덮어쓴다.
                // 상태별 색은 MapUI가 Image.color로 직접 칠하므로 트랜지션을 끈다.
                // (켜두면 interactable=false일 때 disabledColor의 알파가 먹어 반투명해진다)
                button.transition = Selectable.Transition.None;

                // 프리팹에 없더라도 호버 피드백이 붙도록 보장
                if (icon.GetComponent<MapPortalIconHover>() == null)
                    icon.gameObject.AddComponent<MapPortalIconHover>();

                Portal captured = portal;   // 클로저가 반복 변수를 잡지 않도록 복사
                button.onClick.AddListener(() => OnPortalClicked(captured));

                portalIcons[portal] = button;
            }
        }
    }

    // ---------------------------------------------------------------- 타일 구조 굽기

    // 씬의 모든 타일맵을 한 번만 훑어 방별 텍스처에 픽셀을 찍는다.
    // 타일맵이 어느 Room의 자식인지는 신뢰하지 않고, 타일의 월드 좌표로 소속 방을 계산한다.
    private Dictionary<Room, Sprite> BakeRoomStructures(Room[] rooms)
    {
        var sprites = new Dictionary<Room, Sprite>();
        if (pixelsPerTile < 1) pixelsPerTile = 1;

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

            var buffer = new Color32[width * height];   // 기본값은 투명
            buffers[room] = buffer;

            // 스테이지 조회를 타일마다 하지 않도록 방 단위로 미리 구해둔다
            groundColors[room] = GroundColorOf(room);
        }

        // Ground -> Wire/Passable -> Obstacle 순으로 덮어써서 위험 요소가 위에 보이도록
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

                Vector3 world = tilemap.GetCellCenterWorld(position);

                Room room;
                if (!byCoordinate.TryGetValue(Room.WorldToCoordinate(world), out room)) continue;

                Color32 color = category == TileCategory.Ground ? groundColors[room] : categoryColor;

                // 픽셀 시작점은 타일 중심이 아니라 좌하단 코너로 잡아야 한다.
                // 중심(x.5)을 쓰면 pixelsPerTile이 2 이상일 때 마지막 타일이 텍스처 밖으로 반 칸 넘친다.
                Vector3 corner = tilemap.CellToWorld(position);

                Vector3 roomCenter = room.transform.position;
                float localX = corner.x - (roomCenter.x - Room.Width * 0.5f);
                float localY = corner.y - (roomCenter.y - Room.Height * 0.5f);

                int px = Mathf.RoundToInt(localX * pixelsPerTile);
                int py = Mathf.RoundToInt(localY * pixelsPerTile);

                Color32[] buffer = buffers[room];
                for (int oy = 0; oy < pixelsPerTile; oy++)
                {
                    int y = py + oy;
                    if (y < 0 || y >= height) continue;

                    int rowStart = y * width;
                    for (int ox = 0; ox < pixelsPerTile; ox++)
                    {
                        int x = px + ox;
                        if (x < 0 || x >= width) continue;

                        buffer[rowStart + x] = color;
                    }
                }
            }
        }

        // 타일 위에 오브젝트를 덮어 찍는다. 타일맵과 달리 이쪽은 Room 자식 구조가 신뢰할 수 있다.
        if (showObjects)
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

                    PaintWorldLine(buffer, room, origin, end, width, height, hazardColor);
                }
            }
        }

        // 방마다 텍스처를 만들면 Image가 서로 다른 텍스처를 참조해 UI 배칭이 깨진다(방 1개 = 드로우 콜 1개).
        // 한 장에 슬롯을 나눠 담고 Sprite의 rect로 잘라 쓰면 전부 같은 텍스처가 되어 한 배치로 묶인다.
        // 슬롯 위치는 지도상 배치와 무관하므로 순서대로 채운다.
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

    // 월드 AABB를 방 로컬 픽셀 사각형으로 바꿔 칠한다. 아주 얇은 것도 최소 1픽셀은 남긴다.
    private void PaintWorldBounds(Color32[] buffer, Room room, Bounds bounds, int width, int height, Color32 color)
    {
        Vector3 roomCenter = room.transform.position;
        float originX = roomCenter.x - Room.Width * 0.5f;
        float originY = roomCenter.y - Room.Height * 0.5f;

        int x0 = Mathf.FloorToInt((bounds.min.x - originX) * pixelsPerTile);
        int x1 = Mathf.CeilToInt((bounds.max.x - originX) * pixelsPerTile);
        int y0 = Mathf.FloorToInt((bounds.min.y - originY) * pixelsPerTile);
        int y1 = Mathf.CeilToInt((bounds.max.y - originY) * pixelsPerTile);

        if (x1 <= x0) x1 = x0 + 1;
        if (y1 <= y0) y1 = y0 + 1;

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

    // 월드 선분을 픽셀 단위로 따라가며 칠한다. 기울어진 레이저도 그대로 표현된다.
    private void PaintWorldLine(Color32[] buffer, Room room, Vector2 from, Vector2 to, int width, int height, Color32 color)
    {
        Vector3 roomCenter = room.transform.position;
        var origin = new Vector2(roomCenter.x - Room.Width * 0.5f, roomCenter.y - Room.Height * 0.5f);

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

    // 스프라이트가 붙은 오브젝트부터 Room까지 부모를 거슬러 올라가며 아는 스크립트를 찾는다.
    // (레이저의 Mouth처럼 자식에 스프라이트만 있는 경우가 있다)
    private Color32 ObjectColorOf(SpriteRenderer renderer, Room room)
    {
        Transform cursor = renderer.transform;

        while (cursor != null)
        {
            if (cursor.GetComponent<Door>() != null || cursor.GetComponent<KeyDoor>() != null) return doorColor;
            if (cursor.GetComponent<ButtonZone>() != null) return buttonColor;
            if (cursor.GetComponent<LaserObstacle>() != null) return hazardColor;
            if (cursor.GetComponent<Checkpoint>() != null) return checkpointColor;
            if (cursor.GetComponent<Key>() != null || cursor.GetComponent<KeyHolderTrigger>() != null) return itemColor;
            if (cursor.GetComponent<AbilityBase>() != null) return itemColor;

            if (cursor == room.transform) break;
            cursor = cursor.parent;
        }

        // 아는 스크립트가 없으면 태그/레이어로 위험물 여부만 판별
        if (renderer.CompareTag("Obstacle")) return hazardColor;
        if (LayerMask.LayerToName(renderer.gameObject.layer) == "Obstacle") return hazardColor;

        return genericObjectColor;
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
            case TileCategory.Obstacle: return obstacleTileColor;
            case TileCategory.Passable: return passableTileColor;
            case TileCategory.Wire: return wireTileColor;
            case TileCategory.BlockZone: return blockZoneTileColor;
        }
        return groundTileColor;
    }

    // 지형 색은 방이 참조하는 StageData가 소유한다.
    // 스테이지가 비어 있는 방만 기본 색으로 떨어진다.
    private Color32 GroundColorOf(Room room)
    {
        if (room == null || room.Stage == null) return groundTileColor;

        return room.Stage.MinimapGroundColor;
    }

    private enum TileCategory { Ground, Obstacle, Passable, Wire, BlockZone }

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

    // ---------------------------------------------------------------- 열기 / 닫기

    // 방에 진입할 때 RoomCameraTrigger에서 호출
    public void SetCurrentRoom(Room room)
    {
        if (room == null) return;

        currentRoom = room;
        room.MarkVisited();

        // 지도를 열지 않아도 방문 시점에 바로 드러나도록 해당 셀만 갱신
        Image image;
        if (cells.TryGetValue(room, out image))
        {
            image.enabled = true;
            image.color = visitedTint;
        }
    }

    private void OnToggleMap(InputAction.CallbackContext context)
    {
        if (panel == null) return;

        if (panel.activeSelf)
        {
            Close();
            return;
        }

        Open(MapMode.View, null);
    }

    // 차원문에 올라간 상태에서 상호작용키(F).
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (panel == null) return;

        if (panel.activeSelf)
        {
            // F로 연 지도는 F로 닫는다. M으로 연 지도는 M으로만 닫는다.
            if (mode == MapMode.Travel) Close();
            return;
        }

        Portal origin = Portal.Current;
        if (origin == null) return;             // 차원문에 올라가 있지 않음

        Open(MapMode.Travel, origin);
    }

    private void Open(MapMode openMode, Portal origin)
    {
        mode = openMode;
        originPortal = origin;

        panel.SetActive(true);

        AudioManager.PlayUiSfx(mapOpenSound);

        // Travel 모드에서는 출발 차원문이 있는 방을 중앙에 둔다
        ResetView(origin != null && origin.Room != null ? origin.Room : currentRoom);
        Refresh();
    }

    // 디버그: 모든 방을 방문 처리한다. 되돌리는 기능은 없다(Room.IsVisited는 한 방향).
    private void OnDebugRevealMap(InputAction.CallbackContext context)
    {
        if (!enableDebugReveal) return;

        RevealAllRooms();
    }

    public void RevealAllRooms()
    {
        int revealed = 0;

        // cells에 없는 방까지 포함하도록 씬에서 다시 훑는다
        foreach (Room room in FindObjectsByType<Room>(FindObjectsInactive.Include))
        {
            if (room.IsVisited) continue;

            room.MarkVisited();
            revealed++;
        }

        // 닫혀 있어도 셀/아이콘 상태를 맞춰둔다. 다음에 열 때 바로 반영된다.
        Refresh();

        Debug.Log("[디버그] 방 " + revealed + "개를 새로 방문 처리했습니다. (총 " + cells.Count + "개 셀)");
    }

    private void Close()
    {
        panel.SetActive(false);

        AudioManager.PlayUiSfx(mapCloseSound);
        mode = MapMode.View;
        originPortal = null;
        isDragging = false;
        dragMoved = false;
    }

    // 열 때마다 기본 배율로 돌리고 현재 방을 화면 정가운데에 둔다.
    // 이전에 보던 위치를 유지하고 싶으면 이 호출만 빼면 된다.
    private void ResetView(Room focus)
    {
        zoom = Mathf.Clamp(defaultZoom, minZoom, maxZoom);
        ApplyZoom();
        CenterOnRoom(focus);

        isDragging = false;
        dragMoved = false;
    }

    // 지정한 방의 셀이 패널 중앙에 오도록 컨테이너를 옮긴다.
    // 셀의 컨테이너 로컬 위치가 zoom배로 확대되므로 그만큼 반대로 밀어준다.
    private void CenterOnRoom(Room focus)
    {
        if (cellContainer == null) return;

        cellContainer.anchoredPosition = focus == null
            ? Vector2.zero                                              // 아직 방 진입 전이면 격자 중앙
            : -CoordinateToLocal(focus.Coordinate) * zoom;

        ClampPosition();
    }

    // ---------------------------------------------------------------- 확대 / 축소

    private void HandleZoom()
    {
        if (cellContainer == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        float prevZoom = zoom;

        // 휠 한 칸이 보통 120. 트랙패드의 작은 값도 비례해서 반영되도록 나눈다.
        float notches = scroll / 120f;
        zoom = Mathf.Clamp(zoom * Mathf.Pow(1f + zoomStep, notches), minZoom, maxZoom);

        if (Mathf.Approximately(zoom, prevZoom)) return;

        // 커서 아래의 지점을 제자리에 고정한 채 확대/축소
        Vector2 cursorLocal;
        if (TryGetPanelLocalPoint(Mouse.current.position.ReadValue(), out cursorLocal))
        {
            Vector2 anchored = cellContainer.anchoredPosition;
            cellContainer.anchoredPosition = cursorLocal - (cursorLocal - anchored) * (zoom / prevZoom);
        }

        ApplyZoom();
        ClampPosition();
    }

    private void ApplyZoom()
    {
        if (cellContainer != null)
            cellContainer.localScale = new Vector3(zoom, zoom, 1f);
    }

    // ---------------------------------------------------------------- 드래그 이동

    private void HandleDrag()
    {
        if (cellContainer == null) return;

        Mouse mouse = Mouse.current;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (TryGetPanelLocalPoint(mouse.position.ReadValue(), out dragStartLocal))
            {
                isDragging = true;
                dragMoved = false;      // 누른 순간에는 아직 클릭 후보
                dragStartAnchored = cellContainer.anchoredPosition;
            }
        }

        if (!isDragging) return;

        if (!mouse.leftButton.isPressed)
        {
            isDragging = false;
            return;
        }

        Vector2 current;
        if (!TryGetPanelLocalPoint(mouse.position.ReadValue(), out current)) return;

        Vector2 delta = current - dragStartLocal;

        // 임계값을 넘는 순간부터 드래그로 확정. 이후 버튼 클릭은 무시된다.
        if (!dragMoved && delta.magnitude > dragThreshold)
            dragMoved = true;

        if (!dragMoved) return;         // 아직 클릭일 수 있으니 지도를 움직이지 않는다

        cellContainer.anchoredPosition = dragStartAnchored + delta;
        ClampPosition();
    }

    // 어떤 방이든 화면 정가운데로 가져올 수 있고, 그 바깥으로 paddingRooms 칸만큼 더 여유를 준다.
    // (화면보다 작을 때 중앙 고정하는 방식으로는 특정 방을 가운데 놓을 수 없다)
    private void ClampPosition()
    {
        if (!clampToBounds || cellContainer == null) return;

        Vector2 step = cellSize + gap;
        Vector2 half = gridExtent * 0.5f + Vector2.one * paddingRooms;
        Vector2 limit = half * step * zoom;

        Vector2 pos = cellContainer.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -limit.x, limit.x);
        pos.y = Mathf.Clamp(pos.y, -limit.y, limit.y);
        cellContainer.anchoredPosition = pos;
    }

    // Screen Space - Overlay 캔버스이므로 camera는 null을 넘긴다.
    private bool TryGetPanelLocalPoint(Vector2 screenPosition, out Vector2 local)
    {
        local = Vector2.zero;
        if (panelRect == null) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect, screenPosition, null, out local);
    }

    // ---------------------------------------------------------------- 갱신

    private void Refresh()
    {
        foreach (KeyValuePair<Room, Image> pair in cells)
        {
            // 방문하지 않은 방은 Image 자체를 끈다. 방이 거기 있다는 것조차 보이면 안 된다.
            // 방문한 순간부터 그 방의 타일 구조가 드러난다.
            bool visited = pair.Key.IsVisited;

            pair.Value.enabled = visited;
            if (visited)
                pair.Value.color = visitedTint;
        }

        UpdatePortalIcons();
        UpdateCurrentRoomHighlight();
        UpdatePlayerMarker();
    }

    // 방문한 방의 차원문만 보인다. 선택은 Travel 모드에서만 가능하다.
    private void UpdatePortalIcons()
    {
        foreach (KeyValuePair<Portal, Button> pair in portalIcons)
        {
            Portal portal = pair.Key;
            Button button = pair.Value;

            bool visible = portal.Room != null && portal.Room.IsVisited;
            button.gameObject.SetActive(visible);
            if (!visible) continue;

            // 선택 가능 여부는 모드로만 갈린다. 색은 어느 모드에서든 동일하게 둔다.
            button.interactable = mode == MapMode.Travel && portal != originPortal;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = portalIconColor;
        }
    }

    // 차원문 아이콘 클릭
    private void OnPortalClicked(Portal target)
    {
        if (dragMoved) return;                  // 지도를 끌던 손이 떨어진 것은 클릭이 아니다
        if (mode != MapMode.Travel) return;     // 일반 지도(M)에서는 선택 불가
        if (target == null || target == originPortal) return;

        if (PlayerController.Instance == null) return;

        StartCoroutine(TravelTo(target));
    }

    IEnumerator TravelTo(Portal target)
    {

        // 다른 포탈로 순간이동을 클릭한 순간
        AudioManager.Instance.PlayUI(portalInSound);
        // 여기에 포탈에 빨려들어가는 애니메이션  teleportTime동안 실행

        yield return new WaitForSeconds(teleportTime);

        Transform player = PlayerController.Instance.transform;
        player.position = target.ArrivalPosition;

        // 이동 전 속도가 남아 있으면 도착 직후 엉뚱한 방향으로 튄다
        Rigidbody2D body = PlayerController.Instance.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = target.ArrivalPosition;
            body.linearVelocity = Vector2.zero;
        }

        AudioManager.PlaySfx(portalOutSound, target.ArrivalPosition);

        // 맵 반대편으로 날아간 블럭과 경로는 의미가 없으므로 정리한다
        if (MarkingManager.Instance != null)
            MarkingManager.Instance.ResetMarkingState();

        Close();

        // 방 카메라는 플레이어가 새 방 트리거에 들어가면서 다음 물리 스텝에 전환된다.
        // 그때 Cinemachine이 맵을 가로질러 블렌딩하지 않도록 한 프레임 뒤에 끊어준다.
        StartCoroutine(CutCameraNextFrame());
    }

    private System.Collections.IEnumerator CutCameraNextFrame()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        if (Camera.main == null) yield break;

        var brain = Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();
        if (brain != null) brain.ResetState();
    }

    private void UpdateCurrentRoomHighlight()
    {
        if (currentRoomHighlight == null) return;

        bool visible = currentRoom != null;
        currentRoomHighlight.gameObject.SetActive(visible);
        if (!visible) return;

        currentRoomHighlight.anchoredPosition = CoordinateToLocal(currentRoom.Coordinate);
    }

    // 현재 방 셀 안에서 플레이어의 상대 위치를 보간해 표시.
    // 컨테이너 로컬 좌표로 계산하므로 줌/팬의 영향을 자동으로 따라간다.
    private void UpdatePlayerMarker()
    {
        if (playerMarker == null) return;

        bool visible = showPlayerMarker && currentRoom != null && PlayerController.Instance != null;
        playerMarker.gameObject.SetActive(visible);
        if (!visible) return;

        Vector3 playerPos = PlayerController.Instance.transform.position;
        Vector3 roomCenter = currentRoom.transform.position;

        // Room의 Transform은 방의 중앙이므로 좌하단으로 옮겨서 0~1 비율을 낸다
        float u = Mathf.Clamp01((playerPos.x - (roomCenter.x - Room.Width * 0.5f)) / Room.Width);
        float v = Mathf.Clamp01((playerPos.y - (roomCenter.y - Room.Height * 0.5f)) / Room.Height);

        Vector2 cellOrigin = CoordinateToLocal(currentRoom.Coordinate) - cellSize * 0.5f;
        playerMarker.anchoredPosition = cellOrigin + new Vector2(u * cellSize.x, v * cellSize.y);
    }
}
