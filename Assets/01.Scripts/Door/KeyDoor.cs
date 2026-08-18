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

    private Vector3 originScale;
    private Tween scaleTween;

    private void Awake()
    {
        originScale = transform.localScale;
    }

    public void SetOpen(bool open)
    {
        scaleTween?.Kill();

        float currentScaleY = transform.localScale.y;
        float targetScaleY = open ? openScale : originScale.y;
        float speed = open ? openSpeed : closeSpeed;

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

        scaleTween = transform
            .DOScaleY(targetScaleY, duration)
            .SetEase(animEase);
    }
}