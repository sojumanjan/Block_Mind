using UnityEngine;

public class PlayerKeyHolder : MonoBehaviour
{
    public static PlayerKeyHolder Instance;
    private Key heldKey;

    public bool HasKey => heldKey != null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

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

    public void ResetKey()
    {
        if (heldKey != null) {
            heldKey.ResetLocation();
            heldKey = null;
        }
    }
}