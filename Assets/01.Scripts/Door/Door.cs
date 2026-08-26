using UnityEngine;
using DG.Tweening;

public class Door : MonoBehaviour
{
    [Header("애니메이션 속도 (유닛/초)")]
    [SerializeField] private float openSpeed = 3f;   // 열릴 때 초당 몇 유닛 줄어드는지
    [SerializeField] private float closeSpeed = 3f;  // 닫힐 때 초당 몇 유닛 늘어나는지
    [SerializeField] private Ease animEase = Ease.Linear;

    [Header("플레이어 동반 이동")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 topCheckSize = new Vector2(1f, 0.1f);
    [SerializeField] private Vector2 topCheckOffset = new Vector2(0f, 0.5f);

    [Header("참조 크기 (스프라이트 원본 세로 길이, 유니티 단위)")]
    [SerializeField] private float baseHeight = 1f;

    [Header("전부 열렸을 때 남아있는 문의 스케일")]
    [SerializeField] private float openScale = 0.3f;

    [Header("사운드")]
    [Tooltip("문이 움직이는 동안만 재생된다. LoopSound의 playOnEnable은 꺼두어야 한다")]
    [SerializeField] private LoopSound moveSound;

    [Header("기즈모 미리보기")]
    [SerializeField] private bool previewTopCheck = true;

    private Vector3 originScale;
    private Tween scaleTween;
    private float prevScaleY;

    private void Awake()
    {
        originScale = transform.localScale;
    }

    public void SetOpen(bool open)
    {
        scaleTween?.Kill();
        StopMoveSound();        // 이전 이동 소리를 남기지 않는다

        float currentScaleY = transform.localScale.y;
        float targetScaleY = open ? openScale : originScale.y;
        float speed = open ? openSpeed : closeSpeed;

        // 스케일 차이를 실제 월드 유닛 거리로 환산
        float distanceInUnits = Mathf.Abs(targetScaleY - currentScaleY) * baseHeight;

        // 거리 / 속도 = 걸리는 시간
        float duration = speed > 0f ? distanceInUnits / speed : 0f;

        prevScaleY = currentScaleY;

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
            .OnUpdate(OnScaleUpdate)
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

    private void OnScaleUpdate()
    {
        float currentScaleY = transform.localScale.y;
        float deltaScaleY = currentScaleY - prevScaleY;
        float deltaHeight = deltaScaleY * baseHeight;

        CarryPlayerOnTop(deltaHeight);

        prevScaleY = currentScaleY;
    }

    private void CarryPlayerOnTop(float deltaHeight)
    {
        Vector2 boxCenter = GetCheckBoxCenter();

        Collider2D hit = Physics2D.OverlapBox(boxCenter, topCheckSize, transform.eulerAngles.z, playerLayer);
        if (hit == null) return;

        Rigidbody2D playerRb = hit.attachedRigidbody;

        if (playerRb != null)
        {
            // 플레이어가 위로 솟구치는 중(점프)이면 문에 딸려가지 않음
            if (playerRb.linearVelocity.y > 0.01f)
                return;

            Vector2 deltaY = (Vector2)transform.up * -deltaHeight;
            playerRb.position += deltaY;

            // 문이 내려갈 때만 낙하 속도를 죽여서 톡톡거림 방지
            if (deltaHeight < 0f && playerRb.linearVelocity.y < 0f)
            {
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
            }
        }
        else
        {
            Vector2 deltaY = (Vector2)transform.up * -deltaHeight;
            hit.transform.position += (Vector3)deltaY;
        }
    }

    private Vector2 GetCheckBoxCenter()
    {
        // 에디트 모드에서는 originScale이 아직 세팅 안 됐으므로 현재 스케일을 원본으로 간주
        float baseScaleY = Application.isPlaying ? originScale.y : transform.localScale.y;
        float scaleRatio = baseScaleY != 0f ? transform.localScale.y / baseScaleY : 1f;

        Vector2 localOffset = (Vector2)transform.right * topCheckOffset.x
                             + (Vector2)transform.up * (topCheckOffset.y * scaleRatio);

        return (Vector2)transform.position + localOffset;
    }

    private void OnDrawGizmos()
    {
        if (!previewTopCheck) return;

        Vector2 boxCenter = GetCheckBoxCenter();
        float angle = transform.eulerAngles.z;

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, Quaternion.Euler(0, 0, angle), Vector3.one);
        Gizmos.matrix = rotationMatrix;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawCube(Vector3.zero, topCheckSize);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, topCheckSize);

        Gizmos.matrix = Matrix4x4.identity;
    }
}