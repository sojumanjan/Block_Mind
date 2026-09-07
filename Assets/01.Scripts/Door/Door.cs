using UnityEngine;

// 버튼으로 여닫는 문. 열리고 닫히는 연출은 ScalingDoor가 담당한다.
// 이 클래스는 문 위에 올라탄 플레이어를 함께 옮기는 부분만 갖는다.
public class Door : ScalingDoor
{
    [Header("플레이어 동반 이동")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 topCheckSize = new Vector2(1f, 0.1f);
    [SerializeField] private Vector2 topCheckOffset = new Vector2(0f, 0.5f);

    [Header("기즈모 미리보기")]
    [SerializeField] private bool previewTopCheck = true;

    private float prevScaleY;

    // 스케일이 얼마나 변했는지 알려면 직전 값을 들고 있어야 한다
    protected override void OnMoveStarting(float currentScaleY)
    {
        prevScaleY = currentScaleY;
    }

    protected override void OnMoveUpdate()
    {
        float currentScaleY = transform.localScale.y;
        float deltaHeight = (currentScaleY - prevScaleY) * BaseHeight;

        CarryPlayerOnTop(deltaHeight);

        prevScaleY = currentScaleY;
    }

    private void CarryPlayerOnTop(float deltaHeight)
    {
        Collider2D rider = RiderCarry.FindRider(GetCheckBoxCenter(), topCheckSize, transform.eulerAngles.z, playerLayer);
        if (rider == null) return;

        // 플레이어가 위로 솟구치는 중(점프)이면 문에 딸려가지 않음
        if (RiderCarry.IsRising(rider)) return;

        // 문은 자기 up 방향으로만 늘어나고 줄어든다
        RiderCarry.Move(rider, (Vector2)transform.up * -deltaHeight);

        // 문이 내려갈 때만 낙하 속도를 죽여서 톡톡거림 방지
        if (deltaHeight < 0f) RiderCarry.DampenFall(rider);
    }

    private Vector2 GetCheckBoxCenter()
    {
        // 에디트 모드에서는 OriginScale이 아직 세팅 안 됐으므로 현재 스케일을 원본으로 간주
        float baseScaleY = Application.isPlaying ? OriginScale.y : transform.localScale.y;
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
