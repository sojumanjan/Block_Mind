using UnityEngine;
using DG.Tweening;

public class Door : MonoBehaviour
{
    [Header("애니메이션")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private Ease animEase = Ease.InOutSine;

    [Header("플레이어 동반 이동")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 topCheckSize = new Vector2(1f, 0.1f);
    [SerializeField] private Vector2 topCheckOffset = new Vector2(0f, 0.5f);

    [Header("참조 크기 (스프라이트 원본 세로 길이, 유니티 단위)")]
    [SerializeField] private float baseHeight = 1f; // 스케일 1일 때의 실제 월드 세로 길이

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

        Vector2 deltaY = (Vector2)transform.up * -deltaHeight; // 문의 로컬 up 방향 기준

        Rigidbody2D playerRb = hit.attachedRigidbody;
        if (playerRb != null)
            playerRb.position += deltaY;
        else
            hit.transform.position += (Vector3)deltaY;
    }

    private Vector2 GetCheckBoxCenter()
    {
        Vector2 localOffset = (Vector2)transform.right * topCheckOffset.x
                             + (Vector2)transform.up * topCheckOffset.y;

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