using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

// 오디오 소유 규칙:
//   일회성 효과음  -> 이 매니저의 풀. 재생 중에 오브젝트가 파괴돼도 소리가 끊기지 않는다.
//                     (Key.Consume과 MarkingManager의 블럭은 Destroy되므로 자기 AudioSource로는 안 된다)
//   멈출 수 있는 소리 -> 오브젝트가 자기 AudioSource를 갖는다. LoopSound 참고.
//                      (풀은 재생 중인 소리로 돌아갈 손잡이가 없어서 Stop을 지원할 수 없다)
//   BGM            -> 이 매니저의 A/B 소스 크로스페이드. 곡은 StageData가 소유한다.
//
// 감쇠 방식:
//   Unity의 3D 롤오프를 쓰지 않는다. 거리에 비례해 줄어들면 1칸 옆과 10칸 옆이 달라지기 때문이다.
//   대신 "같은 방 / 다른 방" 2단계로만 갈린다. SoundData.muffleOutsideRoom 참고.
//   모든 소스는 2D(spatialBlend 0)이고 볼륨만 조절한다.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("믹서 그룹 (비워도 동작하지만 볼륨 조절 UI를 붙이려면 필요)")]
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [Tooltip("다른 방 소리를 보낼 그룹. Lowpass를 걸어두면 볼륨만 줄이는 것보다 '멀다'는 느낌이 산다. 비워두면 sfxGroup을 쓴다")]
    [SerializeField] private AudioMixerGroup sfxMuffledGroup;

    [Header("효과음 풀")]
    [Tooltip("동시에 울릴 수 있는 효과음 개수. 넘치면 가장 오래 쓴 것을 재사용한다")]
    [SerializeField] private int poolSize = 12;

    [Header("BGM")]
    [SerializeField] private float bgmFadeDuration = 1.5f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.7f;

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private int nextPoolIndex;

    private AudioSource bgmA;
    private AudioSource bgmB;
    private AudioSource activeBgm;
    private AudioClip currentBgmClip;

    public AudioMixerGroup SfxGroup => sfxGroup;

    // 플레이어가 방을 옮겼을 때 발생. LoopSound가 즉시 볼륨을 다시 계산한다.
    // 폴링으로 확인하면 방에 들어간 뒤 최대 검사주기만큼 늦게 반응한다.
    public event Action PlayerRoomChanged;

    public static void NotifyPlayerRoomChanged()
    {
        if (Instance == null) return;
        Instance.PlayerRoomChanged?.Invoke();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;

        BuildSources();
    }

    // 오디오소스 풀링을 위한 공간 poolSize개만큼 생성. bgm은 크로스페이드용으로 2개
    private void BuildSources()
    {
        for (int i = 0; i < poolSize; i++)
            pool.Add(CreateSource("Sfx_" + i.ToString("00")));

        bgmA = CreateSource("Bgm_A");
        bgmB = CreateSource("Bgm_B");
    }

    private AudioSource CreateSource(string sourceName)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // 전부 2D. 감쇠는 볼륨 계산으로 처리한다

        return source;
    }

    // ---------------------------------------------------------------- 효과음

    // 호출부에서 null 검사를 반복하지 않도록 static 래퍼를 둔다

    // 위치가 있는 효과음. SoundData가 muffleOutsideRoom이면 다른 방에서 난 소리는 작게 들린다.
    public static void PlaySfx(SoundData sound, Vector3 position)
    {
        if (Instance == null) return;
        Instance.Play(sound, position);
    }

    // 위치 개념이 없는 효과음(UI 등). 항상 원래 볼륨으로 들린다.
    public static void PlayUiSfx(SoundData sound)
    {
        if (Instance == null) return;
        Instance.PlayUI(sound);
    }

    public void Play(SoundData sound, Vector3 position)
    {
        if (sound == null || !sound.HasClip) return;

        bool outsideRoom = sound.MuffleOutsideRoom && IsOutsidePlayerRoom(position);
        float volume = outsideRoom ? sound.Volume * sound.OutsideRoomVolume : sound.Volume;

        PlayInternal(sound, volume, outsideRoom);
    }

    public void PlayUI(SoundData sound)
    {
        if (sound == null || !sound.HasClip) return;

        PlayInternal(sound, sound.Volume, false);
    }

    // UI든 효과음이든 같은 풀을 쓴다.
    // 단일 소스를 공유하면 두 소리가 겹칠 때 앞의 것이 잘린다.
    private void PlayInternal(SoundData sound, float volume, bool muffled)
    {
        AudioSource source = TakeFromPool();
        if (source == null) return;

        source.clip = sound.PickClip();
        source.volume = volume;
        source.pitch = sound.PickPitch();
        source.loop = false;
        source.outputAudioMixerGroup = muffled && sfxMuffledGroup != null ? sfxMuffledGroup : sfxGroup;
        source.Play();
    }

    // 소리가 난 방과 플레이어가 있는 방이 다른가.
    // 거리를 재지 않으므로 몇 칸 떨어졌는지는 결과에 영향이 없다.
    public bool IsOutsidePlayerRoom(Vector3 position)
    {
        if (PlayerController.Instance == null) return false;

        Vector2Int soundRoom = Room.WorldToCoordinate(position);
        Vector2Int playerRoom = Room.WorldToCoordinate(PlayerController.Instance.transform.position);

        return soundRoom != playerRoom;
    }

    // LoopSound가 자기 볼륨을 정할 때 쓴다
    public float RoomVolumeFor(SoundData sound, Vector3 position)
    {
        if (sound == null) return 0f;
        if (!sound.MuffleOutsideRoom) return sound.Volume;

        return IsOutsidePlayerRoom(position) ? sound.Volume * sound.OutsideRoomVolume : sound.Volume;
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
