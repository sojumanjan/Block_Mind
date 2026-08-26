using UnityEngine;

// 부모 스케일이 어떻든 이 오브젝트의 월드 스케일(= 화면에 보이는 크기와 비율)을 일정하게 유지한다.
// 문마다 부모 스케일이 달라서 자식 스프라이트의 localScale을 매번 손으로 역산하던 작업을 대신한다.
//
// 부모가 회전해 있어도 자식이 부모에 대해 회전하지 않았다면 정확하게 맞는다.
// (자식만 따로 회전시키면 비균일 스케일과 겹쳐 기울어짐이 생겨 보정할 수 없다)
[ExecuteAlways]
public class KeepWorldScale : MonoBehaviour
{
    [Tooltip("유지할 월드 스케일. 컴포넌트를 붙이는 순간의 크기가 자동으로 들어간다")]
    [SerializeField] private Vector3 targetWorldScale = Vector3.one;

    // 컴포넌트를 붙인 순간의 크기를 목표로 잡아, 기존 배치가 바뀌지 않게 한다
    private void Reset()
    {
        targetWorldScale = transform.lossyScale;
    }

    private void OnEnable()
    {
        Apply();
    }

    // 부모가 런타임에 스케일을 바꿔도(문 여닫힘 트윈 등) 따라가야 하므로 매 프레임 확인한다
    private void LateUpdate()
    {
        Apply();
    }

    [ContextMenu("현재 크기를 목표로 저장")]
    private void CaptureCurrentScale()
    {
        targetWorldScale = transform.lossyScale;
    }

    private void Apply()
    {
        Transform parent = transform.parent;
        Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;

        Vector3 local = new Vector3(
            Compensate(targetWorldScale.x, parentScale.x),
            Compensate(targetWorldScale.y, parentScale.y),
            Compensate(targetWorldScale.z, parentScale.z));

        // 같은 값을 매 프레임 다시 대입하면 에디터가 계속 더티로 표시된다
        if (transform.localScale != local)
            transform.localScale = local;
    }

    private static float Compensate(float target, float parentScale)
    {
        return Mathf.Approximately(parentScale, 0f) ? target : target / parentScale;
    }
}
