using UnityEngine;
using DG.Tweening;

public class KeyDoor : MonoBehaviour
{
    [Header("애니메이션 속도 (유닛/초)")]
    [SerializeField] private float openSpeed = 3f;   // 열릴 때 초당 몇 유닛 줄어드는지
    [SerializeField] private float closeSpeed = 3f;  // 닫힐 때 초당 몇 유닛 늘어나는지
    [SerializeField] private Ease animEase = Ease.Linear;

    [Header("참조 크기 (스프라이트 원본 세로 길이, 유니티 단위)")]
    [SerializeField] private float baseHeight = 1f;

    [Header("전부 열렸을 때 남아있는 문의 스케일")]
    [SerializeField] private float openScale = 0.3f;

    [Header("사운드")]
    [Tooltip("문이 움직이는 동안만 재생된다. LoopSound의 playOnEnable은 꺼두어야 한다")]
    [SerializeField] private LoopSound moveSound;

    private Vector3 originScale;
    private Tween scaleTween;

    private void Awake()
    {
        originScale = transform.localScale;
    }

    // Awake 순서가 보장되지 않으므로 모든 Awake가 끝난 Start에서 구독한다
    private void Start()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate += CloseInstantly;
    }

    private void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate -= CloseInstantly;
    }

    // 체크포인트가 바뀌면 애니메이션 없이 닫힌 상태로 되돌린다
    private void CloseInstantly()
    {
        SetOpen(false, true);
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
            transform.localScale = new Vector3(transform.localScale.x, targetScaleY, transform.localScale.z);
            return;
        }

        // 스케일 차이를 실제 월드 유닛 거리로 환산
        float distanceInUnits = Mathf.Abs(targetScaleY - currentScaleY) * baseHeight;

        // 거리 / 속도 = 걸리는 시간
        float duration = speed > 0f ? distanceInUnits / speed : 0f;

        if (duration <= 0f)
        {
            // 이미 목표 상태거나 속도가 0이면 즉시 적용
            transform.localScale = new Vector3(transform.localScale.x, targetScaleY, transform.localScale.z);
            return;
        }

        PlayMoveSound();

        scaleTween = transform
            .DOScaleY(targetScaleY, duration)
            .SetEase(animEase)
            .OnComplete(StopMoveSound);
    }

    private void PlayMoveSound()
    {
        if (moveSound != null) moveSound.Play();
    }

    private void StopMoveSound()
    {
        if (moveSound != null) moveSound.Stop();
    }
}