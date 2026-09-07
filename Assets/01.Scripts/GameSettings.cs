using UnityEngine;
using UnityEngine.Audio;

// 볼륨과 전체화면 설정을 저장하고 적용한다.
// ESC 메뉴를 한 번도 열지 않아도 게임 시작 시 적용되어야 하므로 메뉴와 분리해 둔다.
//
// 믹서에 "SFXVolume" / "BGMVolume"이 노출 파라미터로 등록되어 있어야 한다.
// 믹서 창에서 이름을 바꾸면 아래 상수도 같이 바꿔야 한다.
public class GameSettings : SingletonBehaviour<GameSettings>
{
    public const string SfxParam = "SFXVolume";
    public const string BgmParam = "BGMVolume";

    private const string SfxKey = "settings.sfxVolume";
    private const string BgmKey = "settings.bgmVolume";
    private const string FullscreenKey = "settings.fullscreen";

    [SerializeField] private AudioMixer mixer;

    [Tooltip("슬라이더가 0일 때 적용할 dB. -80이면 사실상 무음")]
    [SerializeField] private float minDecibel = -80f;

    public float SfxVolume { get; private set; }
    public float BgmVolume { get; private set; }
    public bool Fullscreen { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
        BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, 1f));
        Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
    }

    // AudioMixer.SetFloat은 에디트 모드에서 항상 실패하고 런타임에만 먹는다.
    // Awake가 아니라 Start에서 적용해 믹서가 준비된 뒤에 쓴다.
    private void Start()
    {
        SetSfxVolume(SfxVolume);
        SetBgmVolume(BgmVolume);
        SetFullscreen(Fullscreen);
    }

    public void SetSfxVolume(float linear01)
    {
        SfxVolume = Mathf.Clamp01(linear01);
        ApplyToMixer(SfxParam, SfxVolume);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
    }

    public void SetBgmVolume(float linear01)
    {
        BgmVolume = Mathf.Clamp01(linear01);
        ApplyToMixer(BgmParam, BgmVolume);
        PlayerPrefs.SetFloat(BgmKey, BgmVolume);
    }

    public void SetFullscreen(bool on)
    {
        Fullscreen = on;

        // 에디터에서는 창 모드가 바뀌지 않는다. 빌드에서만 실제로 적용된다.
        Screen.fullScreenMode = on ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        PlayerPrefs.SetInt(FullscreenKey, on ? 1 : 0);
    }

    // 슬라이더는 선형인데 사람 귀와 믹서는 로그(dB)라 그대로 넣으면
    // 절반으로 줄여도 거의 안 줄어든 것처럼 들린다. dB로 변환해서 넣는다.
    private void ApplyToMixer(string parameterName, float linear01)
    {
        if (mixer == null) return;

        // Log10(0)은 -무한이라 0은 따로 처리해야 한다
        float decibel = linear01 <= 0.0001f ? minDecibel : Mathf.Log10(linear01) * 20f;

        mixer.SetFloat(parameterName, decibel);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
