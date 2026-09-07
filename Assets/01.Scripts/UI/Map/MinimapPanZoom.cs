using UnityEngine;
using UnityEngine.InputSystem;

// 지도의 확대/축소와 드래그 이동. MapUI에서 분리한 부분.
//
// 클릭과 드래그를 구분하는 책임도 여기 있다 - 차원문 아이콘 클릭이 지도를 끄는 손짓과 섞이면 안 된다.
public class MinimapPanZoom
{
    public class Settings
    {
        public float defaultZoom = 1f;
        public float minZoom = 0.5f;
        public float maxZoom = 5f;
        public float zoomStep = 0.15f;

        // 이 픽셀 이상 끌면 클릭이 아니라 드래그로 본다
        public float dragThreshold = 6f;

        public int paddingRooms = 2;
        public bool clampToBounds = true;

        // 셀 한 칸이 차지하는 간격 (cellSize + gap)
        public Vector2 step = Vector2.one;
    }

    private readonly RectTransform container;
    private readonly RectTransform panelRect;
    private readonly Settings settings;

    private float zoom = 1f;
    private bool isDragging;
    private bool dragMoved;         // 이번 누름이 임계값을 넘어 드래그가 되었나 (클릭과 구분)
    private Vector2 dragStartLocal;
    private Vector2 dragStartAnchored;

    // 격자의 가로/세로 칸 수 - 1. 셀을 다 만든 뒤에 정해지므로 나중에 넣는다.
    public Vector2 GridExtent { get; set; }

    // 지도를 끌던 손이 떨어진 것은 클릭이 아니다. 아이콘 쪽에서 이 값을 본다.
    public bool DragMoved => dragMoved;

    public MinimapPanZoom(RectTransform container, RectTransform panelRect, Settings settings)
    {
        this.container = container;
        this.panelRect = panelRect;
        this.settings = settings;
    }

    // 열 때마다 기본 배율로 돌리고 지정한 지점을 화면 정가운데에 둔다.
    // focusLocal은 zoom을 곱하지 않은 컨테이너 로컬 좌표다.
    public void ResetView(Vector2 focusLocal)
    {
        zoom = Mathf.Clamp(settings.defaultZoom, settings.minZoom, settings.maxZoom);
        ApplyZoom();

        if (container != null)
            container.anchoredPosition = -focusLocal * zoom;

        ClampPosition();
        CancelDrag();
    }

    public void CancelDrag()
    {
        isDragging = false;
        dragMoved = false;
    }

    // 지도가 열려 있는 동안 매 프레임
    public void Tick()
    {
        HandleZoom();
        HandleDrag();
    }

    private void HandleZoom()
    {
        if (container == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        float prevZoom = zoom;

        // 휠 한 칸이 보통 120. 트랙패드의 작은 값도 비례해서 반영되도록 나눈다.
        float notches = scroll / 120f;
        zoom = Mathf.Clamp(zoom * Mathf.Pow(1f + settings.zoomStep, notches), settings.minZoom, settings.maxZoom);

        if (Mathf.Approximately(zoom, prevZoom)) return;

        // 커서 아래의 지점을 제자리에 고정한 채 확대/축소
        Vector2 cursorLocal;
        if (TryGetPanelLocalPoint(Mouse.current.position.ReadValue(), out cursorLocal))
        {
            Vector2 anchored = container.anchoredPosition;
            container.anchoredPosition = cursorLocal - (cursorLocal - anchored) * (zoom / prevZoom);
        }

        ApplyZoom();
        ClampPosition();
    }

    private void ApplyZoom()
    {
        if (container != null)
            container.localScale = new Vector3(zoom, zoom, 1f);
    }

    private void HandleDrag()
    {
        if (container == null) return;

        Mouse mouse = Mouse.current;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (TryGetPanelLocalPoint(mouse.position.ReadValue(), out dragStartLocal))
            {
                isDragging = true;
                dragMoved = false;      // 누른 순간에는 아직 클릭 후보
                dragStartAnchored = container.anchoredPosition;
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
        if (!dragMoved && delta.magnitude > settings.dragThreshold)
            dragMoved = true;

        if (!dragMoved) return;         // 아직 클릭일 수 있으니 지도를 움직이지 않는다

        container.anchoredPosition = dragStartAnchored + delta;
        ClampPosition();
    }

    // 어떤 방이든 화면 정가운데로 가져올 수 있고, 그 바깥으로 paddingRooms 칸만큼 더 여유를 준다.
    // (화면보다 작을 때 중앙 고정하는 방식으로는 특정 방을 가운데 놓을 수 없다)
    private void ClampPosition()
    {
        if (!settings.clampToBounds || container == null) return;

        Vector2 half = GridExtent * 0.5f + Vector2.one * settings.paddingRooms;
        Vector2 limit = half * settings.step * zoom;

        Vector2 pos = container.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -limit.x, limit.x);
        pos.y = Mathf.Clamp(pos.y, -limit.y, limit.y);
        container.anchoredPosition = pos;
    }

    // Screen Space - Overlay 캔버스이므로 camera는 null을 넘긴다.
    private bool TryGetPanelLocalPoint(Vector2 screenPosition, out Vector2 local)
    {
        local = Vector2.zero;
        if (panelRect == null) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect, screenPosition, null, out local);
    }
}
