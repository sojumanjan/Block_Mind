using UnityEngine;

// 효과음 하나 = 에셋 하나.
// 볼륨/피치를 코드가 아니라 에셋에서 조절하게 하는 것이 목적이다.
// 사운드 밸런싱은 반복 작업이라 인스펙터에서 만져야 한다.
[CreateAssetMenu(menuName = "Block Mind/Sound Data", fileName = "SFX_00")]
public class SoundData : ScriptableObject
{
    [Tooltip("여러 개를 넣으면 재생할 때마다 무작위로 하나를 고른다. 같은 소리가 반복되는 느낌을 줄인다")]
    [SerializeField] private AudioClip[] clips;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Tooltip("재생마다 이 범위에서 피치를 무작위로 고른다. x와 y를 같게 두면 고정")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("방 단위 감쇠")]
    [Tooltip("켜면 소리가 난 방과 플레이어가 있는 방이 다를 때 작게 들린다. " +
             "거리에 비례하지 않으므로 1칸 옆이든 10칸 옆이든 같은 크기다")]
    [SerializeField] private bool muffleOutsideRoom = false;

    [Tooltip("다른 방에서 났을 때의 볼륨 배율")]
    [Range(0f, 1f)]
    [SerializeField] private float outsideRoomVolume = 0.35f;

    public float Volume => volume;

    public bool MuffleOutsideRoom => muffleOutsideRoom;

    public float OutsideRoomVolume => outsideRoomVolume;

    public bool HasClip
    {
        get
        {
            if (clips == null) return false;

            foreach (AudioClip clip in clips)
                if (clip != null) return true;

            return false;
        }
    }

    // 비어 있는 칸이 섞여 있어도 안전하게 고른다
    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;

        for (int attempt = 0; attempt < clips.Length; attempt++)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) return clip;
        }

        foreach (AudioClip clip in clips)
            if (clip != null) return clip;

        return null;
    }

    public float PickPitch()
    {
        return Random.Range(Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));
    }
}
