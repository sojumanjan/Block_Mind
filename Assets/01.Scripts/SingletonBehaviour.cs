using UnityEngine;

// 씬에 하나만 존재하는 컴포넌트의 공통 처리.
//
// 매번 직접 쓰면 빠뜨리게 되는 두 가지를 여기서 한 번만 처리한다.
//
//   1) 파괴될 때 Instance를 비운다.
//      비우지 않으면 씬을 다시 불러왔을 때 이미 파괴된 오브젝트를 가리키는 static 참조가 남는다.
//      "처음부터 다시" 같은 씬 리로드 기능을 만들 수 없었던 원인이 이것이다.
//
//   2) 중복 인스턴스를 알려준다.
//      그냥 무시하면 나중에 붙은 쪽이 조용히 동작하지 않아 원인을 찾기 어렵다.
public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    public static T Instance { get; private set; }

    // 파생 클래스가 Awake를 쓸 때는 반드시 base.Awake()를 먼저 부른다
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(typeof(T).Name + "이(가) 씬에 둘 이상 있습니다. 나중에 발견된 쪽은 Instance로 등록되지 않습니다.", this);
            return;
        }

        Instance = (T)this;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
