using UnityEngine;
using DG.Tweening;

public class Door : MonoBehaviour
{
    [Header("애니메이션")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private Ease animEase = Ease.InOutSine;

    [Header("플레이어 동반 이동")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 topCheckSize = new Vector2(0.1f, 1f); // 세로로 긴 형태 (x, y)
    [SerializeField] private float topCheckOffset = 0.5f;

    [Header("참조 크기 (스프라이트 원본 높이, 유니티 단위)")]
    [SerializeField] private float baseHeight = 1f;

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

        float targetY = open ? 0f : originScale.y;
        prevScaleY = transform.localScale.y;

        scaleTween = transform
            .DOScaleY(targetY, animDuration)
            .SetEase(animEase)
            .OnUpdate(OnScaleUpdate);
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

        Vector2 deltaY = (Vector2)transform.up * deltaHeight; // 로컬 up 방향 기준으로 이동량 적용

        Rigidbody2D playerRb = hit.attachedRigidbody;
        if (playerRb != null)
            playerRb.position += deltaY;
        else
            hit.transform.position += (Vector3)deltaY;
    }

    // 문의 로컬 왼쪽(-right) 방향으로 오프셋된 감지 박스 중심 위치
    private Vector2 GetCheckBoxCenter()
    {
        return (Vector2)transform.position + (Vector2)(-transform.right) * topCheckOffset;
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

        Gizmos.matrix = Matrix4x4.identity; // 다른 기즈모에 영향 안 주도록 리셋
    }
}