using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach this component to a bench GameObject.
/// When the player enters the interaction radius and presses W, the checkpoint
/// is registered in CheckpointManager, the player's health is fully restored,
/// and the player is snapped into a sitting pose on the bench.
/// Pressing any horizontal movement key exits the sitting state.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Save System")]
    [Tooltip("Unique identifier for this bench checkpoint. Must match across save/load.")]
    [SerializeField] private string checkpointID = "";

    [Header("Interaction")]
    [Tooltip("Radius of the CircleCollider2D trigger used to detect the player.")]
    public float interactionRadius = 1.5f;

    [Tooltip("Tag assigned to the Player GameObject.")]
    public string playerTag = "Player";

    [Header("Visual Feedback")]
    [Tooltip("Optional GameObject shown above the bench when the player is in range (e.g. a 'Press W' prompt sprite).")]
    public GameObject interactionPrompt;

    [Tooltip("Optional Animator on the bench sprite; set parameter 'isActive' to true when this checkpoint is the current one.")]
    public Animator benchAnimator;

    [Header("Sitting")]
    [Tooltip("Local offset from this bench's position where the player sprite will be placed while sitting.")]
    public Vector3 sitOffset = new Vector3(0f, 0.4f, 0f);

    private static readonly int IsActiveParam = Animator.StringToHash("isActive");

    private bool _playerInRange;
    private bool _isActive;
    private bool _isSitting;
    private PlayerMovement _cachedPlayerMovement;

    /// <summary>
    /// Fired whenever any checkpoint is activated. All other checkpoints
    /// subscribe to this to update their visual state.
    /// </summary>
    public static event System.Action<Checkpoint> CheckpointActivated;

    /// <summary>
    /// Fired when the player is already sitting and presses E.
    /// CharmMenuUI subscribes to this to open/close the charm menu.
    /// </summary>
    public static event System.Action OnCharmMenuRequested;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        SetPromptVisible(false);

        CheckpointManager.RegisterBench(checkpointID, transform.position, transform.position + sitOffset);
    }

    private void Update()
    {
        if (!_playerInRange)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            if (!_isActive)
                Activate();
            else if (!_isSitting)
                Sit();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame &&
            _cachedPlayerMovement != null && _cachedPlayerMovement.IsSitting)
        {
            OnCharmMenuRequested?.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        _playerInRange = true;
        _cachedPlayerMovement = other.GetComponent<PlayerMovement>();

        if (!_isActive)
            SetPromptVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        _playerInRange = false;
        _isSitting = false;
        _cachedPlayerMovement = null;
        SetPromptVisible(false);
    }

    private void Activate()
    {
        _isActive = true;
        SetPromptVisible(false);

        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.SetCheckpoint(checkpointID, transform.position);
        else
            Debug.LogWarning("Checkpoint: CheckpointManager not found in scene.");

        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.RestoreFullHealth();
        else
            Debug.LogWarning("Checkpoint: PlayerHealth not found in scene.");

        if (benchAnimator != null)
            benchAnimator.SetBool(IsActiveParam, true);

        CheckpointActivated?.Invoke(this);

        Sit();
    }

    /// <summary>Snaps the player into the sitting pose on this bench.</summary>
    private void Sit()
    {
        if (_cachedPlayerMovement != null)
        {
            _cachedPlayerMovement.SetSitting(true, transform.position + sitOffset);
            _isSitting = true;
            GameManager.Instance?.SaveGame();
            StalactiteManager.RespawnAll();
        }
        else
        {
            Debug.LogWarning("Checkpoint: PlayerMovement not found on player.");
        }
    }

    /// <summary>
    /// Called by other Checkpoint instances via the static event to deactivate
    /// this checkpoint's visual when a different bench is selected.
    /// </summary>
    public void OnOtherCheckpointActivated(Checkpoint activated)
    {
        if (activated == this)
            return;

        _isActive = false;
        _isSitting = false;

        if (benchAnimator != null)
            benchAnimator.SetBool(IsActiveParam, false);

        if (_playerInRange)
            SetPromptVisible(true);
    }

    private void OnEnable()
    {
        CheckpointActivated += OnOtherCheckpointActivated;
    }

    private void OnDisable()
    {
        CheckpointActivated -= OnOtherCheckpointActivated;
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(visible);
    }
}