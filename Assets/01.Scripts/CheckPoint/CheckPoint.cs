using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;
    [Header("사운드")]
    [SerializeField] private SoundData checkPointActivateSound;

    [Header("활성화 연출")]
    [Tooltip("뽀잉 하는 정도. 0.25면 원래 크기의 25%까지 튀어오른다")]
    [SerializeField] private float punchScale = 0.25f;
    [SerializeField] private float punchDuration = 0.35f;
    [Tooltip("튀는 횟수. 1이면 한 번 뽀잉, 2 이상이면 여러 번 흔들린다")]
    [SerializeField] private int punchVibrato = 1;

    private Transform punchTarget;
    private Vector3 originScale;
    private Tween punchTween;

    private void Awake()
    {
        // 스프라이트가 자식에 있으면 그쪽을 흔든다. 같은 오브젝트면 자기 자신이 된다
        punchTarget = sr != null ? sr.transform : transform;
        originScale = punchTarget.localScale;

        if (sr != null && inactiveSprite != null)
            sr.sprite = inactiveSprite;
    }

    // 플레이어가 체크포인트에 닿는다면
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && (!other.CompareTag("Block"))) return;

        // 활성화 여부 판정과 방송은 매니저에게 위임한다.
        // 리셋 대상(열쇠/마킹/문)은 각자 CheckPointActivate를 구독해서 스스로 처리한다.
        AudioManager.PlayUiSfx(checkPointActivateSound);
        PlayPunch();

        CheckpointManager.Instance.ActivateCheckpoint(this, transform.position, other);
    }

    // 살짝 뽀잉 하는 피드백
    private void PlayPunch()
    {
        if (punchTarget == null) return;

        // 이전 트윈이 중간에 끊겼으면 스케일이 어중간하게 남아 있다. 원래 크기로 맞춘 뒤 시작
        punchTween?.Kill();
        punchTarget.localScale = originScale;

        punchTween = punchTarget
            .DOPunchScale(Vector3.one * punchScale, punchDuration, punchVibrato, 0.5f)
            .SetLink(gameObject);
    }

    // 매니저가 상태를 바꿔줄 때 호출 (활성/비활성 스프라이트 갱신용)
    public void SetVisualState(bool isActive)
    {
        if (sr == null) return;
        sr.sprite = isActive ? activeSprite : inactiveSprite;
    }
}