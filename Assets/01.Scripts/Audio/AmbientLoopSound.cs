using UnityEngine;

// 오브젝트가 스스로 갖는 지속 루프 사운드. 레이저 웅웅거림, 차원문 앰비언트 등.
// 이런 소리는 켜짐/꺼짐과 위치가 오브젝트에 묶여 있어서 매니저 풀에 맡길 수 없다.
// (풀에 맡기면 "어느 레이저가 어느 소스를 쓰는지"를 매니저가 추적해야 한다)
//
// 감쇠도 매니저와 같은 규칙이다 - 같은 방 / 다른 방 2단계이고 거리는 보지 않는다.
[RequireComponent(typeof(AudioSource))]
public class AmbientLoopSound : MonoBehaviour
{
    [SerializeField] private SoundData sound;

    [Tooltip("여러 개가 동시에 울릴 때 위상이 겹치지 않도록 시작 지점을 무작위로 잡는다")]
    [SerializeField] private bool randomizeStartTime = true;

    [Tooltip("플레이어가 방을 옮겼는지 확인하는 주기(초). 매 프레임 볼 필요가 없다")]
    [SerializeField] private float roomCheckInterval = 0.2f;

    private AudioSource source;
    private float nextCheckTime;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        Configure();
    }

    private void OnEnable()
    {
        if (sound == null || !sound.HasClip || source == null) return;
        if (source.clip == null) Configure();

        if (randomizeStartTime && source.clip != null)
            source.time = Random.Range(0f, source.clip.length);

        ApplyRoomVolume();
        source.Play();
    }

    private void OnDisable()
    {
        if (source != null) source.Stop();
    }

    private void Update()
    {
        if (source == null || !source.isPlaying) return;
        if (Time.time < nextCheckTime) return;

        nextCheckTime = Time.time + roomCheckInterval;
        ApplyRoomVolume();
    }

    // 같은 방이면 원래 볼륨, 다른 방이면 SoundData가 정한 배율
    private void ApplyRoomVolume()
    {
        if (sound == null || source == null) return;

        source.volume = AudioManager.Instance != null
            ? AudioManager.Instance.RoomVolumeFor(sound, transform.position)
            : sound.Volume;
    }

    private void Configure()
    {
        if (sound == null || source == null) return;

        source.clip = sound.PickClip();
        source.volume = sound.Volume;
        source.pitch = sound.PickPitch();
        source.spatialBlend = 0f;   // 전부 2D. 감쇠는 볼륨으로 처리한다
        source.loop = true;
        source.playOnAwake = false;

        if (AudioManager.Instance != null)
            source.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
    }
}
