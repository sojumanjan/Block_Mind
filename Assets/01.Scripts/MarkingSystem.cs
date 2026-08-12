using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MarkingSystem : MonoBehaviour
{
    [Header("마킹 설정")]
    [SerializeField] private int maxMarkingCount = 5;      // 최대 마킹 가능 횟수 (인스펙터 조절)
    [SerializeField] private Transform markSourceTransform; // 마킹 위치의 기준이 되는 오브젝트
    [SerializeField] private GameObject markerPrefab;       // 마킹 위치 표시용 (선택, 없어도 동작)

    [Header("블럭 설정")]
    [SerializeField] private GameObject blockPrefab;        // 이동할 블럭 프리팹
    [SerializeField] private float moveSpeed = 5f;          // 블럭 이동 속도
    [SerializeField] private float waitTime = 1f;           // 각 지점에서 머무는 시간

    [Header("플레이어 동반 이동 설정")]
    [SerializeField] private LayerMask playerLayer;         // 플레이어 레이어
    [SerializeField] private Vector2 blockTopSize = new Vector2(1f, 0.1f); // 블럭 윗면 감지 박스 크기
    [SerializeField] private float topCheckOffset = 0.5f;   // 블럭 중심에서 윗면까지 거리

    private readonly List<Vector3> markPositions = new List<Vector3>();
    private readonly List<GameObject> markerObjects = new List<GameObject>();

    private InputActions inputActions;
    private bool isMoving = false;

    private void Awake()
    {
        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Block.StartMoving.performed += OnStartMoving;
    }

    private void OnDisable()
    {
        inputActions.Block.StartMoving.performed -= OnStartMoving;
        inputActions.Disable();
    }

    private void Update()
    {
        HandleMarkingInput();
    }

    // 좌클릭 시 그림자 마킹을 남김.
    private void HandleMarkingInput()
    {
        if (isMoving) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (markPositions.Count >= maxMarkingCount) return;
            if (markSourceTransform == null)
            {
                Debug.LogWarning("markSourceTransform이 지정되지 않았습니다.");
                return;
            }

            Vector3 pos = markSourceTransform.position; // 마우스가 아닌 지정 오브젝트 위치
            pos.z = 0f;
            AddMark(pos);
        }
    }

    private void AddMark(Vector3 pos)
    {
        markPositions.Add(pos);

        if (markerPrefab != null)
        {
            GameObject marker = Instantiate(markerPrefab, pos, Quaternion.identity);
            markerObjects.Add(marker);
        }
    }

    private void OnStartMoving(InputAction.CallbackContext context)
    {
        if (markPositions.Count < 1) return;
        if (isMoving) return;

        StartCoroutine(MoveBlockAlongPath());
    }

    private IEnumerator MoveBlockAlongPath()
    {
        isMoving = true;

        GameObject block = Instantiate(blockPrefab, markPositions[0], Quaternion.identity);

        yield return new WaitForSeconds(waitTime);

        for (int i = 1; i < markPositions.Count; i++)
        {
            Vector3 target = markPositions[i];

            while (block != null && Vector3.Distance(block.transform.position, target) > 0.001f)
            {
                Vector3 prevPos = block.transform.position;

                Vector3 newPos = Vector3.MoveTowards(prevPos, target, moveSpeed * Time.deltaTime);
                Vector3 delta = newPos - prevPos; // 이번 프레임 이동량

                // 블럭 위 플레이어 감지 → X축 방향만 함께 이동
                CarryPlayerOnTop(block.transform.position, delta);

                block.transform.position = newPos;
                yield return null;
            }

            if (block == null) break;

            yield return new WaitForSeconds(waitTime);
        }

        if (block != null)
            Destroy(block);

        ClearMarks();
        isMoving = false;
    }

    // 블럭 윗면에 올라탄 플레이어를 delta의 X축 성분만큼만 함께 이동
    // 끼임(충돌) 판정은 여기서 하지 않음 - 플레이어 쪽 CrushDetector(트리거)가 별도로 처리
    private void CarryPlayerOnTop(Vector3 blockPos, Vector3 delta)
    {
        Vector2 boxCenter = new Vector2(blockPos.x, blockPos.y + topCheckOffset);

        Collider2D hit = Physics2D.OverlapBox(
            boxCenter,
            blockTopSize,
            0f,
            playerLayer);

        if (hit == null) return;

        // X축 이동량만 사용 (Y축은 블럭 위에 서 있으므로 콜라이더가 알아서 처리)
        Vector2 deltaX = new Vector2(delta.x, 0f);

        Rigidbody2D playerRb = hit.attachedRigidbody;
        if (playerRb != null)
            playerRb.position += deltaX;
        else
            hit.transform.position += (Vector3)deltaX;
    }

    private void ClearMarks()
    {
        markPositions.Clear();
        foreach (var m in markerObjects)
            if (m != null) Destroy(m);
        markerObjects.Clear();
    }

    [Header("기즈모 미리보기")]
    [SerializeField] private bool previewTopCheck = true;

    private void OnDrawGizmos()
    {
        if (!previewTopCheck) return;

        // 이 오브젝트 위치를 블럭이라 가정하고 감지 박스 미리보기
        Vector2 boxCenter = new Vector2(transform.position.x, transform.position.y + topCheckOffset);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawCube(boxCenter, blockTopSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCenter, blockTopSize);
    }
}