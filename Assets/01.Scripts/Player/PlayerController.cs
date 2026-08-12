using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rigid;

    InputActions inputActions;

    public float moveSpeed = 5f;
    public float jumpPower = 10f;
    [SerializeField] private float maxFallSpeed = 15f; // 최대 낙하 속도 (인스펙터에서 조절)
    private Vector2 moveDir;
    private float jumpRecogTime = 0.2f;
    private float elaspedTime = 0f;
    [Tooltip("블럭 위에 플레이어가 서있다고 판정하는 범위(블럭 위에 서있다면 함께 이동하기 위한 설정)")]
    [SerializeField] private Vector2 jumpBoxSize = new Vector2(1.5f, 0.2f);
    [SerializeField] private float jumpBoxYOffset = 0.9f;

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
        // 플레이어 좌우 방향 바라보도록
        if (moveDir.x > 0)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        else if (moveDir.x < 0)
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    private void FixedUpdate()
    {
        rigid.linearVelocity = new Vector2(moveDir.x * moveSpeed, rigid.linearVelocity.y);

        if (elaspedTime >= 0 && IsGrounded())
        {
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            elaspedTime = -1f;
        }

        ClampFallSpeed();
    }

    // 낙하 속도가 maxFallSpeed를 넘지 않도록 제한
    private void ClampFallSpeed()
    {
        if (rigid.linearVelocity.y < -maxFallSpeed)
        {
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, -maxFallSpeed);
        }
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapBox(transform.position + Vector3.down * jumpBoxYOffset, jumpBoxSize, 0f, LayerMask.GetMask("Ground"))
            || Physics2D.OverlapBox(transform.position + Vector3.down * jumpBoxYOffset, jumpBoxSize, 0f, LayerMask.GetMask("Block"));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position + Vector3.down * jumpBoxYOffset, jumpBoxSize);
    }
}