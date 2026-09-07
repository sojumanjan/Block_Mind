using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// M키로 여는 전체 지도.
// 씬의 Room을 전부 수집해 Coordinate를 UI 격자로 그리고, 각 방의 타일맵을 읽어 구조를 축소 텍스처로 굽는다.
// 방/타일을 추가해도 이 스크립트는 손댈 필요가 없다.
//
// 실제 작업은 세 곳에 나눠져 있다.
//   MinimapBaker      - 타일맵을 읽어 방별 텍스처로 굽고 아틀라스 한 장에 담는다
//   MinimapPanZoom    - 휠 확대/축소, 드래그 이동, 클릭과 드래그 구분
//   MinimapPortalIcons - 고속이동 차원문 아이콘 생성과 표시 상태
//
// 이 클래스는 격자 배치와 좌표 변환, 열기/닫기, 고속이동 실행만 담당한다.
// [SerializeField] 필드는 전부 여기 남아 있다 - 옮기면 직렬화 경로가 바뀌어 인스펙터 값이 날아간다.
public class MapUI : SingletonBehaviour<MapUI>
{
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

    // 일반 보기(M)와 고속이동(차원문에서 F) 두 모드가 있다.
    // 차원문 선택은 Travel 모드에서만 가능하다.
    private enum MapMode { View, Travel }

    private readonly Dictionary<Room, Image> cells = new Dictionary<Room, Image>();

    private InputActions inputActions;
    private MinimapPanZoom panZoom;
    private MinimapPortalIcons portalIcons;

    private Room currentRoom;
    private Vector2 gridCenter;

    private MapMode mode = MapMode.View;
    private Portal originPortal;    // Travel 모드로 열 때 올라가 있던 차원문

    // 지도가 열려 있는 동안에는 다른 마우스 입력을 막아야 한다 (MarkingManager에서 참조)
    public bool IsOpen => panel != null && panel.activeSelf;

    protected override void Awake()
    {
        base.Awake();

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
        RectTransform panelRect = panel != null ? panel.GetComponent<RectTransform>() : null;

        panZoom = new MinimapPanZoom(cellContainer, panelRect, new MinimapPanZoom.Settings
        {
            defaultZoom = defaultZoom,
            minZoom = minZoom,
            maxZoom = maxZoom,
            zoomStep = zoomStep,
            dragThreshold = dragThreshold,
            paddingRooms = paddingRooms,
            clampToBounds = clampToBounds,
            step = cellSize + gap,
        });

        portalIcons = new MinimapPortalIcons(portalIconPrefab, cellContainer, portalIconSize, portalIconColor);

        BuildCells();

        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;

        panZoom.Tick();
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

        Room[] rooms = FindObjectsByType<Room>(FindObjectsInactive.Include);
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
        panZoom.GridExtent = max - min;

        Dictionary<Room, Sprite> structures = new MinimapBaker(BuildPalette()).Bake(rooms);

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

        portalIcons.Build(rooms, WorldToCellLocal, OnPortalClicked);

        // 렌더 순서를 형제 순서로 정한다. 뒤에 있는 형제가 위에 그려진다.
        //   셀 < 현재 방 테두리 < 차원문 아이콘 < 플레이어 마커
        if (currentRoomHighlight != null)
        {
            currentRoomHighlight.sizeDelta = cellSize;
            currentRoomHighlight.SetAsLastSibling();
            currentRoomHighlight.gameObject.SetActive(false);
        }

        portalIcons.BringToFront();

        if (playerMarker != null)
            playerMarker.SetAsLastSibling();
    }

    // 인스펙터에서 맞춘 색들을 굽기용 묶음으로 옮긴다
    private MinimapBaker.Palette BuildPalette()
    {
        return new MinimapBaker.Palette
        {
            pixelsPerTile = pixelsPerTile,
            showObjects = showObjects,

            ground = groundTileColor,
            obstacle = obstacleTileColor,
            passable = passableTileColor,
            wire = wireTileColor,
            blockZone = blockZoneTileColor,

            door = doorColor,
            button = buttonColor,
            hazard = hazardColor,
            checkpoint = checkpointColor,
            item = itemColor,
            generic = genericObjectColor,
        };
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
        Room focus = origin != null && origin.Room != null ? origin.Room : currentRoom;
        panZoom.ResetView(focus == null ? Vector2.zero : CoordinateToLocal(focus.Coordinate));

        Refresh();
    }

    // ESC 메뉴가 "지도가 열려 있으면 지도부터 닫는다"를 하려면 외부에서 부를 수 있어야 한다
    public void Close()
    {
        panel.SetActive(false);

        AudioManager.PlayUiSfx(mapCloseSound);
        mode = MapMode.View;
        originPortal = null;
        panZoom.CancelDrag();
    }

    // 디버그: 모든 방을 방문 처리한다. 되돌리는 기능은 없다(Room.IsVisited는 한 방향).
    private void OnDebugRevealMap(InputAction.CallbackContext context)
    {
        if (!enableDebugReveal) return;

        RevealAllRooms();
    }

    public void RevealAllRooms()
    {
        // cells에 없는 방까지 포함하도록 씬에서 다시 훑는다
        foreach (Room room in FindObjectsByType<Room>(FindObjectsInactive.Include))
            room.MarkVisited();

        // 닫혀 있어도 셀/아이콘 상태를 맞춰둔다. 다음에 열 때 바로 반영된다.
        Refresh();
    }

    // ---------------------------------------------------------------- 고속이동

    // 차원문 아이콘 클릭
    private void OnPortalClicked(Portal target)
    {
        if (panZoom.DragMoved) return;          // 지도를 끌던 손이 떨어진 것은 클릭이 아니다
        if (mode != MapMode.Travel) return;     // 일반 지도(M)에서는 선택 불가
        if (target == null || target == originPortal) return;

        if (PlayerController.Instance == null) return;

        StartCoroutine(TravelTo(target));
    }

    private IEnumerator TravelTo(Portal target)
    {
        // 다른 포탈로 순간이동을 클릭한 순간
        AudioManager.PlayUiSfx(portalInSound);
        // 여기에 포탈에 빨려들어가는 애니메이션 teleportTime동안 실행

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

        AudioManager.PlayUiSfx(portalOutSound);

        // 맵 반대편으로 날아간 블럭과 경로는 의미가 없으므로 정리한다
        if (MarkingManager.Instance != null)
            MarkingManager.Instance.ResetMarkingState();

        Close();

        // 방 카메라는 플레이어가 새 방 트리거에 들어가면서 다음 물리 스텝에 전환된다.
        // 그때 Cinemachine이 맵을 가로질러 블렌딩하지 않도록 한 프레임 뒤에 끊어준다.
        StartCoroutine(CutCameraNextFrame());
    }

    private IEnumerator CutCameraNextFrame()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        if (Camera.main == null) yield break;

        var brain = Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();
        if (brain != null) brain.ResetState();
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

        portalIcons.Refresh(mode == MapMode.Travel, originPortal);
        UpdateCurrentRoomHighlight();
        UpdatePlayerMarker();
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

        playerMarker.anchoredPosition = WorldToCellLocal(currentRoom, PlayerController.Instance.transform.position);
    }
}
