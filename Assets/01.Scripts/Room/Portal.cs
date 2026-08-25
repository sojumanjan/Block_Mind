using UnityEngine;

// 고속이동 차원문. 플레이어가 접촉한 상태에서 상호작용키(F)를 누르면
// MapUI가 고속이동 모드로 지도를 열고, 다른 차원문을 선택하면 그곳으로 순간이동한다.
//
// 프리팹 요구사항: isTrigger인 Collider2D가 있어야 접촉을 감지한다.
[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    // 플레이어가 현재 접촉 중인 차원문. 없으면 null.
    public static Portal Current { get; private set; }

    [Tooltip("순간이동 도착 지점. 비우면 이 오브젝트의 위치를 쓴다.")]
    [SerializeField] private Transform arrivalPoint;

    [Tooltip("미니맵에 쓸 아이콘. 비우면 이 차원문의 SpriteRenderer 스프라이트를 그대로 쓴다.")]
    [SerializeField] private Sprite mapIcon;

    public Room Room { get; private set; }

    public Vector3 ArrivalPosition => arrivalPoint != null ? arrivalPoint.position : transform.position;

    // 미니맵에 아이콘을 놓을 기준점. 스프라이트의 "발밑"(가로 중앙 + 아래쪽 끝)이다.
    // 중심을 기준으로 잡으면 아이콘을 실제 크기보다 크게 그릴 때 아래쪽이 바닥에 파묻힌다.
    // 발밑을 기준으로 두면 키워도 위로만 자란다.
    public Vector3 MapFootPosition
    {
        get
        {
            SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null) return transform.position;

            Bounds bounds = renderer.bounds;
            return new Vector3(bounds.center.x, bounds.min.y, 0f);
        }
    }

    // 미니맵 아이콘용 스프라이트. 지정한 것이 없으면 인게임 스프라이트로 대체한다.
    public Sprite MapIcon
    {
        get
        {
            if (mapIcon != null) return mapIcon;

            SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }
    }

    private void Awake()
    {
        Room = GetComponentInParent<Room>();

        if (Room == null)
            Debug.LogWarning("Portal이 Room 밖에 있습니다. 지도에 표시되지 않습니다.", this);
    }

    private void OnDisable()
    {
        if (Current == this) Current = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Current = this;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (Current == this) Current = null;
    }
}
