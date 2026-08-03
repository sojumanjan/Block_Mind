using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rigid;

    InputActions inputActions;

    public float moveSpeed = 5f;
    public float jumpPower = 10f;
    private Vector2 moveDir;
    private float jumpRecogTime = 0.2f;
    private float elaspedTime = 0f;
    public float maxYVelocity = 5f;

    private void Awake()
    {
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

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        moveDir.x = inputActions.Player.Move.ReadValue<float>();

        if (elaspedTime >= 0) elaspedTime -= Time.deltaTime;
        if (inputActions.Player.Jump.WasPressedThisFrame())
        {
            elaspedTime = jumpRecogTime;
        }
    }

    private void FixedUpdate()
    {
        rigid.linearVelocity = new Vector2(moveDir.x * moveSpeed, rigid.linearVelocity.y);

        if (elaspedTime >= 0 && IsGrounded())
        {
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }

        if (rigid.linearVelocityY >= maxYVelocity) rigid.linearVelocityY = maxYVelocity;
        if (rigid.linearVelocityY <= -maxYVelocity) rigid.linearVelocityY = -maxYVelocity;
    }
    bool IsGrounded()
    {
        return Physics2D.OverlapBox(transform.position + Vector3.down * 0.5f, new Vector2(1.2f, 0.3f),0f, LayerMask.GetMask("Ground"))
            || Physics2D.OverlapBox(transform.position + Vector3.down * 0.5f, new Vector2(1.2f, 0.3f), 0f, LayerMask.GetMask("FollowingBlock"));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position + Vector3.down * 0.5f, new Vector2(1.2f, 0.2f));
    }
}
