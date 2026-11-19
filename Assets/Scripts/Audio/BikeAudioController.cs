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

        [Header("Audio Clips (Optional - Uses Procedural Synth if unassigned)")]
        [SerializeField] private AudioClip engineLoopClip;
        [SerializeField] private AudioClip shiftBarkClip;
        [SerializeField] private AudioClip limiterPulseClip;

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

        private void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000f;

            _engineSource = GetComponent<AudioSource>();
            _engineSource.loop = true;
            _engineSource.playOnAwake = false;

            // Separate child GameObject for SFX to avoid multiple AudioSources on the GameObject with OnAudioFilterRead
            GameObject sfxChild = new GameObject("Bike_SFX_Source");
            sfxChild.transform.SetParent(transform, false);
            _sfxSource = sfxChild.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

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
            if (engineLoopClip != null)
            {
                _engineSource.clip = engineLoopClip;
                _engineSource.Play();
            }
            else
            {
                // Play silent carrier so OnAudioFilterRead is actively pumped by the engine
                if (!_engineSource.isPlaying)
                {
                    _engineSource.Play();
                }
            }
        }

        private void Update()
        {
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

            // Cache thread-safe primitives for DSP audio thread
            _hasCustomClip = (engineLoopClip != null);
            _threadIsMuted = isMuted;
            _threadIsShiftCut = _isShiftCutActive;

            if (engine != null && config != null)
            {
                _threadRpm = engine.CurrentRpm;
                _threadIsOnLimiter = engine.IsOnLimiter;

                float volumeFactor = isMuted ? 0f : masterVolume;
                if (_isShiftCutActive) volumeFactor *= 0.1f;
                _threadVolume = volumeFactor;

                if (_hasCustomClip)
                {
                    float rpmRatio = Mathf.InverseLerp(config.idleRpm, config.redlineRpm, engine.CurrentRpm);
                    _engineSource.pitch = Mathf.Lerp(0.75f, 2.3f, rpmRatio);
                    _engineSource.volume = volumeFactor * (engine.CurrentRpm > config.idleRpm + 100f ? 1.0f : 0.6f);
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
            if (_hasCustomClip || _threadIsMuted) return;

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
