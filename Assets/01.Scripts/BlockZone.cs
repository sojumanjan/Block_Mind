using UnityEngine;
using UnityEngine.Tilemaps;

// 그림자는 통과할 수 없지만 플레이어는 통과할 수 있는 영역.
// 플레이어 입장에서는 없는 것처럼 동작한다 - 발판으로도 쓰이지 않는다.
//
// 두 가지 방식 모두 지원한다. 붙어 있는 Collider2D의 종류만 다르다.
//   1) 오브젝트 하나 + BoxCollider2D                        - 사각 영역 하나를 빠르게 놓을 때
//   2) 타일맵 + TilemapCollider2D (+ CompositeCollider2D)    - Wire/Normal처럼 타일로 칠할 때
//
// 레이어는 BlockZone(전용)을 전제로 한다. Ground 레이어에 두면
// PlayerController.IsGrounded()가 Ground 마스크로 이 영역을 훑어서
// 플레이어가 영역 안에서 공중 점프를 할 수 있게 된다.
public class BlockZone : MonoBehaviour
{
    [Tooltip("이 레이어에 속한 대상만 통과시킨다")]
    [SerializeField] private string passThroughLayer = "Player";

    [Header("기즈모")]
    [Tooltip("타일맵일 때는 타일 자체가 보이므로 기즈모를 그리지 않는다")]
    [SerializeField] private bool previewZone = true;
    [SerializeField] private Color previewColor = new Color(0.65f, 0.40f, 0.95f, 0.25f);

    private void Awake()
    {
        ApplyLayerOverride();
    }

#if UNITY_EDITOR
    // 에디터에서도 적용해 인스펙터의 Exclude Layers에 값이 보이게 한다.
    // (런타임에만 설정하면 "설정이 비어 있는데 되는 건가?" 하고 헷갈린다)
    private void OnValidate()
    {
        ApplyLayerOverride();
    }
#endif

    private void ApplyLayerOverride()
    {
        // CompositeCollider2D를 쓰면 TilemapCollider2D의 도형이 컴포짓으로 병합되고
        // 실제 충돌은 컴포짓이 담당한다. 첫 번째 콜라이더만 설정하면 엉뚱한 쪽을 건드려
        // 통과 설정이 먹지 않는다. 그래서 붙어 있는 콜라이더 전부에 적용한다.
        Collider2D[] zoneColliders = GetComponents<Collider2D>();
        if (zoneColliders.Length == 0)
        {
            // Collider2D는 추상 타입이라 RequireComponent로 강제할 수 없어 여기서 알린다
            Debug.LogWarning("Collider2D가 없습니다. BoxCollider2D나 TilemapCollider2D를 붙여야 그림자를 막습니다.", this);
            return;
        }

        int layer = LayerMask.NameToLayer(passThroughLayer);
        if (layer < 0)
        {
            Debug.LogWarning("'" + passThroughLayer + "' 레이어가 없습니다. 통과 설정을 적용하지 못했습니다.", this);
            return;
        }

        LayerMask pass = 1 << layer;

        foreach (Collider2D zoneCollider in zoneColliders)
        {
            // 실제로 막아야 하므로 트리거가 아니다
            if (zoneCollider.isTrigger) zoneCollider.isTrigger = false;

            // 충돌 매트릭스를 건드리지 않고 이 콜라이더만 해당 레이어를 무시하게 한다.
            // (매트릭스를 고치면 BlockZone 레이어를 쓰는 다른 오브젝트까지 영향을 받는다)
            if (zoneCollider.excludeLayers != pass) zoneCollider.excludeLayers = pass;
        }
    }

    private void OnDrawGizmos()
    {
        if (!previewZone) return;

        // 타일맵은 bounds가 칠해진 영역 전체를 감싸는 큰 사각형이라
        // 기즈모를 그리면 실제 모양과 전혀 달라 오히려 헷갈린다.
        if (GetComponent<Tilemap>() != null) return;

        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider == null) return;

        Bounds bounds = zoneCollider.bounds;

        Gizmos.color = previewColor;
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = new Color(previewColor.r, previewColor.g, previewColor.b, 1f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
