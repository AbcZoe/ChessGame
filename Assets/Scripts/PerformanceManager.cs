using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    public static PerformanceManager Instance;

    [SerializeField] private int targetFPS = 60;

    [Header("Aspect Ratio")]
    [SerializeField] private int targetAspectWidth = 16;
    [SerializeField] private int targetAspectHeight = 9;

    private int minWidth;
    private int minHeight;

    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFPS;

#if UNITY_STANDALONE
        InitResolution();
#endif
    }

    void Start()
    {
#if UNITY_ANDROID
        Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
    }

    private void Update()
    {
#if UNITY_STANDALONE
        // 只有當視窗尺寸改變時才處理
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            FixResolution();
        }
#endif
    }

#if UNITY_STANDALONE
    private void InitResolution()
    {
        int screenW = Display.main.systemWidth;
        int screenH = Display.main.systemHeight;

        minWidth = screenW / 2;
        minHeight = screenH / 2;

        Screen.SetResolution(minWidth, minHeight, FullScreenMode.Windowed);

        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    private void FixResolution()
    {
        int width = Screen.width;
        int height = Screen.height;

        // 最小限制
        width = Mathf.Max(width, minWidth);
        height = Mathf.Max(height, minHeight);

        // 強制比例 16:9
        float targetRatio = (float)targetAspectWidth / targetAspectHeight;
        float currentRatio = (float)width / height;

        if (Mathf.Abs(currentRatio - targetRatio) > 0.01f)
        {
            height = Mathf.RoundToInt(width / targetRatio);
        }

        Screen.SetResolution(width, height, Screen.fullScreenMode);
    }
#endif

    public void SetFullscreen(bool fullscreen)
    {
#if UNITY_STANDALONE
        Screen.fullScreenMode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
#endif
    }
}