using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ESC 일시정지 메뉴. 화면 전체를 불투명하게 덮는다.
//
// 지도가 열려 있을 때 ESC는 지도만 닫는다(한 단계씩 뒤로). 메뉴는 그 다음 ESC부터 열린다.
public class PauseMenu : SingletonBehaviour<PauseMenu>
{

    [Header("패널")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject settingsPanel;

    [Header("메뉴 버튼")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("설정")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button backButton;

    [Tooltip("일시정지 중 효과음/BGM을 멈춘다. 끄면 레이저 웅웅거림 등이 계속 울린다")]
    [SerializeField] private bool pauseAudio = true;

    // 일시정지 중 막을 입력. 스크립트마다 new InputActions()로 자기 인스턴스를 갖고 있어서
    // 한 군데서 끄려면 살아있는 에셋 인스턴스를 전부 찾아 각각 꺼야 한다.
    private static readonly string[] GameplayMaps = { "Player", "Block" };

    // UI 맵은 Pause 액션이 들어 있어 통째로 끌 수 없다. 개별 액션만 끈다.
    private static readonly string[] BlockedUiActions = { "Map", "Interact", "DebugRevealMap" };

    private readonly List<InputActionMap> disabledMaps = new List<InputActionMap>();
    private readonly List<InputAction> disabledActions = new List<InputAction>();

    private InputActions inputActions;
    private float previousTimeScale = 1f;

    public bool IsPaused { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.UI.Pause.performed += OnPausePressed;
    }

    private void OnDisable()
    {
        inputActions.UI.Pause.performed -= OnPausePressed;
        inputActions.Disable();

        // 정지 상태로 비활성화되면 게임이 멈춘 채로 남는다. 반드시 되돌린다.
        if (IsPaused) Resume();
    }

    private void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(RestartFromCheckpoint);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
        if (backButton != null) backButton.onClick.AddListener(CloseSettings);

        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (panel != null) panel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        // 정지 중이면 설정 -> 메뉴 -> 게임 순으로 한 단계씩 되돌린다
        if (IsPaused)
        {
            if (settingsPanel != null && settingsPanel.activeSelf) CloseSettings();
            else Resume();
            return;
        }

        // 지도가 열려 있으면 지도부터 닫는다. 메뉴는 다음 ESC에 열린다.
        if (MapUI.Instance != null && MapUI.Instance.IsOpen)
        {
            MapUI.Instance.Close();
            return;
        }

        Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // AudioSource는 timeScale과 무관하게 계속 울리므로 따로 멈춘다
        if (pauseAudio) AudioListener.pause = true;

        BlockGameplayInput();

        if (panel != null) panel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        SyncSettingsWidgets();
    }

    public void Resume()
    {
        IsPaused = false;

        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        AudioListener.pause = false;

        RestoreGameplayInput();

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    private void RestartFromCheckpoint()
    {
        // 열쇠/마킹/문 리셋은 체크포인트를 다시 밟은 것과 동일하게 처리한다
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.RestartFromCurrentCheckpoint();

        Resume();
    }

    private void OpenSettings()
    {
        SyncSettingsWidgets();
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Quit()
    {
        PlayerPrefs.Save();

        // 에디터에서는 Application.Quit이 아무 일도 하지 않는다
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 저장된 값을 위젯에 반영한다. 리스너가 되울리지 않도록 알림 없이 넣는다.
    private void SyncSettingsWidgets()
    {
        if (GameSettings.Instance == null) return;

        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameSettings.Instance.SfxVolume);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(GameSettings.Instance.BgmVolume);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Instance.Fullscreen);
    }

    private void OnSfxChanged(float value)
    {
        if (GameSettings.Instance != null) GameSettings.Instance.SetSfxVolume(value);
    }

    private void OnBgmChanged(float value)
    {
        if (GameSettings.Instance != null) GameSettings.Instance.SetBgmVolume(value);
    }

    private void OnFullscreenChanged(bool value)
    {
        if (GameSettings.Instance != null) GameSettings.Instance.SetFullscreen(value);
    }

    private void BlockGameplayInput()
    {
        disabledMaps.Clear();
        disabledActions.Clear();

        foreach (InputActionAsset asset in Resources.FindObjectsOfTypeAll<InputActionAsset>())
        {
            foreach (string mapName in GameplayMaps)
            {
                InputActionMap map = asset.FindActionMap(mapName, false);
                if (map == null || !map.enabled) continue;

                map.Disable();
                disabledMaps.Add(map);
            }

            foreach (string actionName in BlockedUiActions)
            {
                InputAction action = asset.FindAction("UI/" + actionName, false);
                if (action == null || !action.enabled) continue;

                action.Disable();
                disabledActions.Add(action);
            }
        }
    }

    // 우리가 끈 것만 되돌린다. 원래 꺼져 있던 건 건드리지 않는다.
    private void RestoreGameplayInput()
    {
        foreach (InputActionMap map in disabledMaps)
            if (map != null) map.Enable();

        foreach (InputAction action in disabledActions)
            if (action != null) action.Enable();

        disabledMaps.Clear();
        disabledActions.Clear();
    }
}
