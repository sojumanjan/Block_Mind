using UnityEngine;
using UnityEngine.InputSystem;

public class FollowingBlock : MonoBehaviour
{
    Rigidbody2D rigid;

    InputActions inputActions;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        Vector3 pos = Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(pos);
        mousePos.z = 0f;
        transform.position = new Vector2(8, 0);
    }

    private void FixedUpdate()
    {
        if (Mouse.current.rightButton.isPressed) return;
        Vector3 pos = Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(pos);
        mousePos.z = 0f;

        Vector2 currentPos = rigid.position;
        Vector2 targetPos = mousePos;

        float maxMoveDistance = 0.4f; // 한 프레임당 최대 이동 거리
        Vector2 clampedPos = Vector2.MoveTowards(currentPos, targetPos, maxMoveDistance);

        rigid.MovePosition(clampedPos);
    }
}
