using UnityEngine;
using DG.Tweening;

// 오브젝트가 스스로 갖는, 시작과 끝이 있는 사운드.
// 매니저 풀은 "잡고 즉시 재생하고 끝나면 자동 반납"이라 재생 중인 소리를 멈출 손잡이가 없다.
// 도중에 멈춰야 하는 소리는 이 컴포넌트를 쓴다.
//
//   playOnEnable = true   오브젝트가 켜지면 자동 재생 (레이저 웅웅거림, 차원문 앰비언트)
//   playOnEnable = false  코드에서 Play() / Stop()을 부른다 (문 여닫히는 동안만)
//
// 감쇠는 AudioManager와 같은 규칙이다 - 같은 방 / 다른 방 2단계이고 거리는 보지 않는다.
[RequireComponent(typeof(AudioSource))]
public class LoopSound : MonoBehaviour
{
    [SerializeField] private SoundData sound;

    [Tooltip("켜면 오브젝트 활성화 시 자동 재생. 끄면 코드에서 Play/Stop을 직접 부른다")]
    [SerializeField] private bool playOnEnable = true;

    [Tooltip("끄면 클립이 한 번만 재생된다. 도중에 Stop으로 끊을 수 있는 건 그대로다")]
    [SerializeField] private bool loop = true;

    [Tooltip("멈출 때 이 시간만큼 볼륨을 줄인다. 0이면 뚝 끊겨서 딸깍 소리가 난다")]
    [SerializeField] private float fadeOutDuration = 0.08f;

    [Tooltip("여러 개가 동시에 울릴 때 위상이 겹치지 않도록 시작 지점을 무작위로 잡는다")]
    [SerializeField] private bool randomizeStartTime = false;

    [Tooltip("0이면 방 진입 이벤트로만 갱신한다(즉시 반응). 0보다 크면 그 주기로 한 번 더 확인하는 보험이 붙는다")]
    [SerializeField] private float roomCheckInterval = 0f;

    private AudioSource source;
    private Tween fadeTween;
    private float nextCheckTime;
    private bool isStopping;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        Configure();
    }

    // Awake 순서가 보장되지 않으므로 모든 Awake가 끝난 Start에서 구독한다
    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayerRoomChanged += OnPlayerRoomChanged;
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayerRoomChanged -= OnPlayerRoomChanged;
    }

    // 방이 바뀐 즉시 볼륨을 다시 계산한다. 재생 중이 아니면 할 일이 없다.
    private void OnPlayerRoomChanged()
    {
        if (source == null || !source.isPlaying) return;
        if (isStopping) return;

        ApplyRoomVolume();
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    private void OnDisable()
    {
        // 비활성화될 때는 페이드 없이 즉시 정리한다 (코루틴/트윈이 못 돌아간다)
        fadeTween?.Kill();
        isStopping = false;

        if (source != null) source.Stop();
    }

    public void Play()
    {
        if (sound == null || !sound.HasClip || source == null) return;

        fadeTween?.Kill();
        isStopping = false;

        if (source.clip == null) Configure();

        if (randomizeStartTime && source.clip != null)
            source.time = Random.Range(0f, source.clip.length);

        ApplyRoomVolume();
        source.Play();
    }

    public void Stop()
    {
        if (source == null || !source.isPlaying) return;

        fadeTween?.Kill();

        if (fadeOutDuration <= 0f)
        {
            isStopping = false;
            source.Stop();
            return;
        }

        // 페이드 중에는 ApplyRoomVolume이 볼륨을 다시 올려버리면 안 된다
        isStopping = true;

        fadeTween = source.DOFade(0f, fadeOutDuration)
            .OnComplete(() =>
            {
                source.Stop();
                isStopping = false;
            });
    }

    // 이벤트로 갱신하므로 평소에는 돌 필요가 없다.
    // roomCheckInterval을 0보다 크게 두면 이벤트를 놓쳤을 때를 대비한 보험으로 동작한다.
    private void Update()
    {
        if (roomCheckInterval <= 0f) return;
        if (source == null || !source.isPlaying) return;
        if (isStopping) return;
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
        source.loop = loop;
        source.playOnAwake = false;

        if (AudioManager.Instance != null)
            source.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
    }
}
