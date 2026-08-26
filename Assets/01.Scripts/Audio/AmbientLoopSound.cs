using UnityEngine;

// 오브젝트가 스스로 갖는 지속 루프 사운드. 레이저 웅웅거림, 차원문 앰비언트 등.
// 이런 소리는 켜짐/꺼짐과 위치가 오브젝트에 묶여 있어서 매니저 풀에 맡길 수 없다.
// (풀에 맡기면 "어느 레이저가 어느 소스를 쓰는지"를 매니저가 추적해야 한다)
[RequireComponent(typeof(AudioSource))]
public class AmbientLoopSound : MonoBehaviour
{
    [SerializeField] private SoundData sound;

    [Tooltip("여러 개가 동시에 울릴 때 위상이 겹치지 않도록 시작 지점을 무작위로 잡는다")]
    [SerializeField] private bool randomizeStartTime = true;

    private AudioSource source;

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

        source.Play();
    }

    private void OnDisable()
    {
        if (source != null) source.Stop();
    }

    private void Configure()
    {
        if (sound == null || source == null) return;

        source.clip = sound.PickClip();
        source.volume = sound.Volume;
        source.pitch = sound.PickPitch();
        source.spatialBlend = sound.Spatial ? 1f : 0f;
        source.loop = true;
        source.playOnAwake = false;

        if (AudioManager.Instance != null)
            source.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
    }
}
