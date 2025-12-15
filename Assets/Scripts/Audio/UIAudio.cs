using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unbound.Audio
{
    /// <summary>
    /// Simple component to add audio feedback to UI elements
    /// Attach to any UI element with Button, Toggle, or implement pointer events
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UIAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler
    {
        [Header("Sound IDs")]
        [SerializeField] private string hoverSoundID = "ui_hover";
        [SerializeField] private string clickSoundID = "ui_click";
        [SerializeField] private string pressDownSoundID;

        [Header("Audio Clips (Alternative to IDs)")]
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip pressDownClip;

        [Header("Settings")]
        [SerializeField] private bool playHoverSound = true;
        [SerializeField] private bool playClickSound = true;
        [SerializeField] private bool playPressDownSound = false;
        [SerializeField] private float volume = 1f;

        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!playHoverSound || !IsInteractable())
                return;

            PlaySound(hoverSoundID, hoverClip);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!playClickSound || !IsInteractable())
                return;

            PlaySound(clickSoundID, clickClip);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!playPressDownSound || !IsInteractable())
                return;

            PlaySound(pressDownSoundID, pressDownClip);
        }

        /// <summary>
        /// Plays the appropriate sound
        /// </summary>
        private void PlaySound(string soundID, AudioClip clip)
        {
            if (AudioManager.Instance == null)
                return;

            // Prefer clip if provided
            if (clip != null)
            {
                AudioManager.Instance.PlayUIDirect(clip, volume);
            }
            else if (!string.IsNullOrEmpty(soundID))
            {
                AudioManager.Instance.PlayUI(soundID);
            }
        }

        /// <summary>
        /// Checks if the UI element is interactable
        /// </summary>
        private bool IsInteractable()
        {
            return _selectable == null || _selectable.interactable;
        }

        /// <summary>
        /// Sets the hover sound ID
        /// </summary>
        public void SetHoverSound(string soundID)
        {
            hoverSoundID = soundID;
        }

        /// <summary>
        /// Sets the click sound ID
        /// </summary>
        public void SetClickSound(string soundID)
        {
            clickSoundID = soundID;
        }

        /// <summary>
        /// Sets whether hover sound is enabled
        /// </summary>
        public void SetHoverEnabled(bool enabled)
        {
            playHoverSound = enabled;
        }

        /// <summary>
        /// Sets whether click sound is enabled
        /// </summary>
        public void SetClickEnabled(bool enabled)
        {
            playClickSound = enabled;
        }
    }

    /// <summary>
    /// Global UI audio settings that can be applied to all UIAudio components
    /// </summary>
    public class UIAudioDefaults : MonoBehaviour
    {
        private static UIAudioDefaults _instance;

        [Header("Default Sound IDs")]
        public string defaultHoverSoundID = "ui_hover";
        public string defaultClickSoundID = "ui_click";
        public string defaultBackSoundID = "ui_back";
        public string defaultErrorSoundID = "ui_error";
        public string defaultSuccessSoundID = "ui_success";

        public static UIAudioDefaults Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UIAudioDefaults>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        /// <summary>
        /// Plays the default hover sound
        /// </summary>
        public static void PlayHover()
        {
            if (Instance != null)
            {
                AudioManager.Instance?.PlayUI(Instance.defaultHoverSoundID);
            }
        }

        /// <summary>
        /// Plays the default click sound
        /// </summary>
        public static void PlayClick()
        {
            if (Instance != null)
            {
                AudioManager.Instance?.PlayUI(Instance.defaultClickSoundID);
            }
        }

        /// <summary>
        /// Plays the default back/cancel sound
        /// </summary>
        public static void PlayBack()
        {
            if (Instance != null)
            {
                AudioManager.Instance?.PlayUI(Instance.defaultBackSoundID);
            }
        }

        /// <summary>
        /// Plays the default error sound
        /// </summary>
        public static void PlayError()
        {
            if (Instance != null)
            {
                AudioManager.Instance?.PlayUI(Instance.defaultErrorSoundID);
            }
        }

        /// <summary>
        /// Plays the default success sound
        /// </summary>
        public static void PlaySuccess()
        {
            if (Instance != null)
            {
                AudioManager.Instance?.PlayUI(Instance.defaultSuccessSoundID);
            }
        }
    }

    /// <summary>
    /// Component for playing audio when a slider value changes
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class UISliderAudio : MonoBehaviour
    {
        [Header("Sound Settings")]
        [SerializeField] private string tickSoundID = "ui_tick";
        [SerializeField] private AudioClip tickClip;
        [SerializeField] private float tickCooldown = 0.05f;

        private Slider _slider;
        private float _lastTickTime;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            if (_slider != null)
            {
                _slider.onValueChanged.RemoveListener(OnValueChanged);
            }
        }

        private void OnValueChanged(float value)
        {
            if (Time.time - _lastTickTime < tickCooldown)
                return;

            _lastTickTime = Time.time;

            if (tickClip != null)
            {
                AudioManager.Instance?.PlayUIDirect(tickClip, 0.5f);
            }
            else if (!string.IsNullOrEmpty(tickSoundID))
            {
                AudioManager.Instance?.PlayUI(tickSoundID);
            }
        }
    }

    /// <summary>
    /// Component for playing audio when a toggle state changes
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class UIToggleAudio : MonoBehaviour
    {
        [Header("Sound Settings")]
        [SerializeField] private string toggleOnSoundID = "ui_toggle_on";
        [SerializeField] private string toggleOffSoundID = "ui_toggle_off";
        [SerializeField] private AudioClip toggleOnClip;
        [SerializeField] private AudioClip toggleOffClip;

        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnValueChanged);
            }
        }

        private void OnValueChanged(bool isOn)
        {
            if (isOn)
            {
                if (toggleOnClip != null)
                {
                    AudioManager.Instance?.PlayUIDirect(toggleOnClip);
                }
                else if (!string.IsNullOrEmpty(toggleOnSoundID))
                {
                    AudioManager.Instance?.PlayUI(toggleOnSoundID);
                }
            }
            else
            {
                if (toggleOffClip != null)
                {
                    AudioManager.Instance?.PlayUIDirect(toggleOffClip);
                }
                else if (!string.IsNullOrEmpty(toggleOffSoundID))
                {
                    AudioManager.Instance?.PlayUI(toggleOffSoundID);
                }
            }
        }
    }
}

