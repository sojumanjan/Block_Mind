using UnityEngine;

public class AppleManager : MonoBehaviour
{
    public static AppleManager Instance;
    public int totalAppleCount { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public void GetApple()
    {
        totalAppleCount++;
    }
}
