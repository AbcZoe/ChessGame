using UnityEngine;

public class AudioManager : PersistentSingleton<AudioManager> 
{
    [SerializeField] AudioSource SFXPlayer;

    const float MIN_PITCH = 0.95f;
    const float MAX_PITCH = 1.0f;

    public void PlaySFX(AudioData audioData)
    {
        SFXPlayer.PlayOneShot(audioData.audioClip, audioData.volume);
    }

    public void PlayRandomSFX(AudioData audioData)
    {
        SFXPlayer.pitch = Random.Range(MIN_PITCH, MAX_PITCH);
        PlaySFX(audioData);
    }

}

[System.Serializable] public class AudioData
{
    public AudioClip audioClip;
    public float volume = 1f;
}