using UnityEngine;
using Redline.Config;
using Redline.Core;

namespace Redline.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class BikeAudioController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private RaceConfig config;
        [SerializeField] private BikeEngineModel engine;
        [SerializeField] private SequentialGearbox gearbox;
        [SerializeField] private RaceSessionController session;

        [Header("Audio Clips (Assigned in Inspector)")]
        [SerializeField] private AudioClip engineLoopClip;
        [SerializeField] private AudioClip shiftBarkClip;
        [SerializeField] private AudioClip limiterPulseClip;
        [SerializeField] private AudioClip countdownBeepClip;
        [SerializeField] private AudioClip greenLightGoClip;
        [SerializeField] private AudioClip victoryFanfareClip;

        [Header("Mixer & Volume")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.8f;
        [SerializeField] private bool isMuted = false;

        private AudioSource _engineSource;
        private AudioSource _sfxSource;

        // Thread-safe cached values for the audio DSP thread
        private volatile bool _hasCustomClip;
        private volatile bool _threadIsMuted;
        private volatile bool _threadIsOnLimiter;
        private volatile bool _threadIsShiftCut;
        private float _threadRpm = 2800f;
        private float _threadVolume = 0.8f;
        private float _limiterTimer;
        private volatile bool _threadLimiterPulse;

        private float _sampleRate = 48000f;
        private float _proceduralPhase;
        private bool _isShiftCutActive;
        private float _shiftCutTimer;
        private float _stateVolumeMultiplier = 0.5f;
        private float _limiterSfxTimer = 0f;

        public bool IsMuted => isMuted;
        public void SetConfig(RaceConfig newConfig) => config = newConfig;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (config == null)
            {
                config = RaceConfig.GetDefaultConfig();
                if (config != null) UnityEditor.EditorUtility.SetDirty(this);
            }
            if (engine == null) engine = GetComponent<BikeEngineModel>();
            if (gearbox == null) gearbox = GetComponent<SequentialGearbox>();
            if (session == null) session = GetComponent<RaceSessionController>();

            if (engineLoopClip == null) engineLoopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Engine_1000cc_Loop.wav");
            if (shiftBarkClip == null) shiftBarkClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Quickshifter_Bark.wav");
            if (limiterPulseClip == null) limiterPulseClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/RevLimiter_Stutter.wav");
            if (countdownBeepClip == null) countdownBeepClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Countdown_Beep.wav");
            if (greenLightGoClip == null) greenLightGoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/GreenLight_Go.wav");
            if (victoryFanfareClip == null) victoryFanfareClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Victory_Fanfare.wav");
        }
#endif

        private void Awake()
        {
            // Ensure Unity AudioListener is unpaused and active
            AudioListener.pause = false;
            AudioListener.volume = 1f;

            if (config == null) config = RaceConfig.GetDefaultConfig();
            if (engine == null) engine = GetComponent<BikeEngineModel>();
            if (gearbox == null) gearbox = GetComponent<SequentialGearbox>();
            if (session == null) session = GetComponent<RaceSessionController>();

#if UNITY_EDITOR
            if (engineLoopClip == null) engineLoopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Engine_1000cc_Loop.wav");
            if (shiftBarkClip == null) shiftBarkClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Quickshifter_Bark.wav");
            if (limiterPulseClip == null) limiterPulseClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/RevLimiter_Stutter.wav");
            if (countdownBeepClip == null) countdownBeepClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Countdown_Beep.wav");
            if (greenLightGoClip == null) greenLightGoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/GreenLight_Go.wav");
            if (victoryFanfareClip == null) victoryFanfareClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Victory_Fanfare.wav");
#endif

            _sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000f;

            _engineSource = GetComponent<AudioSource>();
            if (_engineSource == null) _engineSource = gameObject.AddComponent<AudioSource>();
            _engineSource.loop = true;
            _engineSource.playOnAwake = false;
            _engineSource.spatialBlend = 0f; // Pure 2D cockpit audio: zero distance attenuation
            _engineSource.priority = 0;     // Highest audio priority

            // Separate child GameObject for SFX to avoid multiple AudioSources on the GameObject with OnAudioFilterRead
            Transform existingSfx = transform.Find("Bike_SFX_Source");
            if (existingSfx != null)
            {
                _sfxSource = existingSfx.GetComponent<AudioSource>();
            }
            else
            {
                GameObject sfxChild = new GameObject("Bike_SFX_Source");
                sfxChild.transform.SetParent(transform, false);
                _sfxSource = sfxChild.AddComponent<AudioSource>();
            }
            if (_sfxSource != null)
            {
                _sfxSource.playOnAwake = false;
                _sfxSource.spatialBlend = 0f; // Pure 2D SFX
            }

            if (gearbox != null)
            {
                gearbox.OnUpshifted += HandleUpshift;
            }
        }

        private void OnDestroy()
        {
            if (gearbox != null) gearbox.OnUpshifted -= HandleUpshift;
        }

        private void Start()
        {
            if (_engineSource != null && engineLoopClip != null)
            {
                _engineSource.clip = engineLoopClip;
                _engineSource.loop = true;
                _engineSource.spatialBlend = 0f;
                _engineSource.Play();
            }
            else if (_engineSource != null)
            {
                // Play carrier so OnAudioFilterRead procedural synth is pumped
                if (!_engineSource.isPlaying)
                {
                    _engineSource.Play();
                }
            }
        }

        private void Update()
        {
            if (session == null) session = FindAnyObjectByType<RaceSessionController>();
            if (engine == null) engine = FindAnyObjectByType<BikeEngineModel>();
            if (config == null) config = RaceConfig.GetDefaultConfig();

            // Guarantee engine loop is continuously playing
            if (_engineSource != null && engineLoopClip != null && !_engineSource.isPlaying && !isMuted)
            {
                _engineSource.clip = engineLoopClip;
                _engineSource.loop = true;
                _engineSource.spatialBlend = 0f;
                _engineSource.Play();
            }

            // Engine state volume:
            // Ready: Superbike idles on the grid at 50% volume so the player immediately hears the rumbling engine!
            // Staging & Racing: Full 100% roaring engine power!
            // Result: Shuts off completely (0% volume) so the bike audio ends cleanly when the race ends!
            float targetStateVolume = 1f;
            if (session != null)
            {
                if (session.State == RaceState.Ready)
                {
                    targetStateVolume = 0.50f;
                }
                else if (session.State == RaceState.Racing && session.IsFinishingRollout)
                {
                    targetStateVolume = 0.25f; // Throttle cut coast-down past finish line
                }
                else if (session.State == RaceState.Result)
                {
                    targetStateVolume = 0f; // Bike audio ends immediately upon finishing the race
                }
            }

            float fadeSpeed = (session != null && session.State == RaceState.Result) ? 10f : 6f;
            _stateVolumeMultiplier = Mathf.MoveTowards(_stateVolumeMultiplier, targetStateVolume, Time.deltaTime * fadeSpeed);

            // Handle shift cut duration on main thread
            if (_isShiftCutActive)
            {
                _shiftCutTimer -= Time.deltaTime;
                if (_shiftCutTimer <= 0f)
                {
                    _isShiftCutActive = false;
                }
            }

            // Limiter pulse timing on main thread
            _limiterTimer += Time.deltaTime * 90f;
            _threadLimiterPulse = Mathf.Sin(_limiterTimer) > 0f;

            // Rev limiter rapid stutter sound effects (active only while racing, never in Result screen)
            bool isRacingState = session != null && session.State == RaceState.Racing;
            if (engine != null && engine.IsOnLimiter && !isMuted && isRacingState)
            {
                _limiterSfxTimer -= Time.deltaTime;
                if (_limiterSfxTimer <= 0f)
                {
                    _limiterSfxTimer = 0.08f; // ~12.5 Hz stutter pops
                    if (limiterPulseClip != null && _sfxSource != null)
                    {
                        _sfxSource.PlayOneShot(limiterPulseClip, masterVolume * 0.75f);
                    }
                }
            }
            else
            {
                _limiterSfxTimer = 0f;
            }

            // Cache thread-safe primitives for DSP audio thread
            _hasCustomClip = (engineLoopClip != null);
            _threadIsMuted = isMuted;
            _threadIsShiftCut = _isShiftCutActive;

            if (engine != null && config != null)
            {
                _threadRpm = engine.CurrentRpm;
                _threadIsOnLimiter = engine.IsOnLimiter;

                float volumeFactor = isMuted ? 0f : (masterVolume * _stateVolumeMultiplier);
                if (_isShiftCutActive) volumeFactor *= 0.15f;
                if (session != null && session.State == RaceState.Result && _stateVolumeMultiplier < 0.01f)
                {
                    volumeFactor = 0f;
                }
                _threadVolume = volumeFactor;

                if (_hasCustomClip && _engineSource != null)
                {
                    float rpmRatio = Mathf.InverseLerp(config.idleRpm, config.redlineRpm, engine.CurrentRpm);
                    // Pitch shifts smoothly from deep 0.70x rumble at 2,800 RPM to screaming 2.25x howl at 13,000 RPM
                    _engineSource.pitch = Mathf.Lerp(0.70f, 2.25f, rpmRatio);
                    
                    // Slightly quieter at idle (~0.70x), opening to full 1.0x on throttle
                    float rpmVol = Mathf.Lerp(0.70f, 1.0f, rpmRatio);
                    _engineSource.volume = volumeFactor * rpmVol;
                }
            }
        }

        private void HandleUpshift(int newGear, ShiftGrade grade)
        {
            if (isMuted) return;

            // Trigger ignition cut
            _isShiftCutActive = true;
            _shiftCutTimer = 0.07f; // 70ms

            // Play shift bark on separate SFX AudioSource
            if (shiftBarkClip != null && _sfxSource != null)
            {
                _sfxSource.PlayOneShot(shiftBarkClip, masterVolume);
            }
        }

        public void PlayCountdownBeep()
        {
            if (isMuted || _sfxSource == null || countdownBeepClip == null) return;
            _sfxSource.PlayOneShot(countdownBeepClip, masterVolume * 0.9f);
        }

        public void PlayGreenLightGo()
        {
            if (isMuted || _sfxSource == null || greenLightGoClip == null) return;
            _sfxSource.PlayOneShot(greenLightGoClip, masterVolume);
        }

        public void PlayVictoryFanfare()
        {
            if (isMuted || _sfxSource == null || victoryFanfareClip == null) return;
            _sfxSource.PlayOneShot(victoryFanfareClip, masterVolume);
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
        }

        public void SetVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
        }

        // Runs on Unity's high-priority DSP audio thread!
        // NOTE: NEVER access UnityEngine.Object (null checks on clips/components) or Time.time here!
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_threadIsMuted)
            {
                for (int i = 0; i < data.Length; i++) data[i] = 0f;
                return;
            }

            // CRITICAL FIX: If an audio clip (Engine_1000cc_Loop.wav) is playing on the AudioSource,
            // DO NOT zero or overwrite data! Let Unity's native audio engine stream the audio clip directly!
            if (_hasCustomClip)
            {
                return;
            }

            // Fallback: only synthesize procedural audio if NO audio clip is present
            if (_threadVolume <= 0.0001f)
            {
                for (int i = 0; i < data.Length; i++) data[i] = 0f;
                return;
            }

            float rpm = _threadRpm;
            float fundamentalFreq = (rpm / 60f) * 2f; // 4-cylinder 4-stroke
            float sampleRate = _sampleRate;
            float volume = _threadIsShiftCut ? 0.03f : _threadVolume * 0.22f;

            for (int i = 0; i < data.Length; i += channels)
            {
                _proceduralPhase += 2f * Mathf.PI * fundamentalFreq / sampleRate;
                if (_proceduralPhase > 2f * Mathf.PI) _proceduralPhase -= 2f * Mathf.PI;

                // Rich 1000cc superbike harmonics
                float sample = Mathf.Sin(_proceduralPhase) * 0.5f 
                             + Mathf.Sin(_proceduralPhase * 2f) * 0.3f 
                             + Mathf.Sin(_proceduralPhase * 3f) * 0.15f
                             + (Mathf.PingPong(_proceduralPhase * 4f, 1f) - 0.5f) * 0.1f;

                // Limiter chatter
                if (_threadIsOnLimiter)
                {
                    sample *= _threadLimiterPulse ? 1.0f : 0.2f;
                }

                sample *= volume;

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] += sample;
                }
            }
        }
    }
}
