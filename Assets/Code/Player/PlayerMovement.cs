using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Sonidos")]

    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hurtSound;


    //Lamar al control de animaciones y el rigidbody
    private Rigidbody2D rb;
    private PlayerAnimationController animController;
//Creo que todos los Headers dejan bastante en claro para que sirve cada cosa, asi que me ahorro de explicar
//Lo mas complicado 
    [Header("Puntos de Detección")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Prefabs y Capas")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    // ============================================
    // MOVIMIENTO
    // ============================================
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float blockMoveSpeed = 2f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private bool isSprintMode = true;

    private float horizontalInput;
    private bool facingRight = true;

    // ============================================
    // SALTO
    // ============================================
    [Header("Salto")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float doubleJumpForce = 10f;
    [SerializeField] private float fallMultiplier = 3f; //Cuando se deja de ascender se aplica esta fuerza
    [SerializeField] private float lowJumpMultiplier = 2.5f; //Si el salto es muy bajo la gravedad tambien, se duplica
    [SerializeField] private float coyoteTime = 0.15f; //Cuando el jugador sale de isGrounded, este contador le permite saltar
    //[SerializeField] private float jumpBufferTime = 0.1f;  //para que el jugador pueda saltar un momento antes de aterrizar
    //[SerializeField] private float wallJumpCooldown = 0.3f; //cooldown para que no se buguee el salto en pared

    private float coyoteTimeCounter;
    private bool hasDoubleJumped;



    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    private bool isDashing;
    private float dashTimer;
    private float lastDashTime = -10f;
    //Sinceramente todos estos cambios dejaron de funcionar desde que me arreglaron un bug en donde el personaje se quedaba pegado
    //En teoria lo que deberia hacer esta seccion era poder pegarte a la pared y caer mas lento, y bajar mas rapido con un boton
    //Desde que el jugador no se pega a las paredes, este cambio no es necesario y solo se dejo 
    [Header("Interacción con Paredes")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallSlideFastSpeed = 6f;
    [SerializeField] private float wallJumpForceX = 8f;  
    [SerializeField] private float wallJumpForceY = 15f; 
    [SerializeField] private float wallGravity = 0.5f;

    private float originalGravity;
    private bool isFastWallSliding;
    private bool isWallSliding;

    [Header("Combate")]
    [SerializeField] private float attackStepSpeed = 5f; //El jugador en el frame que saca la espada es empujado hacia delante
    [SerializeField] private float attackStepDelay = 0.15f; //Este es el delay que tiene antes de recibir el empuje
    [SerializeField] private float attackStepDuration = 0.1f; // y su duracion
    [SerializeField] private float attackGroundDuration = 0.4f; 
    [SerializeField] private float attackAirDuration = 0.4f;
    [SerializeField] private float attackCooldown = 0.1f; //Cooldown entre ataques para evitar bugs visuales (ataque infinito)
    [SerializeField] private float knockbackForce = 10f; // fuerza de empuje
    [SerializeField] private float damageRecoveryTime = 0.5f;
    //estas variables se usan a lo largo del codigo 
    private int currentCombo;
    private bool isAttacking;
    private float attackDelayTimer;
    private float attackMoveTimer;
    private bool attackStepActive;
    private float lastAttackTime = -10f;
    //Proyectiles ya colocados pero aun no implementados
    [Header("Proyectiles")]
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileCooldown = 0.5f;

    private float lastProjectileTime;
    //Habilidades usables en los dos personajes 
    [Header("Habilidades Desbloqueables")]
    public bool canMove = true;
    public bool canJump = true;
    public bool canDoubleJump = true;
    public bool canAttack = true;
    public bool canDash = true;
    public bool canWallCling = true;
    public bool canBlock = true;
    public bool canThrowProjectile = true;

    [Header("Detección")]
    [SerializeField] private float groundCheckRay = 0.2f;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float inputDeadzone = 0.1f;

    private bool isGrounded;
    private bool isTouchingWall;
    private bool wasGrounded;


    [SerializeField] private bool isTakingDamage;


    private void Start()
    {
        InitializeComponents();
    }

    private void Update()
    {
        if (isTakingDamage) return;

        CaptureInput();
        UpdateDetectionStates();
        UpdateJumpTimers();
        UpdateAttackStepTimer();

        if (!isDashing)
        {
            HandleAllActions();
        }
        else
        {
            UpdateDashTimer();
        }

        ApplyBetterFalling();
    }
    //fisicas en el escudo, al correr, el empuje de ataque y movimiento
    private void FixedUpdate()
    {
        if (isTakingDamage) return;

        bool isBlocking = Input.GetKey(KeyCode.X);
        bool isHoldingCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // Empuje de ataque con un poco de delay
        if (attackStepActive && attackMoveTimer > 0)
        {
            float direction = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * attackStepSpeed, rb.linearVelocity.y);
            attackMoveTimer -= Time.fixedDeltaTime;

            if (attackMoveTimer <= 0)
            {
                attackStepActive = false;
            }
        }
        // Movimiento normal con sprint/slow
        else if (!isDashing && canMove && !isAttacking && !isBlocking)
        {
            float finalSpeed = moveSpeed;

            if (isHoldingCtrl)
            {
                if (isSprintMode)
                {
                    finalSpeed = moveSpeed * sprintMultiplier;
                }
                else
                {
                    finalSpeed = moveSpeed / sprintMultiplier;
                }
            }

            ApplyMovement(horizontalInput * finalSpeed);

        }
        // Bloqueo con movimiento lento
        else if (isBlocking && !isAttacking)
        {
            ApplyMovement(horizontalInput * blockMoveSpeed);
        }
        else if (isAttacking && !attackStepActive)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
    //inicializar los componentes necesarios
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        animController = GetComponent<PlayerAnimationController>();

        if (animController == null)
        {
            animController = gameObject.AddComponent<PlayerAnimationController>();
        }

        animController.Initialize(this);
        originalGravity = rb.gravityScale;
    }

    //Agarra el imput horizontal del jugador

    private void CaptureInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
    }

    //Detectar suelo y pared con raycasts simples
    private void UpdateDetectionStates()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckRay, groundLayer);

        // Detectar pared - SUPER SIMPLE
        Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
        isTouchingWall = Physics2D.Raycast(wallCheck.position, wallDirection, wallCheckDistance, wallLayer);

        // Reset de doble salto y fast slide
        if (isGrounded && !wasGrounded)
        {
            hasDoubleJumped = false;
            isFastWallSliding = false;
        }
    }

    private void UpdateJumpTimers()
    {
        coyoteTimeCounter = isGrounded ? coyoteTime : coyoteTimeCounter - Time.deltaTime;

    }
    // Maneja todas las acciones del jugador segun las habilidades desbloqueadas
    private void HandleAllActions()
    {
        if (canMove) HandleMovement();
        if (canWallCling) HandleWallCling();
        if (canJump) HandleJump();
        if (canAttack) HandleAttack();
        if (canDash) HandleDash();
        if (canThrowProjectile) HandleProjectile();
    }

    //Todo sobre movimiento basico y flip del personaje

    private void HandleMovement()
    {
        if (isAttacking) return;
        FlipCharacter(horizontalInput);
       
       
    }

    private void ApplyMovement(float speed)
    {
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

       
    }

    private void FlipCharacter(float direction)
    {
        if (direction > 0 && !facingRight)
        {
            Flip();
        }
        else if (direction < 0 && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    //Salto aplicable a tierra y pared, con coyote time y buffer de salto, mide levemente la distancia de salto
    //Si saltas muy bajo se aplica low jump, si estas cayendo por mucho tiempo se aplica fall multiplier

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //salto basico, si no estas tocando el suelo no podes saltar
            //pero si tenes coyote time o estas tocando la pared podes saltar
            if (isTouchingWall && !isGrounded)
            {
                WallJump();
            }

            else if (isGrounded || coyoteTimeCounter > 0f)
            {
                PerformJump(jumpForce);
                hasDoubleJumped = false;
                coyoteTimeCounter = 0f;
            }

            else if (!isGrounded && !hasDoubleJumped && canDoubleJump)
            {
                PerformDoubleJump();
            }
        }
    }

    private void PerformJump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    private void PerformDoubleJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * doubleJumpForce, ForceMode2D.Impulse);
        hasDoubleJumped = true;
        if (animController != null)
        {
            animController.TriggerDoubleJump();
        }
    }

    // Wall Jump
    private void WallJump()
    {
        
        float jumpDirX = facingRight ? -1f : 1f;

        rb.linearVelocity = new Vector2(jumpDirX * wallJumpForceX, wallJumpForceY);

        Flip();

        hasDoubleJumped = false;

        isWallSliding = false;
        rb.gravityScale = originalGravity;

    }

    private void ApplyBetterFalling()
    {
        // No aplicar si estamos en wall slide
        if (isWallSliding) return;

        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallMultiplier;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rb.gravityScale = lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = originalGravity;
        }
    }


    //Dash, que hace ese codigo? una fuerza horizontal super rapida por un tiempo corto
    //todo esto marcado en los valores publicos

    private void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time > lastDashTime + dashCooldown)
        {
            isDashing = true;
            dashTimer = dashDuration;
            lastDashTime = Time.time;
        }

        if (isDashing)
        {
            float dashDirection = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);
        }
    }

    private void UpdateDashTimer()
    {
        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0)
        {
            isDashing = false;
        }
    }

 //Wall Cling lo mas basico posible pero funcional

    private void HandleWallCling()
    {

        bool isPressingTowardsWall = Mathf.Abs(horizontalInput) > inputDeadzone &&
                                     Mathf.Sign(horizontalInput) == (facingRight ? 1 : -1);


        isWallSliding = !isGrounded && isTouchingWall && isPressingTowardsWall;

        if (isWallSliding)
        {

            hasDoubleJumped = false;


            if (isFastWallSliding)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideFastSpeed);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }

            rb.gravityScale = wallGravity;
        }
        else
        {
            if (rb.gravityScale == wallGravity)
            {
                rb.gravityScale = originalGravity;
            }
            isFastWallSliding = false;
        }
    }

 //Todo relacionado al combate

    private void UpdateAttackStepTimer()
    {
        if (isAttacking && attackDelayTimer > 0)
        {
            attackDelayTimer -= Time.deltaTime;

            if (attackDelayTimer <= 0 && !attackStepActive)
            {
                attackStepActive = true;
                attackMoveTimer = attackStepDuration;
            }
        }
    }

    private void HandleAttack()
    {
        if (isAttacking || Time.time < lastAttackTime + attackCooldown) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (isGrounded)
            {
                currentCombo = (currentCombo == 1) ? 2 : 1;
                StartAttack(currentCombo);
            }
            else
            {
                StartAttack(1);
            }
           
            AudioManager.Instance.PlaySFX(attackSound, 0.1f, 0.5f);
        }
    }

    private void StartAttack(int comboIndex)
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animController.SetComboIndex(comboIndex);

        attackDelayTimer = attackStepDelay;
        attackStepActive = false;
        attackMoveTimer = 0;

        float duration = isGrounded ? attackGroundDuration : attackAirDuration;
        StartCoroutine(AttackCoroutine(duration));
    }

    private IEnumerator AttackCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        StopAttack();
    }

    private void StopAttack()
    {
        isAttacking = false;
        attackDelayTimer = 0;
        attackMoveTimer = 0;
        attackStepActive = false;
    }

    public void OnAttackHitFrame()
    {
        // El empuje se maneja en FixedUpdate
    }
    //Cuando se recibe el daño se saca de donde y se le aplica una fuerza en contra para ser empujado
    public void TakeDamage(Vector2 attackerPosition)
    {
        // Asegurar que los componentes estén inicializados
        if (rb == null || animController == null)
        {
            InitializeComponents();
        }

        isTakingDamage = true;

        if (animController != null)
        {
            animController.TriggerDamage();
        }

        Vector2 knockbackDirection = ((Vector2)transform.position - attackerPosition).normalized;
        float minKnockbackY = 0.5f;
        float maxKnockbackY = 1f;
        knockbackDirection.y = Mathf.Clamp(knockbackDirection.y + minKnockbackY, minKnockbackY, maxKnockbackY);

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        //hola
        StartCoroutine(DamageRecoveryCoroutine());

        if (hurtSound != null)
        {
            float randomPitch = Random.Range(0.9f, 1.1f);
            AudioManager.Instance.PlaySFX(hurtSound, 1f, randomPitch);
        }
    }

    private IEnumerator DamageRecoveryCoroutine()
    {
        yield return new WaitForSeconds(damageRecoveryTime);
        isTakingDamage = false;
        animController.StopDamage();
    }

   //Lanzar arrojables (todavia no setupeado, pero por lo menos esta en el codigo)

    private void HandleProjectile()
    {
        if (Input.GetKeyDown(KeyCode.C) && Time.time > lastProjectileTime + projectileCooldown)
        {
            if (projectilePrefab != null && projectileSpawnPoint != null)
            {
                lastProjectileTime = Time.time;

                GameObject projectile = Instantiate(projectilePrefab,
                    projectileSpawnPoint.position, Quaternion.identity);

                Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
                if (projectileRb != null)
                {
                    float direction = facingRight ? 1f : -1f;
                    projectileRb.linearVelocity = new Vector2(direction * projectileSpeed, 0);
                }

                animController.TriggerThrow();
            }
        }
    }

    //Propiedades publicas de estado para el animator u otros scripts

    public bool IsGrounded => isGrounded;
    public bool IsTouchingWall => isTouchingWall;
    public bool IsAttacking => isAttacking;
    public bool IsDashing => isDashing;
    public bool IsTakingDamage => isTakingDamage;
    public float HorizontalInput => horizontalInput;
    public float VerticalVelocity => rb.linearVelocity.y;
    public bool IsBlocking => Input.GetKey(KeyCode.X);
    public bool IsWallSliding => isWallSliding;
    public bool IsSprinting => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

    //Gizmos para debug de raycasts

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Vector2 origin = groundCheck.position;
            Vector2 direction = Vector2.down;
            float distance = groundCheckRay;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, groundLayer);
            Gizmos.color = hit.collider != null ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + direction * distance);
        }

        if (wallCheck != null)
        {
            Vector2 dir = facingRight ? Vector2.right : Vector2.left;
            bool wall = Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, wallLayer);
            Gizmos.color = wall ? Color.blue : Color.yellow;
            Gizmos.DrawRay(wallCheck.position, (Vector3)dir * wallCheckDistance);
        }
    }
}