using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    public static PerformanceManager Instance;

    [SerializeField] private int targetFPS = 60;
    [SerializeField] private int targetAspectWidth = 16;
    [SerializeField] private int targetAspectHeight = 9;

    private Vector2Int lastSize;
    private float lastChangeTime;

    private const float debounceTime = 0.15f; // 防抖

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
    }

    private void Update()
    {
#if UNITY_STANDALONE
        HandleResize();
#endif
    }

    private void HandleResize()
    {
        Vector2Int current = new Vector2Int(Screen.width, Screen.height);

        // 視窗沒變 → 不處理
        if (current == lastSize) return;

        lastSize = current;
        lastChangeTime = Time.time;

        // 等使用者停下來再修正
        StartCoroutine(ApplyAfterDelay());
    }

    private System.Collections.IEnumerator ApplyAfterDelay()
    {
        yield return new WaitForSeconds(debounceTime);

        // 如果期間還在變動 → 取消這次修正
        if (Time.time - lastChangeTime < debounceTime)
            yield break;

        ApplyAspectRatio();
    }

    private void ApplyAspectRatio()
    {   
        // 全螢幕時不強制比例
        if (Screen.fullScreenMode != FullScreenMode.Windowed)
            return;

        int width = Screen.width;
        int height = Screen.height;

        float targetRatio = (float)targetAspectWidth / targetAspectHeight;
        float currentRatio = (float)width / height;

        if (Mathf.Abs(currentRatio - targetRatio) < 0.01f)
            return;

        // 以 width 為基準
        height = Mathf.RoundToInt(width / targetRatio);

        Screen.SetResolution(width, height, FullScreenMode.Windowed);
    }
}