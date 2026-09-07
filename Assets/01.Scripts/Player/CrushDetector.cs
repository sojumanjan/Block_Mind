using UnityEngine;

public class CrushDetector : SingletonBehaviour<CrushDetector>
{
    [SerializeField] private LayerMask crushLayers;

    public event System.Action OnCrushed;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & crushLayers) != 0)
        {
            OnCrushed?.Invoke();
        }
    }
}