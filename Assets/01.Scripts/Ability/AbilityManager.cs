using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityManager : SingletonBehaviour<AbilityManager>
{

    [Header("능력 오브젝트 참조")]
    [SerializeField] private FollowingShadow followingShadow;
    [SerializeField] private MarkingManager markingSystem;

    public bool hasMarkingFirst { get; private set; }
    public bool hasMarkingSecond { get; private set; }


    private void Start()
    {
        // 게임 시작 시 능력 false로 설정.
        SetMarkingFirst(false);
        MarkingManager.Instance.SetMarkingCount(1);
    }

    // 마킹 시스템 + 마킹 1개 능력 해금
    private void SetMarkingFirst(bool unlocked)
    {
        hasMarkingFirst = unlocked;

        // 그림자와 마킹시스템 On
        if (followingShadow != null)
            followingShadow.gameObject.SetActive(unlocked);
        if (markingSystem != null)
            markingSystem.gameObject.SetActive(unlocked);
        if (FollowingShadow.Instance != null)
            FollowingShadow.Instance.transform.position =  PlayerController.Instance.transform.position;
    }

    // 두번째 마킹 개수 해금
    private void SetMarkingSecond(bool unlocked)
    {
        // 그냥 마킹 카운트만 올리면 끝
        if (unlocked)
        {
            MarkingManager.Instance.SetMarkingCount(2);
            hasMarkingSecond = true;
        }
        else MarkingManager.Instance.SetMarkingCount(1);
    }

    // 업글 템 획득시 호출
    public void UnlockMarkingFirst() => SetMarkingFirst(true);
    public void UnlockMarkingSecond() => SetMarkingSecond(true);
}