using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityMarkingFirst : AbilityBase
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            AbilityManager.Instance.UnlockMarkingFirst();
            StartCoroutine(base.DestroyItem());
        }
    }
}
