using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    public Rigidbody2D rb;
    public Animator animator;
    bool isFacingRight = true;
    public ParticleSystem smokeFX;

    [Header("Movement")]
    public float moveSpeed = 5f;
    float horizontalMovement;

    [Header("Jumping")]
    public float jumpPower = 10f;
    public int maxJumps = 1;
    private int jumpsRemaining;
    public float jumpCutMultiplier = 0.35f;
    public float lowJumpGravityMult = 2f;
    private bool isJumpHeld;

    [Header("GroundCheck")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    public LayerMask groundLayer;
    bool isGrounded;
    bool wasGrounded;

    [Header("WallCheck")]
    public Transform wallCheckPos;
    public Vector2 wallCheckSize = new Vector2(0.49f, 0.03f);
    public LayerMask wallLayer;

    [Header("WallMovement")]
    public float wallSlideSpeed = 2;
    bool isWallSliding;

    float wallJumpTime = 0.5f;
    float wallJumpTimer;
    public Vector2 wallJumpPower = new Vector2(5f, 10f);
    public float wallJumpCooldownDuration = 0.2f;
    private float _wallJumpCooldown = 0f;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 18f;
    public float fallGravityMult = 2f;

    [Header("Attack")]
    public int attackDamage = 1;
    [SerializeField] private float attackKnockbackForce = 3f;
    public Collider2D attackHitbox;
    public Transform slashEffectTransform;
    public Animator slashEffectAnimator;
    public float attackDuration = 0.43f;
    public float downAttackDuration = 0.43f;

    public Vector2 normalHitboxOffset = new Vector2(0.347f, -0.222f);
    public Vector2 downHitboxOffset = new Vector2(0f, -0.6f);
    public Vector2 normalSlashEffectOffset = new Vector2(0.43f, -0.18f);
    public Vector2 downSlashEffectOffset = new Vector2(0f, -0.6f);
    public float normalSlashEffectRotation = 0f;
    public float downSlashEffectRotation = -90f;

    [Header("Spike Bounce")]
    public float spikeBounceForce = 14f;

    [Header("Unlockable Abilities")]
    public bool doubleJumpUnlocked = false;
    public bool wallJumpUnlocked = false;
    public bool dashUnlocked = false;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;

    [SerializeField] private GameObject dashEffect;

    private bool _isDashing;
    private bool _canDash = true;

    bool isDownAttacking;
    bool isAttacking;
    private bool _attackQueued;
    [SerializeField] private float comboWindow = 0.3f;
    private bool _isComboQueued;
    private bool _attack2Running;
    private int _comboStep;
    private readonly HashSet<int> _hitTargetsThisSwing = new HashSet<int>();

    private bool _isGameplayBlocked;
    private bool _isSitting;

    [Header("Hard Landing")]
    [SerializeField] float hardLandingThreshold = 6f;

    private float _fallStartY;
    private bool _isHardLanding;
    private bool _inputEnabled;

    private static readonly int IsSittingParam    = Animator.StringToHash("isSitting");
    private static readonly int HardLandingParam  = Animator.StringToHash("hardLanding");

    public Vector3 LastGroundedPosition { get; private set; }
    public bool IsGrounded => isGrounded;
    public bool IsSitting  => _isSitting;
    public bool IsDashing  => _isDashing;

    private void Awake()
    {
        Instance = this;
        _inputEnabled = false;
    }

    void Update()
    {
        if (_isSitting)
        {
            if (CharmMenuUI.Instance != null && CharmMenuUI.Instance.IsOpen)
                return;

            if (horizontalMovement != 0f)
                ExitSitting();

            return;
        }

        if (_isDashing)
            return;

        GroundCheck();
        ProcessGravity();

        if (wallJumpUnlocked)
        {
            ProcessWallSlide();
            ProcessWallJump();
        }

        rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
        Flip();

        animator.SetFloat("yVelocity", isGrounded ? 0f : rb.linearVelocity.y);
        animator.SetFloat("magnitude", Mathf.Abs(horizontalMovement));
        animator.SetBool("isWallSliding", isWallSliding);
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (!_inputEnabled)      return;
        if (_isGameplayBlocked)  return;

        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    /// <summary>
    /// Legat în Player Input component → acțiunea "Dash".
    /// Adaugă binding pe Left Shift în .inputactions asset.
    /// </summary>
    public void Dash(InputAction.CallbackContext context)
    {
        if (!context.performed)                             return;
        if (!_inputEnabled)                                 return;
        if (!dashUnlocked)                                  return;
        if (_isGameplayBlocked || _isSitting || _isDashing) return;
        if (!_canDash)                                      return;
        if (horizontalMovement == 0f)                       return;

        StartCoroutine(PerformDash());
    }

    public void SetGameplayBlocked(bool blocked)
    {
        _isGameplayBlocked = blocked;

        if (blocked)
            horizontalMovement = 0f;
    }

    public void RefreshJumps()
    {
        jumpsRemaining = doubleJumpUnlocked ? 2 : 1;
    }

    public void SetSitting(bool sitting, Vector3 sitPosition = default)
    {
        if (sitting)
        {
            _isSitting = true;
            _inputEnabled = true;
            transform.position = sitPosition;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            animator.SetBool(IsSittingParam, true);
            animator.SetFloat("magnitude", 0f);
            animator.SetFloat("yVelocity", 0f);
            animator.SetBool("isWallSliding", false);
        }
        else
        {
            ExitSitting();
        }
    }

    private void ExitSitting()
    {
        _isSitting = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        animator.SetBool(IsSittingParam, false);
        horizontalMovement = 0f;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (!_inputEnabled)      return;
        if (_isGameplayBlocked)  return;

        if (_isSitting)
            return;

        if (_isDashing)
            return;

        if (context.performed)
        {
            if (wallJumpUnlocked && wallJumpTimer > 0f)
            {
                rb.linearVelocity = new Vector2(0f, wallJumpPower.y);
                wallJumpTimer = 0f;
                _wallJumpCooldown = wallJumpCooldownDuration;
                isJumpHeld = true;
                JumpFX();
            }
            else if (jumpsRemaining > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
                jumpsRemaining--;
                isJumpHeld = true;
                JumpFX();
            }
        }

        if (context.canceled)
        {
            isJumpHeld = false;
            if (rb.linearVelocity.y > 0)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private IEnumerator PerformDash()
    {
        _canDash = false;
        _isDashing = true;

        float dashDirection = isFacingRight ? 1f : -1f;

        SpawnDashTrail(dashDirection);

        if (dashEffect != null)
        {
            dashEffect.SetActive(true);
            dashEffect.transform.localScale = dashDirection > 0
                ? new Vector3(1f, 1f, 1f)
                : new Vector3(-1f, 1f, 1f);
            foreach (var ps in dashEffect.GetComponentsInChildren<ParticleSystem>())
                ps.Play();
        }

        if (!isGrounded) animator.SetBool("isDashing", true);

        rb.linearVelocity = new Vector2(dashDirection * dashSpeed * 1.3f, 0f);
        rb.gravityScale = 0f;

        yield return new WaitForSeconds(dashDuration);

        animator.SetBool("isDashing", false);

        if (dashEffect != null)
            dashEffect.SetActive(false);

        _isDashing = false;
        rb.gravityScale = baseGravity;

        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }

    private void SpawnDashTrail(float direction)
    {
        var go = new GameObject("DashTrail");
        go.transform.position = transform.position;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration        = 0.15f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startColor      = new ParticleSystem.MinMaxGradient(Color.white);
        main.gravityModifier = 0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.enabled = false;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x       = new ParticleSystem.MinMaxCurve(-direction * 2f);

        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        var gradCurve = new ParticleSystem.MinMaxGradient(grad);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color   = gradCurve;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.material       = new Material(Shader.Find("Sprites/Default"));
        rend.material.color = Color.white;

        // Second smaller burst as a child system with its own properties.
        var childGo = new GameObject("DashTrailSmall");
        childGo.transform.SetParent(go.transform, false);

        var ps2 = childGo.AddComponent<ParticleSystem>();
        ps2.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main2 = ps2.main;
        main2.duration        = 0.15f;
        main2.loop            = false;
        main2.startLifetime   = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main2.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 2f);
        main2.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.08f);
        main2.startColor      = new ParticleSystem.MinMaxGradient(Color.white);
        main2.gravityModifier = 0.2f;
        main2.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission2 = ps2.emission;
        emission2.enabled      = true;
        emission2.rateOverTime = 0f;
        emission2.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        var shape2 = ps2.shape;
        shape2.enabled = false;

        var vel2 = ps2.velocityOverLifetime;
        vel2.enabled = true;
        vel2.x       = new ParticleSystem.MinMaxCurve(-direction * 2f);

        var col2 = ps2.colorOverLifetime;
        col2.enabled = true;
        col2.color   = gradCurve;

        var rend2 = ps2.GetComponent<ParticleSystemRenderer>();
        rend2.material       = new Material(Shader.Find("Sprites/Default"));
        rend2.material.color = Color.white;

        ps.Play();
        ps2.Play();
        Destroy(go, 0.5f);
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (!_inputEnabled)      return;
        if (_isGameplayBlocked)  return;
        if (_isSitting)          return;
        if (_isDashing)          return;
        if (!context.performed)  return;
        if (isDownAttacking)     return;
        if (_attack2Running)     return;

        if (_comboStep == 0 && !isAttacking && !_attack2Running)
        {
            bool sHeld = Keyboard.current != null && Keyboard.current.sKey.isPressed;
            bool downArrowHeld = Keyboard.current != null && Keyboard.current.downArrowKey.isPressed;
            bool downAttackRequested = !isGrounded && (sHeld || downArrowHeld);

            if (downAttackRequested)
                StartCoroutine(PerformDownAttack());
            else
            {
                _comboStep = 1;
                StartCoroutine(PerformAttack());
            }
        }
        else if (_comboStep == 1)
        {
            _isComboQueued = true;
        }
        // _comboStep == 2: ignore input during Attack2
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        Debug.Log("A1 START, isAttacking=" + isAttacking);
        _hitTargetsThisSwing.Clear();
        animator.SetBool("isAttacking", true);

        SetHitboxPosition(normalHitboxOffset);
        SetSlashEffectPosition(normalSlashEffectOffset);

        if (slashEffectAnimator != null)
            slashEffectAnimator.SetBool("isAttacking", true);

        if (attackHitbox != null)
        {
            attackHitbox.tag = "Attack";
            attackHitbox.enabled = true;
        }

        yield return new WaitForSeconds(attackDuration);

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        animator.SetBool("isAttacking", false);

        if (slashEffectAnimator != null)
            slashEffectAnimator.SetBool("isAttacking", false);

        Debug.Log("A1 END");
        if (_isComboQueued)
        {
            Debug.Log("A1->A2 DIRECT");
            _isComboQueued = false;
            _comboStep = 2;
            _attack2Running = true;
            StartCoroutine(PerformAttack2());
        }
        else
        {
            StartCoroutine(ComboWindowRoutine());
        }
    }

    private IEnumerator ComboWindowRoutine()
    {
        Debug.Log("WINDOW START");
        yield return new WaitForSeconds(comboWindow);

        if (_isComboQueued)
        {
            Debug.Log("WINDOW->A2");
            _isComboQueued = false;
            _comboStep = 2;
            _attack2Running = true;
            StartCoroutine(PerformAttack2());
        }
        else
        {
            isAttacking = false;
            _comboStep = 0;
        }
    }

    private IEnumerator PerformAttack2()
    {
        Debug.Log("A2 START, _attack2Running=" + _attack2Running);
        _comboStep = 2;
        _hitTargetsThisSwing.Clear();
        animator.SetBool("isAttacking2", true);

        if (slashEffectAnimator != null)
            slashEffectAnimator.SetBool("isAttacking2", true);

        SetHitboxPosition(normalHitboxOffset);

        if (attackHitbox != null)
        {
            attackHitbox.tag = "Attack";
            attackHitbox.enabled = true;
        }

        yield return new WaitForSeconds(attackDuration);

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        animator.SetBool("isAttacking2", false);

        if (slashEffectAnimator != null)
            slashEffectAnimator.SetBool("isAttacking2", false);

        Debug.Log("A2 END");
        isAttacking = false;
        _attack2Running = false;
        _comboStep = 0;
        _isComboQueued = false;
    }

    private IEnumerator PerformDownAttack()
    {
        _hitTargetsThisSwing.Clear();
        isDownAttacking = true;
        animator.SetBool("isDownAttacking", true);

        SetHitboxPosition(downHitboxOffset);
        SetSlashEffectPosition(downSlashEffectOffset);
        SetSlashEffectRotation(downSlashEffectRotation);

        if (slashEffectAnimator != null)
            slashEffectAnimator.SetBool("isDownAttacking", true);

        if (attackHitbox != null)
        {
            attackHitbox.tag = "DownAttack";
            attackHitbox.enabled = true;
        }

        yield return new WaitForSeconds(downAttackDuration);

        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
            attackHitbox.tag = "Attack";
        }

        SetHitboxPosition(normalHitboxOffset);
        SetSlashEffectPosition(normalSlashEffectOffset);
        SetSlashEffectRotation(normalSlashEffectRotation);

        animator.SetBool("isDownAttacking", false);

        if (slashEffectAnimator != null)
            slashEffectAnimator.SetBool("isDownAttacking", false);

        isDownAttacking = false;
    }

    private void SetHitboxPosition(Vector2 localPos)
    {
        if (attackHitbox != null)
            attackHitbox.transform.localPosition = localPos;
    }

    private void SetSlashEffectPosition(Vector2 localPos)
    {
        if (slashEffectTransform != null)
            slashEffectTransform.localPosition = localPos;
    }

    private void SetSlashEffectRotation(float zDegrees)
    {
        if (slashEffectTransform != null)
            slashEffectTransform.localRotation = Quaternion.Euler(0f, 0f, zDegrees);
    }

    private void JumpFX()
    {
        animator.SetTrigger("jump");
        smokeFX.Play();
    }

    private void GroundCheck()
    {
        wasGrounded = isGrounded;

        Collider2D groundHit = Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);
        if (groundHit != null)
        {
            jumpsRemaining = doubleJumpUnlocked ? 2 : 1;
            isGrounded = true;

            if (!wasGrounded) // just landed this frame
            {
                if (!_inputEnabled)
                {
                    // First landing — always play the routine; it sets _inputEnabled when done.
                    if (!_isHardLanding)
                        StartCoroutine(HardLandingRoutine());
                }
                else
                {
                    float fallDist = _fallStartY - transform.position.y;
                    if (fallDist > hardLandingThreshold && !_isHardLanding)
                        StartCoroutine(HardLandingRoutine());
                }
            }

            if (groundHit.GetComponent<FallingPlatform>() == null)
                LastGroundedPosition = transform.position;
        }
        else
        {
            int maxRemaining = doubleJumpUnlocked ? 2 : 1;
            if (wasGrounded && jumpsRemaining == maxRemaining)
                jumpsRemaining--;
            isGrounded = false;

            if (wasGrounded) // just left ground this frame — record fall start
                _fallStartY = transform.position.y;
        }
    }

    private bool WallCheck()
    {
        return Physics2D.OverlapBox(wallCheckPos.position, wallCheckSize, 0, wallLayer);
    }

    private void ProcessGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = baseGravity * fallGravityMult;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
        }
        else if (rb.linearVelocity.y > 0 && !isJumpHeld)
        {
            rb.gravityScale = baseGravity * lowJumpGravityMult;
        }
        else
        {
            rb.gravityScale = baseGravity;
        }
    }

    private void ProcessWallSlide()
    {
        if (_wallJumpCooldown > 0f)
        {
            _wallJumpCooldown -= Time.deltaTime;
            isWallSliding = false;
            return;
        }

        if (!isGrounded && WallCheck() && horizontalMovement != 0)
        {
            isWallSliding = true;
            jumpsRemaining = Mathf.Max(jumpsRemaining, 1);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void ProcessWallJump()
    {
        if (isWallSliding)
        {
            wallJumpTimer = wallJumpTime;
        }
        else if (wallJumpTimer > 0f)
        {
            wallJumpTimer -= Time.deltaTime;
        }
    }

    public bool TryRegisterHit(int instanceID)
    {
        if (_hitTargetsThisSwing.Contains(instanceID)) return false;
        _hitTargetsThisSwing.Add(instanceID);
        return true;
    }

    public void ApplyAttackKnockback(Vector3 targetPosition)
    {
        float dir = transform.position.x < targetPosition.x ? -1f : 1f;
        rb.linearVelocity = new Vector2(dir * attackKnockbackForce, rb.linearVelocity.y);
    }

    public void BounceOffSpike()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, spikeBounceForce);
        jumpsRemaining = doubleJumpUnlocked ? 2 : 1;
    }

    private void Flip()
    {
        if (isFacingRight && horizontalMovement < 0 || !isFacingRight && horizontalMovement > 0)
        {
            isFacingRight = !isFacingRight;
            Vector3 ls = transform.localScale;
            ls.x *= -1f;
            transform.localScale = ls;

            if (rb.linearVelocity.y == 0)
                smokeFX.Play();
        }
    }

    private IEnumerator HardLandingRoutine()
    {
        _isHardLanding = true;
        SetGameplayBlocked(true);
        animator.SetTrigger(HardLandingParam);

        // Wait two frames for the Animator to transition into the HardLanding state.
        yield return null;
        yield return null;

        while (animator.GetCurrentAnimatorStateInfo(0).IsName("HardLanding"))
            yield return null;

        _inputEnabled = true;
        SetGameplayBlocked(false);
        _isHardLanding = false;
    }

    private void OnDrawGizmos()
    {
        if (attackHitbox == null) return;
        Gizmos.color = Color.cyan;
        if (attackHitbox is BoxCollider2D box)
        {
            Vector3 worldCenter = attackHitbox.transform.TransformPoint(
                new Vector3(box.offset.x, box.offset.y, 0f));
            Vector3 worldSize = new Vector3(
                Mathf.Abs(box.size.x * attackHitbox.transform.lossyScale.x),
                Mathf.Abs(box.size.y * attackHitbox.transform.lossyScale.y),
                0.1f);
            Gizmos.DrawWireCube(worldCenter, worldSize);
        }
        else
        {
            Gizmos.DrawWireSphere(attackHitbox.transform.position, 0.25f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(wallCheckPos.position, wallCheckSize);
    }
}