using DG.Tweening;
using UnityEngine;

// 세로 스케일을 줄여서 열리는 문의 공통 동작.
// Door와 KeyDoor가 거의 같은 SetOpen을 각자 갖고 있어서 연출을 손볼 때 두 곳을 고쳐야 했다.
//
// 스케일이 아니라 위치가 움직이는 장치(MovingDoor)는 여기에 속하지 않는다.
public abstract class ScalingDoor : MovingDevice
{
    [Header("애니메이션 속도 (유닛/초)")]
    [SerializeField] private float openSpeed = 3f;   // 열릴 때 초당 몇 유닛 줄어드는지
    [SerializeField] private float closeSpeed = 3f;  // 닫힐 때 초당 몇 유닛 늘어나는지
    [SerializeField] private Ease animEase = Ease.Linear;

    [Header("참조 크기 (스프라이트 원본 세로 길이, 유니티 단위)")]
    [SerializeField] private float baseHeight = 1f;

    [Header("전부 열렸을 때 남아있는 문의 스케일")]
    [SerializeField] private float openScale = 0.3f;

    private Vector3 originScale;
    private Tween scaleTween;

    protected float BaseHeight => baseHeight;

    protected Vector3 OriginScale => originScale;

    protected virtual void Awake()
    {
        originScale = transform.localScale;
    }

    // ButtonZone 등이 공통 타입으로 부르는 진입점.
    // 문에는 "연다/닫는다"가 자연스러운 표현이라 SetOpen을 그대로 두고 여기서 연결한다.
    public override void SetActivated(bool activated)
    {
        SetOpen(activated);
    }

    public void SetOpen(bool open, bool instant = false)
    {
        scaleTween?.Kill();
        StopMoveSound();        // 이전 이동 소리를 남기지 않는다

        float currentScaleY = transform.localScale.y;
        float targetScaleY = open ? openScale : originScale.y;
        float speed = open ? openSpeed : closeSpeed;

        if (instant)
        {
            ApplyScaleY(targetScaleY);
            return;
        }

        // 스케일 차이를 실제 월드 유닛 거리로 환산한 뒤, 거리 / 속도 = 걸리는 시간
        float distanceInUnits = Mathf.Abs(targetScaleY - currentScaleY) * baseHeight;
        float duration = speed > 0f ? distanceInUnits / speed : 0f;

        OnMoveStarting(currentScaleY);

        if (duration <= 0f)
        {
            // 이미 목표 상태거나 속도가 0이면 즉시 적용
            ApplyScaleY(targetScaleY);
            return;
        }

        PlayMoveSound();

        scaleTween = transform
            .DOScaleY(targetScaleY, duration)
            .SetEase(animEase)
            .OnUpdate(OnMoveUpdate)
            .OnComplete(StopMoveSound);
    }

    // 움직이기 직전에 파생 클래스가 상태를 잡을 기회. (Door는 직전 스케일을 기억해 둔다)
    protected virtual void OnMoveStarting(float currentScaleY) { }

    // 트윈이 도는 동안 매 프레임. (Door는 올라탄 플레이어를 함께 옮긴다)
    protected virtual void OnMoveUpdate() { }

    private void ApplyScaleY(float scaleY)
    {
        transform.localScale = new Vector3(transform.localScale.x, scaleY, transform.localScale.z);
    }
}
