using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    [Header("능력 오브젝트 참조")]
    [SerializeField] private FollowingShadow followingShadow;
    [SerializeField] private MarkingManager markingSystem;

    public bool HasShadowAbility { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 게임 시작 시 능력 false로 설정.
        SetMarkingFirst(true);
    }

    // 마킹 시스템 해금 + 마킹 1개 능력 해금
    public void SetMarkingFirst(bool unlocked)
    {
        HasShadowAbility = unlocked;

        // 그림자와 마킹시스템 On
        if (followingShadow != null)
            followingShadow.gameObject.SetActive(unlocked);
        if (markingSystem != null)
            markingSystem.gameObject.SetActive(unlocked);
        if (FollowingShadow.Instance != null)
            FollowingShadow.Instance.transform.position =  PlayerController.Instance.transform.position;
    }

    // 특정 방 진입, 아이템 습득 등 해금 트리거에서 호출
    public void UnlockMarkingFirst() => SetMarkingFirst(true);
}