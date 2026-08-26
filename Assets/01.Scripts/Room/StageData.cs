using UnityEngine;

// 스테이지 하나의 공통 속성. 에셋 하나 = 스테이지 하나다.
// 소속 방 목록은 담지 않는다. 참조는 Room -> StageData 한 방향(N:1)이고,
// 어떤 방이 이 스테이지인지는 각 Room이 이 에셋을 가리키는 것으로 결정된다.
//
// 스테이지에 딸린 속성(BGM, 앰비언트 색, 해금 상태 등)이 생기면 여기에 추가한다.
// 그러면 Room이나 MapUI를 고칠 필요가 없다.
[CreateAssetMenu(menuName = "Block Mind/Stage Data", fileName = "Stage_00")]
public class StageData : ScriptableObject
{
    [Tooltip("UI에 보여줄 스테이지 이름")]
    [SerializeField] private string displayName = "";

    [Tooltip("이 스테이지 방들의 미니맵 지형 색")]
    [SerializeField] private Color minimapGroundColor = new Color(0.80f, 0.82f, 0.88f, 1f);

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

    public Color MinimapGroundColor => minimapGroundColor;
}
