using UnityEngine;
using DG.Tweening;

// ButtonZone에 연결되는 이동 발판.
public class MovingDoor : ActivatableDevice
{
    public enum BarAxis { X, Y }

    [Header("이동 대상")]
    [SerializeField] private Transform platform;

    [Header("위치 (비우면 발판의 시작 위치를 startPoint로 쓴다)")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("속도 (유닛/초)")]
    [Tooltip("활성화되어 endPoint로 갈 때의 속도")]
    [SerializeField] private float activateSpeed = 3f;
    [Tooltip("비활성화되어 startPoint로 돌아올 때의 속도")]
    [SerializeField] private float deactivateSpeed = 2f;
    [SerializeField] private Ease moveEase = Ease.Linear;

    [Header("연결 바 (없어도 동작)")]
    [Tooltip("벽에 고정해 둘 바. 스프라이트 피벗이 벽쪽 끝에 있어야 한다. 위치와 회전은 배치한 그대로 두고 스케일만 조절한다")]
    [SerializeField] private Transform barPivot;
    [Tooltip("바 스프라이트의 길이 방향 축")]
    [SerializeField] private BarAxis barAxis = BarAxis.X;

    [Header("플레이어 동반 이동")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 topCheckSize = new Vector2(2f, 0.2f);
    [Tooltip("발판 중심에서 윗면까지의 거리")]
    [SerializeField] private float topCheckOffset = 0.5f;

    [Header("사운드")]
    [Tooltip("발판이 움직이는 동안만 재생된다. LoopSound의 playOnEnable은 꺼두어야 한다")]
    [SerializeField] private LoopSound moveSound;

    [Header("기즈모")]
    [SerializeField] private bool previewPath = true;

    private Vector3 initialPosition;
    private Vector3 previousPosition;
    private Tween moveTween;
    private Vector3 platformStartPosition;
    private Vector3 barBaseScale;

    private float stretchSign = 1f;     // 발판이 피벗의 어느 쪽에 있는지로 자동 결정
    private float barUnitLength = 1f;   // barPivot 스케일 1일 때의 월드 길이. 자식 스프라이트에서 잰다

    private Vector3 StartPosition => startPoint != null ? startPoint.position : initialPosition;
    private Vector3 EndPosition => endPoint != null ? endPoint.position : initialPosition;

    private void Awake()
    {
        if (platform == null)
        {
            Debug.LogWarning("platform이 지정되지 않았습니다. 움직일 대상이 없습니다.", this);
            return;
        }

        initialPosition = platform.position;
        previousPosition = initialPosition;
        platformStartPosition = platform.position;

        if (barPivot != null)
        {
            barBaseScale = barPivot.localScale;
            stretchSign = MeasureStretchSign();
            barUnitLength = MeasureBarUnitLength();
        }

        if (endPoint == null)
            Debug.LogWarning("endPoint가 지정되지 않아 발판이 움직이지 않습니다.", this);
    }

    // ButtonZone에서 호출
    public override void SetActivated(bool activated)
    {
        if (platform == null) return;

        moveTween?.Kill();
        StopMoveSound();        // 이전 이동 소리를 남기지 않는다

        Vector3 target = activated ? EndPosition : StartPosition;
        float speed = activated ? activateSpeed : deactivateSpeed;

        // 남은 거리로 시간을 낸다. 중간에 방향이 바뀌어도 속도가 일정하게 유지된다.
        float distance = Vector3.Distance(platform.position, target);
        float duration = speed > 0f ? distance / speed : 0f;

        if (duration <= 0f)
        {
            platform.position = target;
            previousPosition = target;
            UpdateBar();
            return;
        }

        previousPosition = platform.position;
        PlayMoveSound();

        moveTween = platform
            .DOMove(target, duration)
            .SetEase(moveEase)
            .OnUpdate(OnPlatformMoved)
            .OnComplete(StopMoveSound);
    }

    private void OnPlatformMoved()
    {
        Vector3 delta = platform.position - previousPosition;
        previousPosition = platform.position;

        CarryPlayerOnTop(delta);
        UpdateBar();
    }

    // ---------------------------------------------------------------- 바 늘이기

    // 바는 위치도 회전도 건드리지 않는다. 배치해 둔 스케일에 발판의 이동량만 더한다.
    // 절대 거리로 계산하지 않는 이유: 배치해 둔 값과 계산값이 다르면 시작하는 순간 바가 튄다.
    // 이동량 기준이면 시작 시점(이동량 0)에는 배치한 모습 그대로다.
    private void UpdateBar()
    {
        if (barPivot == null || platform == null) return;

        // 발판이 시작 위치에서 바가 뻗는 방향으로 얼마나 갔는지
        float displacement = Vector3.Dot(platform.position - platformStartPosition, StretchAxis);
        float added = barUnitLength > 0f ? displacement / barUnitLength : displacement;

        float baseValue = barAxis == BarAxis.X ? barBaseScale.x : barBaseScale.y;

        // 배치한 스케일이 음수(스프라이트를 뒤집어 쓴 경우)여도 크기가 커지도록 부호를 맞춘다
        float sign = baseValue < 0f ? -1f : 1f;
        float result = baseValue + added * sign;

        // 0을 넘어 반대로 뒤집히는 것을 막는다
        result = sign > 0f ? Mathf.Max(0f, result) : Mathf.Min(0f, result);

        Vector3 scale = barBaseScale;
        if (barAxis == BarAxis.X) scale.x = result;
        else scale.y = result;
        barPivot.localScale = scale;
    }

    // 피벗의 회전을 반영한 실제 뻗는 방향(월드)
    private Vector3 StretchAxis
    {
        get
        {
            Vector3 axis = barAxis == BarAxis.X ? barPivot.right : barPivot.up;
            return stretchSign < 0f ? -axis : axis;
        }
    }

    // 발판이 피벗의 어느 쪽에 있는지로 자라는 방향을 정한다.
    // 좌표를 직접 비교하지 않고 피벗의 축에 투영하므로 피벗이 회전돼 있어도 맞는다.
    // (회전이 0이면 결국 X 좌표 비교와 같다)
    private float MeasureStretchSign()
    {
        if (platform == null || barPivot == null) return 1f;

        Vector3 axis = barAxis == BarAxis.X ? barPivot.right : barPivot.up;
        return Vector3.Dot(platform.position - barPivot.position, axis) < 0f ? -1f : 1f;
    }

    // barPivot 스케일이 1일 때 바가 몇 유닛인지를 자식 스프라이트에서 잰다.
    // 스프라이트 원본 크기 x 자식의 스케일이라 회전과 무관하다.
    private float MeasureBarUnitLength()
    {
        if (barPivot == null) return 1f;

        SpriteRenderer renderer = barPivot.GetComponentInChildren<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null) return 1f;

        Vector3 spriteSize = renderer.sprite.bounds.size;
        Vector3 childScale = renderer.transform.localScale;

        // 스프라이트가 barPivot 자신에게 붙어 있으면 그 스케일은 우리가 바꾸는 값이므로 빼고 잰다
        bool onPivot = renderer.transform == barPivot;

        float length = barAxis == BarAxis.X
            ? spriteSize.x * (onPivot ? 1f : Mathf.Abs(childScale.x))
            : spriteSize.y * (onPivot ? 1f : Mathf.Abs(childScale.y));

        return length > 0.0001f ? length : 1f;
    }

    // ---------------------------------------------------------------- 플레이어 동반 이동

    // 발판 윗면에 올라탄 대상을 이동량만큼 같이 옮긴다.
    // Door와 달리 가로/세로 양쪽으로 움직일 수 있어 delta 전체를 쓴다.
    private void CarryPlayerOnTop(Vector3 delta)
    {
        Vector2 boxCenter = (Vector2)platform.position + Vector2.up * topCheckOffset;

        Collider2D hit = Physics2D.OverlapBox(boxCenter, topCheckSize, 0f, playerLayer);
        if (hit == null) return;

        Rigidbody2D body = hit.attachedRigidbody;
        if (body == null)
        {
            hit.transform.position += delta;
            return;
        }

        Vector2 carry = delta;

        // 위로 솟구치는 중(점프)이면 세로로는 딸려가지 않는다. 가로는 그대로 따라간다.
        if (body.linearVelocity.y > 0.01f)
            carry.y = 0f;

        body.position += carry;

        // 발판이 내려갈 때만 낙하 속도를 죽여서 톡톡거림 방지
        if (delta.y < 0f && body.linearVelocity.y < 0f)
            body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
    }

    // ---------------------------------------------------------------- 사운드

    private void PlayMoveSound()
    {
        if (moveSound != null) moveSound.Play();
    }

    private void StopMoveSound()
    {
        if (moveSound != null) moveSound.Stop();
    }

    // ---------------------------------------------------------------- 기즈모

    private void OnDrawGizmos()
    {
        if (!previewPath) return;

        // 에디트 모드에서는 initialPosition이 아직 없으므로 현재 위치를 쓴다
        Vector3 start = startPoint != null ? startPoint.position
                      : platform != null ? platform.position : transform.position;
        Vector3 end = endPoint != null ? endPoint.position : start;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.2f);
        Gizmos.DrawWireSphere(end, 0.2f);

        // 바가 뻗는 방향 표시 (방향이 반대로 보일 때 확인용)
        if (barPivot != null)
        {
            // 에디트 모드에서는 Awake가 안 돌아 캐시가 비어 있으므로 즉석에서 잰다
            Vector3 barAxisDir = barAxis == BarAxis.X ? barPivot.right : barPivot.up;
            if (MeasureStretchSign() < 0f) barAxisDir = -barAxisDir;

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(barPivot.position, barAxisDir * 2f);
            Gizmos.DrawWireSphere(barPivot.position, 0.15f);
        }

        if (platform == null) return;

        // 플레이어 감지 박스
        Vector2 boxCenter = (Vector2)platform.position + Vector2.up * topCheckOffset;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawCube(boxCenter, topCheckSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boxCenter, topCheckSize);
    }
}
