using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 지도 위의 열쇠 아이콘. 차원문 아이콘과 같은 방식으로 스프라이트를 그대로 띄운다.
//
// MinimapPortalIcons와 합치지 않고 따로 둔 이유는 둘의 요구가 실제로 다르기 때문이다.
//   - 열쇠는 클릭 대상이 아니다. Button, 호버, 선택 가능 여부, 출발지 제외가 전부 필요 없다.
//     오히려 클릭을 먹으면 안 된다 - 고속이동 중에 차원문 아이콘을 가릴 수 있다.
//   - 열쇠는 움직인다. 플레이어가 들고 다니므로 매 프레임 위치를 다시 계산해야 하고,
//     지금 어느 방에 있는지도 그때그때 달라진다. 차원문은 한 번 배치하면 끝이다.
public class MinimapKeyIcons
{
    private readonly Dictionary<Key, RectTransform> icons = new Dictionary<Key, RectTransform>();

    private readonly RectTransform prefab;
    private readonly RectTransform container;
    private readonly Vector2 iconSize;
    private readonly Color iconColor;

    public MinimapKeyIcons(RectTransform prefab, RectTransform container, Vector2 iconSize, Color iconColor)
    {
        this.prefab = prefab;
        this.container = container;
        this.iconSize = iconSize;
        this.iconColor = iconColor;
    }

    public void Build(Room[] rooms)
    {
        if (prefab == null || container == null) return;

        foreach (Room room in rooms)
        {
            if (room.Keys == null) continue;

            foreach (Key key in room.Keys)
            {
                // using System 때문에 Object가 모호해진다. UnityEngine 쪽임을 명시한다.
                RectTransform icon = UnityEngine.Object.Instantiate(prefab, container);
                icon.name = "Key " + room.name;
                icon.sizeDelta = iconSize;

                // 발밑을 기준점으로 두어 아이콘을 키워도 위로만 자라게 한다 (차원문과 동일)
                icon.pivot = new Vector2(0.5f, 0f);

                ApplySprite(icon, key);

                // 위치는 Refresh에서 정한다. 열쇠가 움직이므로 여기서 한 번 놓아두면 곧 틀어진다.
                icon.gameObject.SetActive(false);

                icons[key] = icon;
            }
        }
    }

    private void ApplySprite(RectTransform icon, Key key)
    {
        Image image = icon.GetComponent<Image>();
        if (image == null) return;

        Sprite sprite = key.MapIcon;
        if (sprite != null) image.sprite = sprite;

        image.preserveAspect = true;
        image.color = iconColor;

        // 열쇠 아이콘은 표시 전용이다. 켜두면 차원문 아이콘 위에 겹칠 때 클릭을 가로챈다.
        image.raycastTarget = false;
    }

    // 셀 위에 그려지도록 형제 순서를 뒤로 보낸다
    public void BringToFront()
    {
        foreach (RectTransform icon in icons.Values)
            icon.SetAsLastSibling();
    }

    // roomAt: 월드 좌표가 어느 방에 속하는지 돌려주는 함수 (MapUI가 넘긴다. 방이 없으면 null)
    // worldToCellLocal: 방 안의 월드 좌표를 컨테이너 로컬 좌표로 바꾸는 함수
    //
    // 열쇠가 든 방을 매번 다시 찾는 이유: 플레이어가 들고 방을 넘어가면 원래 방 셀에 붙어 있으면 안 된다.
    // worldToCellLocal이 방 경계로 값을 잘라내므로, 방을 갱신하지 않으면 아이콘이 셀 끝에 붙어버린다.
    public void Refresh(Func<Vector3, Room> roomAt, Func<Room, Vector3, Vector2> worldToCellLocal)
    {
        foreach (KeyValuePair<Key, RectTransform> pair in icons)
        {
            Key key = pair.Key;
            RectTransform icon = pair.Value;

            if (key == null)
            {
                icon.gameObject.SetActive(false);
                continue;
            }

            Vector3 world = key.MapFootPosition;
            Room room = key.IsOnMap ? roomAt(world) : null;

            // 방문하지 않은 방의 열쇠는 그 방이 거기 있다는 것조차 드러내면 안 된다
            bool visible = room != null && room.IsVisited;
            icon.gameObject.SetActive(visible);
            if (!visible) continue;

            icon.anchoredPosition = worldToCellLocal(room, world);
        }
    }
}
