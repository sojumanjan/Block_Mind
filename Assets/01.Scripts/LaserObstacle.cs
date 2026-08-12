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
        Vector2 origin = emitPoint.position;
        Vector2 dir = -emitPoint.up;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDistance, hitLayers);

        Vector2 endPoint = hit.collider != null
            ? hit.point
            : origin + dir * maxDistance;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            PlayerLifeManager.Instance.Die();
        }
    }
}