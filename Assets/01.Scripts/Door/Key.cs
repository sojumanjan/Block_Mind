using UnityEngine;
using DG.Tweening;

public class Key : MonoBehaviour
{
    [Header("따라다니기")]
    [SerializeField] private float followRangeX = 0.6f;   // 이 거리 안이면 X는 안 따라감
    [SerializeField] private float followHeight = 1.2f;   // 플레이어 기준 높이
    [SerializeField] private float smoothTimeX = 0.25f;   // X 추적 부드러움 (크면 더 늘어짐)
    [SerializeField] private float smoothTimeY = 0.12f;   // Y 추적 부드러움

    [Header("둥둥 뜨는 효과")]
    [SerializeField] private float bobAmount = 0.15f;
    [SerializeField] private float bobDuration = 1f;

    [Header("획득 연출")]
    [SerializeField] private float punchScale = 0.4f;
    [SerializeField] private float punchDuration = 0.3f;

    [Header("소모 연출")]
    [SerializeField] private float consumeDuration = 0.25f;
    [SerializeField] private Ease consumeEase = Ease.InBack;

    private Transform followTarget;
    private float velocityX;
    private float velocityY;
    private float bobOffset;
    private Vector2 originPos;
    private Vector3 originScale;

    private void Start()
    {
        DOTween.To(() => bobOffset, v => bobOffset = v, bobAmount, bobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
        originPos = transform.position;
        originScale = transform.localScale;
        CheckpointManager.Instance.CheckPointActivate += ResetLocation;
    }

    private void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate -= ResetLocation;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (followTarget != null) return;

        if (other.CompareTag("Player"))
        {
            PlayerKeyHolder holder = other.GetComponent<PlayerKeyHolder>();
            if (holder == null || holder.HasKey) return;

            holder.AddKey(this);

            followTarget = other.transform;
            GetComponent<Collider2D>().enabled = false;

            transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.5f)
                .SetLink(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        Vector3 pos = transform.position;

        // X: 데드존 밖으로 벗어난 만큼만 따라감
        float diffX = followTarget.position.x - pos.x;
        float targetX = pos.x;

        if (Mathf.Abs(diffX) > followRangeX)
        {
            // 진행 방향 뒤쪽 경계선을 목표로 삼아 끌려오듯 따라감
            targetX = followTarget.position.x - Mathf.Sign(diffX) * followRangeX;
        }

        pos.x = Mathf.SmoothDamp(pos.x, targetX, ref velocityX, smoothTimeX);

        // Y: 항상 머리 위 따라감
        float targetY = followTarget.position.y + followHeight + bobOffset;
        pos.y = Mathf.SmoothDamp(pos.y, targetY, ref velocityY, smoothTimeY);

        pos.z = 0f;
        transform.position = pos;
    }

    // 문을 열 때 PlayerKeyHolder가 호출
    public void Consume()
    {
        followTarget = null;

        transform.DOScale(Vector3.zero, consumeDuration)
            .SetEase(consumeEase)
            .SetLink(gameObject);
    }

    // 체크포인트를 밟았을 때 PlayerKeyHolder가 호출.
    // 들고 있던 열쇠가 눈앞에서 뚝 사라지지 않도록 Consume과 같은 연출로 줄어든 뒤
    // 원래 자리에 되돌려 놓는다. Consume과 달리 파괴하지 않으므로 스케일 복구가 필요하다.
    public void ResetLocation()
    {
        // 플레이어를 따라다니지 않으며 소모되거나 원래위치의 얘들은 바로 스케일과 포지션을 원위치.
        if (followTarget == null)
        {
            RestoreToOrigin();
            return;
        }

        followTarget = null;

        // 획득 펀치가 아직 돌고 있으면 스케일 트윈이 서로 덮어써서 크기가 튄다
        transform.DOKill();
        transform.DOScale(Vector3.zero, consumeDuration)
            .SetEase(consumeEase)
            .SetLink(gameObject)
            .OnComplete(RestoreToOrigin);
    }

    private void RestoreToOrigin()
    {
        transform.position = originPos;
        transform.localScale = originScale;
        GetComponent<Collider2D>().enabled = true;
    }
}