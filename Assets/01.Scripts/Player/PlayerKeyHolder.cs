using UnityEngine;

public class PlayerKeyHolder : MonoBehaviour
{
    private Key heldKey;

    public bool HasKey => heldKey != null;

    public void AddKey(Key key)
    {
        heldKey = key;
    }

    public void UseKey()
    {
        if (heldKey == null) return;

        heldKey.Consume();
        heldKey = null;
    }
}