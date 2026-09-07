using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 버튼 호버 확대 + 클릭 사운드.
// Button의 ColorBlock은 색만 바꿔주므로 크기 변화와 소리는 여기서 따로 처리한다.
[RequireComponent(typeof(Button))]
public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private SoundData clickSound;

    [Header("호버")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.12f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    private Button button;
    private Vector3 originScale;
    private Tween scaleTween;

    private void Awake()
    {
        button = GetComponent<Button>();
        originScale = transform.localScale;
    }

    private void OnEnable()
    {
        button.onClick.AddListener(PlayClickSound);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(PlayClickSound);

        // 확대된 채로 패널이 닫히면 다음에 열 때 커진 상태로 나타난다
        scaleTween?.Kill();
        transform.localScale = originScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;

        ScaleTo(originScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ScaleTo(originScale);
    }

    private void ScaleTo(Vector3 target)
    {
        scaleTween?.Kill();

        // 일시정지 메뉴는 Time.timeScale이 0이라 SetUpdate(true)가 없으면 트윈이 멈춘다
        scaleTween = transform.DOScale(target, scaleDuration)
            .SetEase(scaleEase)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void PlayClickSound()
    {
        AudioManager.PlayUiSfx(clickSound);
    }
}
