using System.Collections;
using UnityEngine;

public class AbilityBase : MonoBehaviour
{
    // 어빌리티 아이템 획득 후 이펙트 등 후처리는 이곳에서
    protected IEnumerator DestroyItem()
    {
        yield return null;
        gameObject.SetActive(false);
    }
}
