using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 지도 위의 고속이동 차원문 아이콘. MapUI에서 분리한 부분.
//
// 구운 텍스처가 아니라 별도 UI여야 한다 - 텍스처 픽셀은 클릭을 못 받는다.
public class MinimapPortalIcons
{
    private readonly Dictionary<Portal, Button> icons = new Dictionary<Portal, Button>();

    private readonly RectTransform prefab;
    private readonly RectTransform container;
    private readonly Vector2 iconSize;
    private readonly Color iconColor;

    public MinimapPortalIcons(RectTransform prefab, RectTransform container, Vector2 iconSize, Color iconColor)
    {
        this.prefab = prefab;
        this.container = container;
        this.iconSize = iconSize;
        this.iconColor = iconColor;
    }

    // worldToCellLocal: 방 안의 월드 좌표를 컨테이너 로컬 좌표로 바꾸는 함수 (MapUI가 넘긴다)
    public void Build(Room[] rooms, Func<Room, Vector3, Vector2> worldToCellLocal, Action<Portal> onClicked)
    {
        if (prefab == null || container == null) return;

        foreach (Room room in rooms)
        {
            if (room.Portals == null) continue;

            foreach (Portal portal in room.Portals)
            {
                // using System 때문에 Object가 모호해진다. UnityEngine 쪽임을 명시한다.
                RectTransform icon = UnityEngine.Object.Instantiate(prefab, container);
                icon.name = "Portal " + room.name;
                icon.sizeDelta = iconSize;

                // 발밑을 기준점으로 두어 아이콘을 키워도 위로만 자라게 한다
                icon.pivot = new Vector2(0.5f, 0f);
                icon.anchoredPosition = worldToCellLocal(room, portal.MapFootPosition);

                ApplySprite(icon, portal);

                Button button = icon.GetComponent<Button>();
                if (button == null)
                {
                    Debug.LogWarning("portalIconPrefab에 Button이 없습니다. 선택할 수 없습니다.", icon);
                    continue;
                }

                // Button의 ColorTint는 targetGraphic의 색을 자기가 덮어쓴다.
                // 상태별 색은 여기서 Image.color로 직접 칠하므로 트랜지션을 끈다.
                // (켜두면 interactable=false일 때 disabledColor의 알파가 먹어 반투명해진다)
                button.transition = Selectable.Transition.None;

                // 프리팹에 없더라도 호버 피드백이 붙도록 보장
                if (icon.GetComponent<MapPortalIconHover>() == null)
                    icon.gameObject.AddComponent<MapPortalIconHover>();

                Portal captured = portal;   // 클로저가 반복 변수를 잡지 않도록 복사
                button.onClick.AddListener(() => onClicked(captured));

                icons[portal] = button;
            }
        }
    }

    // 차원문 스프라이트를 그대로 아이콘으로 쓴다. 비율은 유지한다.
    private static void ApplySprite(RectTransform icon, Portal portal)
    {
        Image image = icon.GetComponent<Image>();
        if (image == null) return;

        Sprite sprite = portal.MapIcon;
        if (sprite == null) return;

        image.sprite = sprite;
        image.preserveAspect = true;
    }

    // 셀 위에 그려지도록 형제 순서를 뒤로 보낸다
    public void BringToFront()
    {
        foreach (Button icon in icons.Values)
            icon.transform.SetAsLastSibling();
    }

    // 방문한 방의 차원문만 보인다. 선택은 고속이동 모드에서만 가능하다.
    public void Refresh(bool selectable, Portal origin)
    {
        foreach (KeyValuePair<Portal, Button> pair in icons)
        {
            Portal portal = pair.Key;
            Button button = pair.Value;

            bool visible = portal.Room != null && portal.Room.IsVisited;
            button.gameObject.SetActive(visible);
            if (!visible) continue;

            // 선택 가능 여부는 모드로만 갈린다. 색은 어느 모드에서든 동일하게 둔다.
            button.interactable = selectable && portal != origin;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = iconColor;
        }
    }
}
