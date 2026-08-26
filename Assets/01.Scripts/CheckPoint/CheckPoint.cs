using Unity.VisualScripting;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;
    [Header("사운드")]
    [SerializeField] private SoundData checkPointActivateSound;

    private void Awake()
    {
        if (sr != null && inactiveSprite != null)
            sr.sprite = inactiveSprite;
    }

    // 플레이어가 체크포인트에 닿는다면
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && (!other.CompareTag("Block"))) return;

        // 활성화 여부 판정과 방송은 매니저에게 위임한다.
        // 리셋 대상(열쇠/마킹/문)은 각자 CheckPointActivate를 구독해서 스스로 처리한다.
        AudioManager.Instance.PlayUI(checkPointActivateSound);
        CheckpointManager.Instance.ActivateCheckpoint(this, transform.position, other);
    }

    // 매니저가 상태를 바꿔줄 때 호출 (활성/비활성 스프라이트 갱신용)
    public void SetVisualState(bool isActive)
    {
        if (sr == null) return;
        sr.sprite = isActive ? activeSprite : inactiveSprite;
    }
}