using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections;
using TMPro;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;

    private const string LANGUAGE_KEY = "Language";

    [Header("Dropdown")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Language Codes")]
    [SerializeField] private string[] languageCodes;

    private IEnumerator Start()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            yield break;
        }

        yield return LocalizationSettings.InitializationOperation;

        LoadLanguage();
    }

    // ===== Dropdown 呼叫 =====
    public void OnDropdownChanged(int index)
    {
        if (index < 0 || index >= languageCodes.Length)
            return;

        SetLanguage(languageCodes[index]);
    }

    // ===== 切換語言 =====
    public void SetLanguage(string code)
    {
        var locale =
            LocalizationSettings.AvailableLocales.GetLocale(code);

        if (locale == null)
        {
            Debug.LogWarning($"Locale not found: {code}");
            return;
        }

        LocalizationSettings.SelectedLocale = locale;

        PlayerPrefs.SetString(LANGUAGE_KEY, code);
        PlayerPrefs.Save();
    }

    // ===== 載入語言 =====
    private void LoadLanguage()
    {
        string code =
            PlayerPrefs.GetString(LANGUAGE_KEY, "en");

        var locale =
            LocalizationSettings.AvailableLocales.GetLocale(code);

        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
        }

        // 同步 Dropdown
        if (languageDropdown != null)
        {
            for (int i = 0; i < languageCodes.Length; i++)
            {
                if (languageCodes[i] == code)
                {
                    languageDropdown.value = i;
                    languageDropdown.RefreshShownValue();
                    break;
                }
            }
        }
    }
}