using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Source")]
    public AudioSource sfxSource;

    [Header("Sound Effects")]
    public AudioClip swingClip;
    public AudioClip hitImpactClip;
    public AudioClip dashClip;
    public AudioClip playerHurtClip;
    public AudioClip enemyDieClip;

    void Awake()
    {
        instance = this;
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, bool randomPitch = true)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = randomPitch ? Random.Range(0.92f, 1.12f) : 1f;
        sfxSource.PlayOneShot(clip, volume);
    }
}