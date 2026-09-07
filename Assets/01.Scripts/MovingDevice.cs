using UnityEngine;

// 움직이는 동안만 소리가 나는 장치의 공통 부분.
// Door, KeyDoor, MovingDoor가 똑같은 필드와 Play/Stop 쌍을 각자 갖고 있었다.
//
// 필드 이름(moveSound)은 바꾸지 않는다. Unity는 직렬화된 값을 필드 이름으로 찾으므로
// 이름을 바꾸면 이미 연결해 둔 프리팹의 참조가 끊긴다.
public abstract class MovingDevice : ActivatableDevice
{
    [Header("사운드")]
    [Tooltip("장치가 움직이는 동안만 재생된다. LoopSound의 playOnEnable은 꺼두어야 한다")]
    [SerializeField] private LoopSound moveSound;

    protected void PlayMoveSound()
    {
        if (moveSound != null) moveSound.Play();
    }

    protected void StopMoveSound()
    {
        if (moveSound != null) moveSound.Stop();
    }
}
