using System;
using DG.Tweening;
using Solo.MOST_IN_ONE;
using UnityEngine;

namespace Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        public AudioClip moveSound;
        public AudioClip exitSound;
        public AudioClip blockedSound;
        public AudioClip winSound;
        public AudioClip loseSound;
        public AudioClip buttonSound;
        public AudioClip coinReceiveSound;
        public AudioClip coinSpendSound;

        [Header("Settings")]
        [Range(0f, 1f)] public float volume = 1f;

        private bool isSoundOn = true;
        private bool isVibration = true;

        private AudioSource audioSource;
        private AudioSource arrowOutAudioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                audioSource = gameObject.AddComponent<AudioSource>();
                arrowOutAudioSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        void Start()
        {
            SettingScreenUI.OnVibrationUpDate += OnVibration;
            SettingScreenUI.OnSoundUpdate += OnSound;
        }

        private void OnSound(bool obj)
        {
            isSoundOn = obj;
        }

        private void OnVibration(bool obj)
        {
            isVibration = obj;
        }

        public void PlayMoveSound()
        {
            PlaySound(buttonSound);
            DOVirtual.DelayedCall(0.1f, () =>
            {
                PlaySound(moveSound);
            }, false);
        }

        public void PlayExitSound()
        {
            PlaySound(exitSound);
        }

        public void PlayBlockedSound()
        {
            PlaySound(blockedSound);
        }

        public void PlayWinSound()
        {
            PlaySound(winSound);
        }

        public void PlayLoseSound()
        {
            PlaySound(loseSound);
        }

        public void PlayButtonSound()
        {
            PlaySound(buttonSound);
        }

        public void PlayCoinReceiveSound()
        {
            PlaySound(coinReceiveSound);
        }

        public void PlayCoinSpendSound()
        {
            PlaySound(coinSpendSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (!isSoundOn) return;
            if (clip != null && audioSource != null)
            {
                if (clip == moveSound)
                {
                    ArrowOutSound(clip);
                    return;
                }
                audioSource.PlayOneShot(clip, volume);
            }
        }
        private void ArrowOutSound(AudioClip clip)
        {
            arrowOutAudioSource.PlayOneShot(clip, 0.2f);
        }

        public void PlaySuccessHaptic()
        {
            PlayHaptic(MOST_HapticFeedback.HapticTypes.Success);
        }
        public void PlayWarningHaptic()
        {
            PlayHaptic(MOST_HapticFeedback.HapticTypes.Warning);
        }
        public void PlayLightImpactHaptic()
        {
            PlayHaptic(MOST_HapticFeedback.HapticTypes.LightImpact);
        }

        private void PlayHaptic(MOST_HapticFeedback.HapticTypes hapticTypes = MOST_HapticFeedback.HapticTypes.LightImpact)
        {
            if (!isVibration) return;
            MOST_HapticFeedback.Generate(hapticTypes);
        }
    }
}
