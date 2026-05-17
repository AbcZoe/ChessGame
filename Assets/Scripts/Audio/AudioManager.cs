using UnityEngine;
using UnityEngine.UI;

public class AudioManager : PersistentSingleton<AudioManager>
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource SFXPlayer;
    [SerializeField] private AudioSource BGMPlayer;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;

    const float MIN_PITCH = 0.95f;
    const float MAX_PITCH = 1.05f;

    protected override void Awake()
    {
        base.Awake();

        // ¸ü¤J­µ¶q
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);

        ApplyVolumes();
    }

    #region SFX
    public void PlaySFX(AudioData audioData)
    {
        float finalVolume =
            audioData.volume *
            masterVolume *
            sfxVolume;

        SFXPlayer.pitch = 1f;
        SFXPlayer.PlayOneShot(audioData.audioClip, finalVolume);
    }

    public void PlayRandomSFX(AudioData audioData)
    {
        SFXPlayer.pitch = Random.Range(MIN_PITCH, MAX_PITCH);
        PlaySFX(audioData);
    }
    #endregion

    #region Volume
    public void SetMasterVolume(float value)
    {
        masterVolume = value;

        PlayerPrefs.SetFloat("MasterVolume", value);

        ApplyVolumes();
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = value;

        PlayerPrefs.SetFloat("BGMVolume", value);

        ApplyVolumes();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;

        PlayerPrefs.SetFloat("SFXVolume", value);

        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        SFXPlayer.volume = masterVolume * sfxVolume;

        if (BGMPlayer != null)
            BGMPlayer.volume = masterVolume * bgmVolume;
    }
    #endregion
}

[System.Serializable]
public class AudioData
{
    public AudioClip audioClip;
    public float volume = 1f;
}