using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

// 오디오 소유 규칙:
//   일회성 효과음  -> 이 매니저의 풀. 재생 중에 오브젝트가 파괴돼도 소리가 끊기지 않는다.
//                     (Key.Consume과 MarkingManager의 블럭은 Destroy되므로 자기 AudioSource로는 안 된다)
//   지속 루프      -> 오브젝트가 자기 AudioSource를 갖는다. AmbientLoopSound 참고.
//   UI 효과음      -> 이 매니저의 2D 전용 소스.
//   BGM            -> 이 매니저의 A/B 소스 크로스페이드. 곡은 StageData가 소유한다.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("믹서 그룹 (비워도 동작하지만 볼륨 조절 UI를 붙이려면 필요)")]
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;

    [Header("효과음 풀")]
    [Tooltip("동시에 울릴 수 있는 효과음 개수. 넘치면 가장 오래 쓴 것을 재사용한다")]
    [SerializeField] private int poolSize = 12;

    [Header("3D 감쇠 (방 하나가 32유닛)")]
    [Tooltip("이 거리까지는 최대 음량")]
    [SerializeField] private float minDistance = 6f;
    [Tooltip("이 거리를 넘으면 들리지 않는다. 방 하나 폭보다 작게 두면 옆 방 소리가 새지 않는다")]
    [SerializeField] private float maxDistance = 26f;

    [Header("BGM")]
    [SerializeField] private float bgmFadeDuration = 1.5f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.7f;

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private int nextPoolIndex;

    private AudioSource uiSource;
    private AudioSource bgmA;
    private AudioSource bgmB;
    private AudioSource activeBgm;
    private AudioClip currentBgmClip;

    public AudioMixerGroup SfxGroup => sfxGroup;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        BuildSources();
    }

    // 오디오소스 풀링을 위한 공간 poolSize개만큼 생성. UI와 bgm은 각각 1, 2개
    private void BuildSources()
    {
        for (int i = 0; i < poolSize; i++)
            pool.Add(CreateSource("Sfx_" + i.ToString("00"), true));

        uiSource = CreateSource("UI", false);
        bgmA = CreateSource("Bgm_A", false);
        bgmB = CreateSource("Bgm_B", false);
    }

    private AudioSource CreateSource(string sourceName, bool spatial)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = spatial ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;   // 방 단위로 자르기 쉬운 감쇠
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;

        return source;
    }

    // ---------------------------------------------------------------- 효과음

    // 호출부에서 null 검사를 반복하지 않도록 static 래퍼를 둔다
    public static void PlaySfx(SoundData sound, Vector3 position)
    {
        if (Instance == null) return;
        Instance.Play(sound, position);
    }

    public static void PlayUiSfx(SoundData sound)
    {
        if (Instance == null) return;
        Instance.PlayUI(sound);
    }

    public void Play(SoundData sound, Vector3 position)
    {
        if (sound == null || !sound.HasClip) return;

        // spatial이 꺼진 사운드는 위치를 무시하고 2D로 재생한다
        if (!sound.Spatial)
        {
            PlayUI(sound);
            return;
        }

        AudioSource source = TakeFromPool();
        if (source == null) return;

        source.transform.position = position;
        Configure(source, sound, true);
        source.Play();
    }

    public void PlayUI(SoundData sound)
    {
        if (sound == null || !sound.HasClip || uiSource == null) return;

        Configure(uiSource, sound, false);
        uiSource.Play();
    }

    private void Configure(AudioSource source, SoundData sound, bool spatial)
    {
        source.clip = sound.PickClip();
        source.volume = sound.Volume;
        source.pitch = sound.PickPitch();
        source.spatialBlend = spatial ? 1f : 0f;
        source.loop = false;
        source.outputAudioMixerGroup = sfxGroup;
    }

    // 비어 있는 소스를 먼저 찾고, 전부 재생 중이면 라운드로빈으로 가장 오래 쓴 것을 빼앗는다
    private AudioSource TakeFromPool()
    {
        foreach (AudioSource source in pool)
            if (!source.isPlaying) return source;

        if (pool.Count == 0) return null;

        AudioSource stolen = pool[nextPoolIndex];
        nextPoolIndex = (nextPoolIndex + 1) % pool.Count;

        return stolen;
    }

    // ---------------------------------------------------------------- BGM

    public static void PlayBgm(AudioClip clip)
    {
        if (Instance == null) return;
        Instance.SwitchBgm(clip);
    }

    public void SwitchBgm(AudioClip clip)
    {
        if (clip == null)
        {
            StopBgm();
            return;
        }

        // 같은 곡이면 다시 시작하지 않는다.
        // RoomCameraTrigger가 방을 넘을 때마다 호출하므로 이 검사가 없으면 곡이 계속 끊긴다.
        if (clip == currentBgmClip) return;

        currentBgmClip = clip;

        AudioSource next = activeBgm == bgmA ? bgmB : bgmA;
        AudioSource previous = activeBgm;

        next.DOKill();
        next.clip = clip;
        next.loop = true;
        next.volume = 0f;
        next.outputAudioMixerGroup = bgmGroup;
        next.Play();
        next.DOFade(bgmVolume, bgmFadeDuration);

        if (previous != null && previous.isPlaying)
        {
            previous.DOKill();
            previous.DOFade(0f, bgmFadeDuration).OnComplete(() => previous.Stop());
        }

        activeBgm = next;
    }

    public void StopBgm()
    {
        currentBgmClip = null;

        if (activeBgm == null) return;

        AudioSource fading = activeBgm;
        fading.DOKill();
        fading.DOFade(0f, bgmFadeDuration).OnComplete(() => fading.Stop());

        activeBgm = null;
    }
}
