using UnityEngine;
using UnityEngine.InputSystem;

public class FollowingBlock : MonoBehaviour
{
    Rigidbody2D rigid;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Vector3 pos = Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(pos);
        mousePos.z = 0f;
        transform.position = mousePos;
    }

    private void FixedUpdate()
    {
        Vector3 pos = Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(pos);
        mousePos.z = 0f;

        Vector2 currentPos = rigid.position;
        Vector2 targetPos = mousePos;

        float maxMoveDistance = 0.5f; // 한 프레임당 최대 이동 거리
        Vector2 clampedPos = Vector2.MoveTowards(currentPos, targetPos, maxMoveDistance);

        rigid.MovePosition(clampedPos);
    }
}
