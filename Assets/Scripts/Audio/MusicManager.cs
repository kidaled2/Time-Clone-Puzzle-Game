using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeClone.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicManager : MonoBehaviour
    {
        private const int MainMenuBuildIndex = 0;
        private const string MainMenuSceneName = "MainMenu";

        public static MusicManager Instance { get; private set; }

        [Header("Music Clips")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip gameplayMusic;

        [Header("Playback")]
        [SerializeField, Range(0f, 1f)] private float mainMenuVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float gameplayVolume = 0.65f;
        [SerializeField] private AudioSource audioSource;

        private MusicTrack currentTrack = MusicTrack.None;

        private enum MusicTrack
        {
            None,
            MainMenu,
            Gameplay
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            ConfigureAudioSource();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            ApplySceneMusic(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        public static void PlayMainMenuMusic(bool restart = true)
        {
            if (Instance != null)
            {
                Instance.PlayTrack(MusicTrack.MainMenu, restart);
            }
        }

        public static void PlayGameplayMusic(bool restartIfAlreadyPlaying = false)
        {
            if (Instance != null)
            {
                Instance.PlayTrack(MusicTrack.Gameplay, restartIfAlreadyPlaying);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneMusic(scene);
        }

        private void ApplySceneMusic(Scene scene)
        {
            bool isMainMenu = scene.buildIndex == MainMenuBuildIndex || scene.name == MainMenuSceneName;
            PlayTrack(isMainMenu ? MusicTrack.MainMenu : MusicTrack.Gameplay, isMainMenu);
        }

        private void PlayTrack(MusicTrack track, bool restart)
        {
            AudioClip clip = track == MusicTrack.MainMenu ? mainMenuMusic : gameplayMusic;
            if (audioSource == null || clip == null)
            {
                return;
            }

            if (currentTrack == track && audioSource.clip == clip && audioSource.isPlaying)
            {
                if (restart)
                {
                    audioSource.time = 0f;
                }

                audioSource.volume = GetVolume(track);
                return;
            }

            currentTrack = track;
            audioSource.clip = clip;
            audioSource.volume = GetVolume(track);
            audioSource.time = 0f;
            audioSource.Play();
        }

        private float GetVolume(MusicTrack track)
        {
            return track == MusicTrack.MainMenu ? mainMenuVolume : gameplayVolume;
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
        }
    }
}
