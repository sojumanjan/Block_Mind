using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MarkingManager : MonoBehaviour
{
    public static MarkingManager Instance;
    [Header("마킹 설정")]
    [SerializeField] private int maxMarkingCount = 5;      // 최대 마킹 가능 횟수 (인스펙터 조절)
    [SerializeField] private Transform markSourceTransform; // 마킹 위치의 기준이 되는 오브젝트
    [SerializeField] private GameObject markerPrefab;       // 마킹 위치 표시용 (선택, 없어도 동작)

    [Header("블럭 설정")]
    [SerializeField] private GameObject blockPrefab;        // 이동할 블럭 프리팹
    [SerializeField] private float moveSpeed = 5f;          // 블럭 이동 속도
    [SerializeField] private float waitTime = 1f;           // 첫, 마지막 지점에서 머무는 시간
    [SerializeField] private float cooldownTime = 3f;

    [Header("플레이어 동반 이동 설정")]
    [SerializeField] private LayerMask playerLayer;         // 플레이어 레이어
    [SerializeField] private Vector2 blockTopSize = new Vector2(1f, 0.1f); // 블럭 윗면 감지 박스 크기
    [SerializeField] private float topCheckOffset = 0.5f;   // 블럭 중심에서 윗면까지 거리

    private readonly List<Vector3> markPositions = new List<Vector3>();
    private readonly List<GameObject> markerObjects = new List<GameObject>();

    private InputActions inputActions;
    private bool isMoving = false;

    private Coroutine moveRoutine;      // 진행 중인 블럭 이동/뒷처리 코루틴
    private GameObject activeBlock;     // 실체화되어 있는 블럭 (없으면 null)

    public bool IsMoving => isMoving;
    public int MarkCount => markPositions.Count;
    public bool HasActiveBlock => activeBlock != null;

    private void Awake()
    {
        inputActions = new InputActions();
        if (Instance == null) Instance = this;
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

    // Awake 순서가 보장되지 않으므로 모든 Awake가 끝난 Start에서 구독한다
    private void Start()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate += ResetMarkingState;
    }

    private void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.CheckPointActivate -= ResetMarkingState;
    }

    private void Update()
    {
        HandleMarkingInput();
    }

    // 지도가 열려 있으면 마우스 입력은 지도 것으로 본다.
    private bool IsMapOpen => MapUI.Instance != null && MapUI.Instance.IsOpen;

    // 좌클릭 시 그림자 마킹을 남김.
    private void HandleMarkingInput()
    {
        if (isMoving) return;
        if (IsMapOpen) return;   // 지도 드래그가 마킹으로 새는 것을 막음

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

    // 그림자 위치에 마커를 남김.
    private void AddMark(Vector3 pos)
    {
        markPositions.Add(pos);

        if (markerPrefab != null)
        {
            GameObject marker = Instantiate(markerPrefab, pos, Quaternion.identity);
            markerObjects.Add(marker);
        }
    }

    // 우클릭 시 마킹이 하나라도 되어있다면 블럭 실체화를 시작함.
    private void OnStartMoving(InputAction.CallbackContext context)
    {
        if (markPositions.Count < 1) return;
        if (isMoving) return;
        if (IsMapOpen) return;   // 지도가 열려 있으면 우클릭을 무시

        moveRoutine = StartCoroutine(MoveBlockAlongPath());

        if (FollowingShadow.Instance != null)
            FollowingShadow.Instance.gameObject.SetActive(false);
    }

    // 최근에 찍은 마킹 하나를 제거. 블럭이 이동 중이거나 쿨타임이면 무시. 되돌릴 마킹이 있었으면 true.
    public bool UndoLastMark()
    {
        if (isMoving) return false;
        if (markPositions.Count == 0) return false;

        int last = markPositions.Count - 1;
        markPositions.RemoveAt(last);

        // markerPrefab이 없으면 markerObjects는 비어 있으므로 인덱스를 확인
        if (markerObjects.Count > last)
        {
            GameObject marker = markerObjects[last];
            markerObjects.RemoveAt(last);
            if (marker != null) Destroy(marker);
        }

        return true;
    }

    // 실체화된 블럭을 즉시 제거하고, 블럭이 끝점에 도착했을 때와 동일한 뒷처리를 진행.
    // 취소할 블럭이 있었으면 true. (쿨타임 중이면 블럭이 없으므로 false). MarkingUndoHandler에서 호출.
    public bool TryCancelBlockMove()
    {
        if (activeBlock == null) return false;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(FinishSequence());
        return true;
    }

    // 경로를 따라 블럭을 이동시키는 코루틴.
    private IEnumerator MoveBlockAlongPath()
    {
        isMoving = true;

        activeBlock = Instantiate(blockPrefab, markPositions[0], Quaternion.identity);

        yield return new WaitForSeconds(waitTime);

        for (int i = 1; i < markPositions.Count; i++)
        {
            Vector3 target = markPositions[i];

            while (activeBlock != null && Vector3.Distance(activeBlock.transform.position, target) > 0.001f)
            {
                Vector3 prevPos = activeBlock.transform.position;

                Vector3 newPos = Vector3.MoveTowards(prevPos, target, moveSpeed * Time.deltaTime);
                Vector3 delta = newPos - prevPos;

                CarryPlayerOnTop(activeBlock.transform.position, delta);

                activeBlock.transform.position = newPos;
                yield return null;
            }

            if (activeBlock == null) break;

            yield return new WaitForSeconds(waitTime);
        }

        yield return FinishSequence();
    }

    // 블럭 도착 -> 블럭 제거 -> 마킹 초기화 -> 쿨타임 -> 그림자 복귀
    private IEnumerator FinishSequence()
    {
        DestroyActiveBlock();
        ClearMarks();

        // 쿨타임 동안 마킹 입력 차단
        yield return new WaitForSeconds(cooldownTime);

        isMoving = false;
        moveRoutine = null;

        if (FollowingShadow.Instance != null)
            FollowingShadow.Instance.gameObject.SetActive(true);
    }

    private void DestroyActiveBlock()
    {
        if (activeBlock == null) return;

        Destroy(activeBlock);
        activeBlock = null;
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

    // 체크포인트 활성화 등 외부 요청으로 마킹 상태를 즉시 초기화. 쿨타임 없음.
    public void ResetMarkingState()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        DestroyActiveBlock();
        ClearMarks();

        isMoving = false;

        if (FollowingShadow.Instance != null)
            FollowingShadow.Instance.gameObject.SetActive(true);
    }

    public void GetMarkingCount()
    {
        maxMarkingCount++;
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