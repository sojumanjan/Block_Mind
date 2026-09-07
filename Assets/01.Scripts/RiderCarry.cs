using UnityEngine;

// 문/발판/블럭 위에 올라탄 대상을 함께 옮기는 공통 처리.
//
// Door, MovingDoor, MarkingManager가 각자 같은 코드를 갖고 있었다.
// 다만 "언제 따라가게 할지"는 셋이 다르다 -
//   문   : 점프 중이면 아예 따라가지 않는다 (회전한 문은 가로 성분도 생기므로)
//   발판 : 점프 중이면 세로만 빼고 가로는 따라간다
//   블럭 : 가로로만 움직이므로 점프를 볼 필요가 없다
// 그 판단은 각 호출부에 남기고, 여기서는 기계적인 부분만 담당한다.
public static class RiderCarry
{
    // 이보다 빠르게 위로 움직이면 점프 중으로 본다
    private const float RisingVelocity = 0.01f;

    // 윗면 감지 박스에 걸린 대상. 없으면 null.
    public static Collider2D FindRider(Vector2 boxCenter, Vector2 boxSize, float boxAngle, LayerMask riderLayer)
    {
        return Physics2D.OverlapBox(boxCenter, boxSize, boxAngle, riderLayer);
    }

    // 스스로 위로 솟구치는 중인가. Rigidbody2D가 없으면 판단할 근거가 없으므로 false.
    public static bool IsRising(Collider2D rider)
    {
        Rigidbody2D body = rider != null ? rider.attachedRigidbody : null;

        return body != null && body.linearVelocity.y > RisingVelocity;
    }

    // Rigidbody2D가 있으면 position을 직접 밀고, 없으면 Transform을 옮긴다.
    // velocity를 건드리지 않는 이유는 이건 "실려서 옮겨지는" 이동이지 대상이 스스로 내는 힘이 아니기 때문이다.
    public static void Move(Collider2D rider, Vector2 carry)
    {
        if (rider == null) return;

        Rigidbody2D body = rider.attachedRigidbody;

        if (body != null) body.position += carry;
        else rider.transform.position += (Vector3)carry;
    }

    // 바닥이 내려갈 때 대상이 함께 내려오면서 톡톡거리는 것을 막는다.
    // 이미 떨어지고 있는 경우에만 낙하 속도를 지운다.
    public static void DampenFall(Collider2D rider)
    {
        Rigidbody2D body = rider != null ? rider.attachedRigidbody : null;
        if (body == null || body.linearVelocity.y >= 0f) return;

        body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
    }
}
