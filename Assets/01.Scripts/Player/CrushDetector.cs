using UnityEngine;

public class CrushDetector : MonoBehaviour
{
    public static CrushDetector Instance;
    [SerializeField] private LayerMask crushLayers;

    public event System.Action OnCrushed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & crushLayers) != 0)
        {
            OnCrushed?.Invoke();
        }
    }
}