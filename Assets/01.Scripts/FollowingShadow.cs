using UnityEngine;
using UnityEngine.InputSystem;

public class FollowingShadow : MonoBehaviour
{
    public static FollowingShadow Instance;

    Rigidbody2D rigid;

    InputActions inputActions;

    public bool isFollowing = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
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

    private void FixedUpdate()
    {
        if (Mouse.current.rightButton.isPressed) return;

        Vector3 pos = Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(pos);
        mousePos.z = 0f;

        rigid.MovePosition(mousePos);
    }

    public void MoveShadowToNextRoom(Transform target)
    {
        transform.position = target.position;
    }

    public void SetFollowing(bool following)
    {
        isFollowing = following;
    }
}
