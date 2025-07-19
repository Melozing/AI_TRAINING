using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all sound effects in the game
/// Handles SFX playback with random selection and volume control
/// </summary>
public class SFXManager : MonoBehaviour
{
    [Header("Agent SFX")]
    [SerializeField] private AudioClip[] arrAgentGoalSuccess; // Agent đẩy box thành công
    [SerializeField] private AudioClip[] arrAgentMovement; // Agent di chuyển
    [SerializeField] private AudioClip[] arrAgentPushing; // Agent đẩy thùng

    [Header("Player SFX")]
    [SerializeField] private AudioClip[] arrPlayerSkillQ; // Player dùng skill Q
    [SerializeField] private AudioClip[] arrPlayerSkillR; // Player dùng skill R
    [SerializeField] private AudioClip[] arrPlayerMovement; // Player di chuyển

    [Header("UI SFX")]
    [SerializeField] private AudioClip[] arrNPCApproach; // NPC UI hiện ra
    [SerializeField] private AudioClip[] arrNPCClose; // NPC UI tắt
    [SerializeField] private AudioClip[] arrNPCThinking; // NPC suy nghĩ
    [SerializeField] private AudioClip[] arrButtonClick; // Bấm nút

    [Header("Audio Settings")]
    [SerializeField] private float fltMasterVolume = 1f;
    [SerializeField] private bool blnShowDebugLogs = true;

    [Header("Volume Settings")]
    [Range(0f, 1f)][SerializeField] private float fltAgentGoalSuccessVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float fltAgentMovementVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float fltAgentPushingVolume = 0.7f;

    [Range(0f, 1f)][SerializeField] private float fltPlayerSkillQVolume = 0.9f;
    [Range(0f, 1f)][SerializeField] private float fltPlayerSkillRVolume = 0.9f;
    [Range(0f, 1f)][SerializeField] private float fltPlayerMovementVolume = 0.5f;

    [Range(0f, 1f)][SerializeField] private float fltNPCApproachVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] private float fltNPCCloseVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float fltNPCThinkingVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float fltButtonClickVolume = 0.8f;

    [Header("Movement Settings")]
    [SerializeField] private float fltMovementSoundInterval = 0.5f; // Thời gian giữa các âm thanh di chuyển
    [SerializeField] private float fltMinMovementSpeed = 0.5f; // Tốc độ tối thiểu để phát âm thanh di chuyển

    [Header("Skill Cooldown Settings")]
    // Cooldown được lấy trực tiếp từ PlayerController

    // Private fields
    private AudioSource objMainAudioSource;
    private AudioSource objSecondaryAudioSource; // For overlapping sounds
    private Dictionary<string, float> dictLastPlayTime = new Dictionary<string, float>();

    // Singleton pattern
    public static SFXManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeAudioSources();
    }

    /// <summary>
    /// Initialize audio sources
    /// </summary>
    private void InitializeAudioSources()
    {
        // Create main audio source for most SFX
        objMainAudioSource = gameObject.AddComponent<AudioSource>();

        // Create secondary audio source for overlapping sounds
        objSecondaryAudioSource = gameObject.AddComponent<AudioSource>();

        // Setup audio sources
        SetupAudioSource(objMainAudioSource, 1f);
        SetupAudioSource(objSecondaryAudioSource, 1f);

        if (blnShowDebugLogs)
        {
            Debug.Log("SFXManager: Initialized with 2 audio sources");
        }
    }

    /// <summary>
    /// Setup audio source properties
    /// </summary>
    /// <param name="audioSource">Audio source to setup</param>
    /// <param name="volume">Volume for this source</param>
    private void SetupAudioSource(AudioSource audioSource, float volume)
    {
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    #region Agent SFX

    /// <summary>
    /// Play agent goal success sound
    /// </summary>
    public void PlayAgentGoalSuccess()
    {
        PlayRandomSFX(arrAgentGoalSuccess, objMainAudioSource, "AgentGoalSuccess", fltAgentGoalSuccessVolume);
    }

    /// <summary>
    /// Play agent movement sound
    /// </summary>
    /// <param name="movementSpeed">Current movement speed</param>
    public void PlayAgentMovement(float movementSpeed)
    {
        if (movementSpeed < fltMinMovementSpeed) return;

        if (CanPlaySFX("AgentMovement", fltMovementSoundInterval))
        {
            PlayRandomSFX(arrAgentMovement, objMainAudioSource, "AgentMovement", fltAgentMovementVolume);
        }
    }

    /// <summary>
    /// Play agent pushing sound
    /// </summary>
    public void PlayAgentPushing()
    {
        if (CanPlaySFX("AgentPushing", 0.2f)) // Prevent spam
        {
            PlayRandomSFX(arrAgentPushing, objMainAudioSource, "AgentPushing", fltAgentPushingVolume);
        }
    }

    #endregion

    #region Player SFX

    /// <summary>
    /// Play player skill Q sound
    /// </summary>
    public void PlayPlayerSkillQ()
    {
        float cooldown = GetSkillQCooldown();
        if (CanPlaySFX("PlayerSkillQ", cooldown))
        {
            PlayRandomSFX(arrPlayerSkillQ, objSecondaryAudioSource, "PlayerSkillQ", fltPlayerSkillQVolume);
        }
    }

    /// <summary>
    /// Play player skill R sound
    /// </summary>
    public void PlayPlayerSkillR()
    {
        float cooldown = GetSkillRCooldown();
        if (CanPlaySFX("PlayerSkillR", cooldown))
        {
            PlayRandomSFX(arrPlayerSkillR, objSecondaryAudioSource, "PlayerSkillR", fltPlayerSkillRVolume);
        }
    }

    /// <summary>
    /// Play player movement sound
    /// </summary>
    /// <param name="movementSpeed">Current movement speed</param>
    public void PlayPlayerMovement(float movementSpeed)
    {
        if (movementSpeed < fltMinMovementSpeed) return;

        // PlayerController now handles footstep timing, so we don't need cooldown here
        PlayRandomSFX(arrPlayerMovement, objMainAudioSource, "PlayerMovement", fltPlayerMovementVolume);
    }

    #endregion

    #region UI SFX

    /// <summary>
    /// Play NPC approach sound
    /// </summary>
    public void PlayNPCApproach()
    {
        PlayRandomSFX(arrNPCApproach, objMainAudioSource, "NPCApproach", fltNPCApproachVolume);
    }

    /// <summary>
    /// Play NPC close sound
    /// </summary>
    public void PlayNPCClose()
    {
        PlayRandomSFX(arrNPCClose, objMainAudioSource, "NPCClose", fltNPCCloseVolume);
    }

    /// <summary>
    /// Play NPC thinking sound
    /// </summary>
    public void PlayNPCThinking()
    {
        PlayRandomSFX(arrNPCThinking, objMainAudioSource, "NPCThinking", fltNPCThinkingVolume);
    }

    /// <summary>
    /// Stop NPC thinking sound
    /// </summary>
    public void StopNPCThinking()
    {
        StopSFX("NPCThinking");
    }

    /// <summary>
    /// Play button click sound
    /// </summary>
    public void PlayButtonClick()
    {
        PlayRandomSFX(arrButtonClick, objMainAudioSource, "ButtonClick", fltButtonClickVolume);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Play random SFX from array
    /// </summary>
    /// <param name="clips">Array of audio clips</param>
    /// <param name="audioSource">Audio source to play on</param>
    /// <param name="sfxName">Name for logging</param>
    /// <param name="volume">Volume for this SFX</param>
    private void PlayRandomSFX(AudioClip[] clips, AudioSource audioSource, string sfxName, float volume)
    {
        if (clips == null || clips.Length == 0)
        {
            if (blnShowDebugLogs)
            {
                Debug.LogWarning($"SFXManager: No clips available for {sfxName}");
            }
            return;
        }

        // Select random clip
        AudioClip selectedClip = clips[Random.Range(0, clips.Length)];

        if (selectedClip != null)
        {
            // Apply volume (master volume * specific volume)
            float finalVolume = volume * fltMasterVolume;

            // Choose the best audio source to avoid conflicts
            AudioSource bestSource = ChooseBestAudioSource(sfxName);

            bestSource.PlayOneShot(selectedClip, finalVolume);

            if (blnShowDebugLogs)
            {
                Debug.Log($"SFXManager: Playing {sfxName} - {selectedClip.name} at volume {finalVolume:F2} on {bestSource.name}");
            }
        }
    }

    /// <summary>
    /// Choose the best audio source to avoid conflicts
    /// </summary>
    /// <param name="sfxName">Name of the SFX</param>
    /// <returns>Best audio source to use</returns>
    private AudioSource ChooseBestAudioSource(string sfxName)
    {
        // Use secondary source for skills and important sounds to avoid conflicts
        if (sfxName.Contains("Skill") || sfxName.Contains("Goal") || sfxName.Contains("Button"))
        {
            return objSecondaryAudioSource;
        }

        // Use main source for movement and ambient sounds
        return objMainAudioSource;
    }

    /// <summary>
    /// Get Skill Q cooldown from PlayerController
    /// </summary>
    /// <returns>Cooldown time for Skill Q</returns>
    private float GetSkillQCooldown()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            return playerController.GetSkillQCooldown();
        }

        if (blnShowDebugLogs)
        {
            Debug.LogWarning("SFXManager: PlayerController not found, using default cooldown of 1s for Skill Q");
        }
        return 1.0f; // Default fallback
    }

    /// <summary>
    /// Get Skill R cooldown from PlayerController
    /// </summary>
    /// <returns>Cooldown time for Skill R</returns>
    private float GetSkillRCooldown()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            return playerController.GetSkillRCooldown();
        }

        if (blnShowDebugLogs)
        {
            Debug.LogWarning("SFXManager: PlayerController not found, using default cooldown of 5s for Skill R");
        }
        return 5.0f; // Default fallback
    }

    /// <summary>
    /// Check if enough time has passed to play SFX again
    /// </summary>
    /// <param name="sfxName">Name of the SFX</param>
    /// <param name="minInterval">Minimum time between plays</param>
    /// <returns>True if can play</returns>
    private bool CanPlaySFX(string sfxName, float minInterval)
    {
        if (!dictLastPlayTime.ContainsKey(sfxName))
        {
            dictLastPlayTime[sfxName] = 0f;
            return true;
        }

        if (Time.time - dictLastPlayTime[sfxName] >= minInterval)
        {
            dictLastPlayTime[sfxName] = Time.time;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Set master volume
    /// </summary>
    /// <param name="volume">New volume (0-1)</param>
    public void SetMasterVolume(float volume)
    {
        fltMasterVolume = Mathf.Clamp01(volume);

        if (blnShowDebugLogs)
        {
            Debug.Log($"SFXManager: Master volume set to {fltMasterVolume:F2}");
        }
    }

    /// <summary>
    /// Stop all SFX
    /// </summary>
    public void StopAllSFX()
    {
        if (objMainAudioSource != null)
            objMainAudioSource.Stop();
        if (objSecondaryAudioSource != null)
            objSecondaryAudioSource.Stop();
    }

    /// <summary>
    /// Stop specific SFX by name
    /// </summary>
    /// <param name="sfxName">Name of the SFX to stop</param>
    public void StopSFX(string sfxName)
    {
        // Only stop NPC thinking sound - let other sounds finish naturally
        if (sfxName == "NPCThinking")
        {
            if (objMainAudioSource != null && objMainAudioSource.isPlaying)
            {
                objMainAudioSource.Stop();

                if (blnShowDebugLogs)
                {
                    Debug.Log($"SFXManager: Stopped {sfxName}");
                }
            }
        }
        else
        {
            if (blnShowDebugLogs)
            {
                Debug.Log($"SFXManager: Letting {sfxName} finish naturally");
            }
        }
    }

    #endregion

    #region Volume Control Methods

    /// <summary>
    /// Set volume for specific SFX type
    /// </summary>
    /// <param name="sfxType">Type of SFX</param>
    /// <param name="volume">New volume (0-1)</param>
    public void SetSFXVolume(string sfxType, float volume)
    {
        volume = Mathf.Clamp01(volume);

        switch (sfxType.ToLower())
        {
            case "agentgoalsuccess":
                fltAgentGoalSuccessVolume = volume;
                break;
            case "agentmovement":
                fltAgentMovementVolume = volume;
                break;
            case "agentpushing":
                fltAgentPushingVolume = volume;
                break;
            case "playerskillq":
                fltPlayerSkillQVolume = volume;
                break;
            case "playerskillr":
                fltPlayerSkillRVolume = volume;
                break;
            case "playermovement":
                fltPlayerMovementVolume = volume;
                break;
            case "npcapproach":
                fltNPCApproachVolume = volume;
                break;
            case "npcclose":
                fltNPCCloseVolume = volume;
                break;
            case "npcthinking":
                fltNPCThinkingVolume = volume;
                break;
            case "buttonclick":
                fltButtonClickVolume = volume;
                break;
            default:
                Debug.LogWarning($"SFXManager: Unknown SFX type '{sfxType}'");
                return;
        }

        if (blnShowDebugLogs)
        {
            Debug.Log($"SFXManager: {sfxType} volume set to {volume:F2}");
        }
    }

    /// <summary>
    /// Get current volume for specific SFX type
    /// </summary>
    /// <param name="sfxType">Type of SFX</param>
    /// <returns>Current volume</returns>
    public float GetSFXVolume(string sfxType)
    {
        switch (sfxType.ToLower())
        {
            case "agentgoalsuccess":
                return fltAgentGoalSuccessVolume;
            case "agentmovement":
                return fltAgentMovementVolume;
            case "agentpushing":
                return fltAgentPushingVolume;
            case "playerskillq":
                return fltPlayerSkillQVolume;
            case "playerskillr":
                return fltPlayerSkillRVolume;
            case "playermovement":
                return fltPlayerMovementVolume;
            case "npcapproach":
                return fltNPCApproachVolume;
            case "npcclose":
                return fltNPCCloseVolume;
            case "npcthinking":
                return fltNPCThinkingVolume;
            case "buttonclick":
                return fltButtonClickVolume;
            default:
                Debug.LogWarning($"SFXManager: Unknown SFX type '{sfxType}'");
                return 0f;
        }
    }

    #endregion
}