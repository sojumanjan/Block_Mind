using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

// 미니맵 차원문 아이콘에 마우스를 올렸을 때 살짝 커지는 피드백.
// Button.interactable일 때만 반응하므로, 선택이 가능한 고속이동 모드(F로 연 지도)에서만 커진다.
[RequireComponent(typeof(RectTransform))]
public class MapPortalIconHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private RectTransform rect;
    private Button button;
    private Tween scaleTween;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
    }

    // 커진 상태로 지도가 닫히면 OnPointerExit가 오지 않으므로 여기서 되돌린다
    private void OnDisable()
    {
        scaleTween?.Kill();
        if (rect != null)
            rect.localScale = Vector3.one;   // 비활성화 시엔 즉시 원복
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        AnimateTo(hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(1f);
    }

    private void AnimateTo(float scale)
    {
        if (rect == null) return;

        scaleTween?.Kill();                  // 이전 트윈 중단 (중첩 방지)
        scaleTween = rect.DOScale(scale, duration)
                         .SetEase(ease)
                         .SetUpdate(true);   // Time.timeScale=0 (지도 정지 상태)에서도 동작
    }
}