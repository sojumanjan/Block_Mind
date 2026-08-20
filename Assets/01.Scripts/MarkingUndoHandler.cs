using UnityEngine;
using UnityEngine.InputSystem;

public class MarkingUndoHandler : MonoBehaviour
{
    [SerializeField] private MarkingManager markingSystem;

    private InputActions inputActions;

    private void Awake()
    {
        inputActions = new InputActions();

        if (markingSystem == null)
            markingSystem = GetComponent<MarkingManager>();

        if (markingSystem == null)
            Debug.LogWarning("markingSystem이 지정되지 않았습니다.", this);
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Block.Undo.performed += OnUndo;
    }

    private void OnDisable()
    {
        inputActions.Block.Undo.performed -= OnUndo;
        inputActions.Disable();
    }

    private void OnUndo(InputAction.CallbackContext context)
    {
        if (markingSystem == null) return;

        // 블럭이 실체화된 상태가 우선. 취소되면 마킹은 뒷처리에서 함께 정리된다.
        if (markingSystem.CancelBlockMove()) return;

        markingSystem.UndoLastMark();
    }
}
