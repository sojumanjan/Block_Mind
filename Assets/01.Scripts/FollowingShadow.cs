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

    private void Start()
    {
        PlayerLifeManager.Instance.OnDie += ResetShadowCurRoom;
    }
    private void OnDestroy()
    {
        PlayerLifeManager.Instance.OnDie -= ResetShadowCurRoom;
    }

    private void FixedUpdate()
    {
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

    // 체크포인트가 속한 방을 찾아 해당 방의 그림자 스폰 위치에 스폰. 플레이어 사망시 호출.
    public void ResetShadowCurRoom()
    {
        Debug.Log("그림자 리셋");
        Room room = CheckpointManager.Instance.GetCheckPoint().GetComponentInParent<Room>();
        transform.position = room.shadowSpawnPoint.position;
    }
}
