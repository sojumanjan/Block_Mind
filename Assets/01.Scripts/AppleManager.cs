using UnityEngine;

public class AppleManager : SingletonBehaviour<AppleManager>
{
    public int totalAppleCount { get; private set; }

    public void GetApple()
    {
        totalAppleCount++;
    }
}
