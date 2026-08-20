using UnityEngine;

public class Apple : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            AppleManager.Instance.GetApple();
            Destroy(gameObject);
        }
    }
}
