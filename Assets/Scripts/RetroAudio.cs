using UnityEngine;

public enum RetroSound
{
    PlayerAttack,
    PlayerHit,
    MonsterHit,
    MonsterDefeated,
    ChestOpen,
    Potion,
    BossSummon,
    DoorOpen,
    RouletteGood,
    RouletteBad,

    // 보스 전용. 일반 몬스터 소리와 섞이면 보스라는 느낌이 안 난다.
    BossGroundCast,
    BossLightningCast,
    BossLightningStrike,
    BossOrbCast,
    BossOrbExplode,
    BossHit,
    BossDeath,
    BossPhaseTwo
}

public class RetroAudio : MonoBehaviour
{
    private const int SampleRate = 44100;
    private const string VolumeSettingKey = "MasterVolume";

    private static RetroAudio instance;

    private AudioSource audioSource;
    private AudioClip[] clips;

    public static float MasterVolume
    {
        get { return AudioListener.volume; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAutomatically()
    {
        EnsureInstance();
    }

    private static RetroAudio EnsureInstance()
    {
        if (null != instance)
        {
            return instance;
        }

        GameObject audioObject = new GameObject("RetroAudio");
        instance = audioObject.AddComponent<RetroAudio>();
        return instance;
    }

    private void Awake()
    {
        if (null != instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.0f;
        audioSource.volume = 0.48f;
        audioSource.priority = 0;
        audioSource.ignoreListenerPause = true;
        audioSource.mute = false;

        AudioListener.volume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(VolumeSettingKey, 0.80f)
        );

        CreateClips();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void Play(RetroSound sound)
    {
        RetroAudio audio = EnsureInstance();
        if (null == audio.audioSource || null == audio.clips)
        {
            return;
        }

        int index = (int)sound;
        if (0 > index || index >= audio.clips.Length || null == audio.clips[index])
        {
            return;
        }

        audio.audioSource.PlayOneShot(audio.clips[index]);
    }

    public static void SetMasterVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        AudioListener.volume = clampedVolume;
        PlayerPrefs.SetFloat(VolumeSettingKey, clampedVolume);
        PlayerPrefs.Save();
    }

    private void CreateClips()
    {
        clips = new AudioClip[System.Enum.GetValues(typeof(RetroSound)).Length];
        clips[(int)RetroSound.PlayerAttack] = CreateTone("Attack", 420.0f, 720.0f, 0.09f, true);
        clips[(int)RetroSound.PlayerHit] = CreateTone("PlayerHit", 190.0f, 90.0f, 0.18f, true);
        clips[(int)RetroSound.MonsterHit] = CreateTone("MonsterHit", 260.0f, 160.0f, 0.10f, true);
        clips[(int)RetroSound.MonsterDefeated] = CreateTone("MonsterDefeated", 260.0f, 70.0f, 0.25f, false);
        clips[(int)RetroSound.ChestOpen] = CreateTone("ChestOpen", 360.0f, 880.0f, 0.28f, false);
        clips[(int)RetroSound.Potion] = CreateTone("Potion", 520.0f, 980.0f, 0.25f, false);
        clips[(int)RetroSound.BossSummon] = CreateTone("BossSummon", 75.0f, 240.0f, 0.75f, true);
        clips[(int)RetroSound.DoorOpen] = CreateTone("DoorOpen", 120.0f, 210.0f, 0.16f, true);
        clips[(int)RetroSound.RouletteGood] = CreateTone("RouletteGood", 440.0f, 1040.0f, 0.42f, false);
        clips[(int)RetroSound.RouletteBad] = CreateTone("RouletteBad", 260.0f, 70.0f, 0.48f, true);

        clips[(int)RetroSound.BossGroundCast] = CreateTone("BossGroundCast", 95.0f, 38.0f, 0.45f, true);
        clips[(int)RetroSound.BossLightningCast] = CreateTone("BossLightningCast", 300.0f, 900.0f, 0.30f, false);
        clips[(int)RetroSound.BossLightningStrike] = CreateTone("BossLightningStrike", 1250.0f, 120.0f, 0.22f, true);
        clips[(int)RetroSound.BossOrbCast] = CreateTone("BossOrbCast", 540.0f, 190.0f, 0.26f, true);
        clips[(int)RetroSound.BossOrbExplode] = CreateTone("BossOrbExplode", 170.0f, 45.0f, 0.30f, true);
        clips[(int)RetroSound.BossHit] = CreateTone("BossHit", 200.0f, 130.0f, 0.10f, true);
        clips[(int)RetroSound.BossDeath] = CreateTone("BossDeath", 210.0f, 38.0f, 1.10f, true);
        clips[(int)RetroSound.BossPhaseTwo] = CreateTone("BossPhaseTwo", 150.0f, 640.0f, 0.90f, true);
    }

    private AudioClip CreateTone(
        string clipName,
        float startFrequency,
        float endFrequency,
        float duration,
        bool squareWave)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0.0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float progress = (float)i / Mathf.Max(1, sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            phase += frequency / SampleRate;

            float wave = Mathf.Sin(phase * Mathf.PI * 2.0f);
            if (true == squareWave)
            {
                wave = 0.0f <= wave ? 0.75f : -0.75f;
            }

            float attack = Mathf.Clamp01(progress / 0.06f);
            float release = Mathf.Clamp01((1.0f - progress) / 0.35f);
            samples[i] = wave * attack * release * 0.42f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
