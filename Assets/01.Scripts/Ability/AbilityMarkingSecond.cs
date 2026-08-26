using UnityEngine;

public class AbilityMarkingSecond : AbilityBase
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            AbilityManager.Instance.UnlockMarkingSecond();
            StartCoroutine(base.DestroyItem());
        }
    }
}
