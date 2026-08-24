using UnityEngine;

public class LaserObstacle : MonoBehaviour
{
    [Header("레이저 설정")]
    [SerializeField] private Transform emitPoint;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private LayerMask hitLayers;

    [Header("렌더링")]
    [SerializeField] private LineRenderer lineRenderer;

    private void Update()
    {
        FireLaser();
    }

    private void FireLaser()
    {
        Vector2 origin, endPoint;
        RaycastHit2D hit;
        if (!TryGetBeam(out origin, out endPoint, out hit)) return;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            PlayerLifeManager.Instance.Die();
        }
    }

    // 광선 구간만 계산해서 돌려준다. 부작용 없음 - 미니맵처럼 밖에서 구간이 필요할 때 쓴다.
    // (LineRenderer는 Update가 한 번 돌기 전에는 값이 비어 있어 신뢰할 수 없다)
    public bool TryGetBeam(out Vector2 origin, out Vector2 endPoint)
    {
        RaycastHit2D ignored;
        return TryGetBeam(out origin, out endPoint, out ignored);
    }

    private bool TryGetBeam(out Vector2 origin, out Vector2 endPoint, out RaycastHit2D hit)
    {
        origin = Vector2.zero;
        endPoint = Vector2.zero;
        hit = default(RaycastHit2D);

        if (emitPoint == null) return false;

        origin = emitPoint.position;
        Vector2 dir = -emitPoint.up;

        hit = Physics2D.Raycast(origin, dir, maxDistance, hitLayers);

        endPoint = hit.collider != null
            ? hit.point
            : origin + dir * maxDistance;

        return true;
    }
}