using System.Collections;
using UnityEngine;

public class FeatherBossController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Transform leftPosition;
    [SerializeField] private Transform rightPosition;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float moveCooldown = 5f;
    [SerializeField] private float teleportDuration = 0.5f; // Tiempo que tarda en desaparecer/aparecer
    private bool isAtLeftPosition = true;
    private SpriteRenderer spriteRenderer;

    [Header("Feather Projectile")]
    [SerializeField] private GameObject featherPrefab;
    [SerializeField] private float featherSpeed = 8f;

    [Header("Attack 1: Falling Feathers")]
    [SerializeField] private int fallingFeatherCount = 5;
    [SerializeField] private float fallingFeatherInterval = 0.3f;
    [SerializeField] private float featherFallSpeed = 5f;
    [SerializeField] private Transform fallingFeatherSpawnArea; // Centro del área de spawn
    [SerializeField] private float fallingFeatherSpawnRangeX = 8f; // Rango horizontal
    [SerializeField] private float fallingFeatherSpawnHeight = 6f; // Altura de spawn
    [SerializeField] private AudioClip fallingFeatherSFX;

    [Header("Attack 2: Cross & X Pattern")]
    [SerializeField] private float crossDistance = 3f;
    [SerializeField] private float crossWarningTime = 1f; // Tiempo antes de lanzar las plumas
    [SerializeField] private GameObject warningIndicatorPrefab; // Opcional: indicador visual
    [SerializeField] private AudioClip crossPatternSFX;
    [SerializeField] private AudioClip warningBeepSFX;

    [Header("Attack 3: Melee Defense")]
    [SerializeField] private float meleeDetectionRange = 3f;
    [SerializeField] private int defenseFeatherCount = 3;
    [SerializeField] private float defenseFeatherSpread = 20f; // Ángulo de dispersión
    [SerializeField] private float defenseActivationRange = 2.5f; // A qué distancia se activa
    [SerializeField] private AudioClip defenseFeatherSFX;

    [Header("Attack 4: Feather Wave")]
    [SerializeField] private int waveFeatherCount = 8;
    [SerializeField] private float waveInitialSpeed = 3f;
    [SerializeField] private float waveAcceleration = 1.5f;
    [SerializeField] private float waveVerticalSpacing = 1f;
    [SerializeField] private float waveStartY = -3f; // Posición Y inicial de la ola
    [SerializeField] private AudioClip waveAttackSFX;

    [Header("Attack 5: Rising Feather Wave")]
    [SerializeField] private int risingFeatherCount = 6;
    [SerializeField] private float risingSpeed = 4f;
    [SerializeField] private float risingHomeSpeed = 2f;
    [SerializeField] private float risingHomeDistance = 4f; // A qué distancia empieza a seguir
    [SerializeField] private float risingSpawnY = -6f; // Altura de spawn
    [SerializeField] private float risingSpawnRangeX = 8f; // Rango horizontal
    [SerializeField] private AudioClip risingAttackSFX;

    [Header("Attack 6 (Bonus): Feather Explosion")]
    [SerializeField] private Transform[] explosionSpawnPoints; // Puntos específicos de explosión
    [SerializeField] private int explosionFeatherCount = 12;
    [SerializeField] private float explosionFeatherSpeedMin = 0.8f;
    [SerializeField] private float explosionFeatherSpeedMax = 1.2f;
    [SerializeField] private AudioClip explosionSFX;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private BossLife bossLife;

    [Header("Audio")]
    [SerializeField] private AudioClip moveSFX;
    [SerializeField] private AudioClip teleportSFX;

    [Header("Attack Timing")]
    [SerializeField] private float timeBetweenAttacks = 2.5f;

    private bool isAttacking = false;
    private Coroutine moveCoroutine;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (bossLife == null)
            Debug.LogWarning("BossLife no asignado en FeatherBossController.");

        // Posicionar en punto inicial
        transform.position = leftPosition.position;

        StartCoroutine(AttackCycle());
        StartCoroutine(MovementCycle());
    }

    private IEnumerator MovementCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(moveCooldown);
            MoveToOppositePosition();
        }
    }

    public void OnBossHit()
    {
        // Llamar desde BossLife cuando reciba daño
        MoveToOppositePosition();
    }

    private void MoveToOppositePosition()
    {
        if (isAttacking) return;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(TeleportCoroutine());
    }

    private IEnumerator TeleportCoroutine()
    {
        Vector3 targetPos = isAtLeftPosition ? rightPosition.position : leftPosition.position;

        if (animator != null)
            animator.SetTrigger("Disappear");

        if (moveSFX != null)
            AudioManager.Instance.PlaySFX(moveSFX);

        // Fade out
        float elapsed = 0f;
        Color originalColor = spriteRenderer.color;

        while (elapsed < teleportDuration / 2)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / (teleportDuration / 2));
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // Teleport instantáneo
        transform.position = targetPos;
        isAtLeftPosition = !isAtLeftPosition;

        if (teleportSFX != null)
            AudioManager.Instance.PlaySFX(teleportSFX);

        // Fade in
        elapsed = 0f;
        while (elapsed < teleportDuration / 2)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / (teleportDuration / 2));
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        spriteRenderer.color = originalColor;

        if (animator != null)
            animator.SetTrigger("Appear");
    }

    private IEnumerator AttackCycle()
    {
        yield return new WaitForSeconds(1f); // Delay inicial

        while (true)
        {
            float waitTime = Random.Range(timeBetweenAttacks * 0.7f, timeBetweenAttacks * 1.3f);
            yield return new WaitForSeconds(waitTime);

            // Verificar defensa melee primero
            if (IsPlayerInDefenseRange())
            {
                yield return StartCoroutine(Attack3_MeleeDefense());
                continue;
            }

            if (!isAttacking)
            {
                if (bossLife != null && bossLife.health > bossLife.maxHealth / 2)
                {
                    // Fase normal: ataques individuales
                    int attackNumber = Random.Range(1, 7); // 1-6
                    yield return StartCoroutine(ExecuteAttack(attackNumber));
                }
                else if (bossLife != null)
                {
                    // Fase furia: ataques combinados más rápidos
                    int firstAttack = Random.Range(1, 7);
                    yield return StartCoroutine(ExecuteAttack(firstAttack));

                    yield return new WaitForSeconds(0.3f);

                    int secondAttack = Random.Range(1, 7);
                    while (secondAttack == firstAttack)
                        secondAttack = Random.Range(1, 7);

                    yield return StartCoroutine(ExecuteAttack(secondAttack));
                }
            }
        }
    }

    private bool IsPlayerInDefenseRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) < defenseActivationRange;
    }

    private IEnumerator ExecuteAttack(int attackNumber)
    {
        isAttacking = true;

        if (animator != null)
            animator.SetTrigger("Attack");

        switch (attackNumber)
        {
            case 1: yield return StartCoroutine(Attack1_FallingFeathers()); break;
            case 2: yield return StartCoroutine(Attack2_CrossAndXPattern()); break;
            case 3: yield return StartCoroutine(Attack3_MeleeDefense()); break;
            case 4: yield return StartCoroutine(Attack4_FeatherWave()); break;
            case 5: yield return StartCoroutine(Attack5_RisingFeatherWave()); break;
            case 6: yield return StartCoroutine(Attack6_FeatherExplosion()); break;
        }

        isAttacking = false;
    }

    // ===== ATAQUE 1: PLUMAS QUE CAEN Y EXPLOTAN =====
    private IEnumerator Attack1_FallingFeathers()
    {
        Vector3 spawnCenter = fallingFeatherSpawnArea != null ?
            fallingFeatherSpawnArea.position : Vector3.zero;

        for (int i = 0; i < fallingFeatherCount; i++)
        {
            float randomX = spawnCenter.x + Random.Range(-fallingFeatherSpawnRangeX, fallingFeatherSpawnRangeX);
            Vector3 spawnPos = new Vector3(randomX, spawnCenter.y + fallingFeatherSpawnHeight, 0);

            GameObject feather = Instantiate(featherPrefab, spawnPos, Quaternion.Euler(0, 0, 90));

            // Agregar componente de pluma que cae
            FallingFeather falling = feather.AddComponent<FallingFeather>();
            falling.fallSpeed = featherFallSpeed;
            falling.featherPrefab = featherPrefab;
            falling.featherSpeed = featherSpeed;

            if (fallingFeatherSFX != null)
                AudioManager.Instance.PlaySFX(fallingFeatherSFX);

            yield return new WaitForSeconds(fallingFeatherInterval);
        }

        yield return new WaitForSeconds(1f);
    }

    // ===== ATAQUE 2: PATRÓN DE + Y X CON WARNING =====
    private IEnumerator Attack2_CrossAndXPattern()
    {
        if (player == null) yield break;

        Vector3 playerPos = player.position;

        // Patrón de cruz (+)
        Vector2[] crossDirections = {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right
        };

        // Mostrar warnings
        GameObject[] warnings = new GameObject[crossDirections.Length];
        for (int i = 0; i < crossDirections.Length; i++)
        {
            Vector3 spawnPos = playerPos + (Vector3)(crossDirections[i] * crossDistance);

            if (warningIndicatorPrefab != null)
            {
                warnings[i] = Instantiate(warningIndicatorPrefab, spawnPos, Quaternion.identity);
            }
        }

        if (warningBeepSFX != null)
            AudioManager.Instance.PlaySFX(warningBeepSFX);

        yield return new WaitForSeconds(crossWarningTime);

        // Destruir warnings y lanzar plumas
        foreach (GameObject warning in warnings)
        {
            if (warning != null)
                Destroy(warning);
        }

        foreach (Vector2 dir in crossDirections)
        {
            Vector3 spawnPos = playerPos + (Vector3)(dir * crossDistance);
            GameObject feather = Instantiate(featherPrefab, spawnPos, Quaternion.identity);

            Rigidbody2D rb = feather.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = -dir * featherSpeed;

            RotateFeatherToDirection(feather, -dir);
        }

        if (crossPatternSFX != null)
            AudioManager.Instance.PlaySFX(crossPatternSFX);

        yield return new WaitForSeconds(0.5f);

        // Patrón X (diagonal) - mismo proceso
        Vector2[] xDirections = {
            new Vector2(1, 1).normalized,
            new Vector2(-1, 1).normalized,
            new Vector2(1, -1).normalized,
            new Vector2(-1, -1).normalized
        };

        warnings = new GameObject[xDirections.Length];
        for (int i = 0; i < xDirections.Length; i++)
        {
            Vector3 spawnPos = playerPos + (Vector3)(xDirections[i] * crossDistance);

            if (warningIndicatorPrefab != null)
            {
                warnings[i] = Instantiate(warningIndicatorPrefab, spawnPos, Quaternion.identity);
            }
        }

        if (warningBeepSFX != null)
            AudioManager.Instance.PlaySFX(warningBeepSFX);

        yield return new WaitForSeconds(crossWarningTime);

        foreach (GameObject warning in warnings)
        {
            if (warning != null)
                Destroy(warning);
        }

        foreach (Vector2 dir in xDirections)
        {
            Vector3 spawnPos = playerPos + (Vector3)(dir * crossDistance);
            GameObject feather = Instantiate(featherPrefab, spawnPos, Quaternion.identity);

            Rigidbody2D rb = feather.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = -dir * featherSpeed;

            RotateFeatherToDirection(feather, -dir);
        }

        if (crossPatternSFX != null)
            AudioManager.Instance.PlaySFX(crossPatternSFX);

        yield return new WaitForSeconds(1f);
    }

    // ===== ATAQUE 3: DEFENSA MELEE CON HOMING =====
    private IEnumerator Attack3_MeleeDefense()
    {
        if (player == null) yield break;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        for (int i = 0; i < defenseFeatherCount; i++)
        {
            float spreadAngle = (i - (defenseFeatherCount - 1) / 2f) * defenseFeatherSpread;
            Vector2 direction = Quaternion.Euler(0, 0, spreadAngle) * directionToPlayer;

            GameObject feather = Instantiate(featherPrefab, transform.position + (Vector3)direction * 0.5f, Quaternion.identity);

            // Agregar componente de pluma con homing
            HomingFeather homing = feather.AddComponent<HomingFeather>();
            homing.player = player;
            homing.speed = featherSpeed * 1.2f;
            homing.homeStrength = 3f;
            homing.activationRange = meleeDetectionRange;

            RotateFeatherToDirection(feather, direction);

            if (defenseFeatherSFX != null)
                AudioManager.Instance.PlaySFX(defenseFeatherSFX);

            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.5f);
    }

    // ===== ATAQUE 4: OLA DE PLUMAS HORIZONTAL =====
    private IEnumerator Attack4_FeatherWave()
    {
        // Dirección de la ola según posición del jefe
        Vector2 waveDirection = isAtLeftPosition ? Vector2.right : Vector2.left;
        float startX = isAtLeftPosition ? leftPosition.position.x : rightPosition.position.x;

        for (int i = 0; i < waveFeatherCount; i++)
        {
            float yPos = waveStartY + (i * waveVerticalSpacing);
            Vector3 spawnPos = new Vector3(startX, yPos, 0);

            GameObject feather = Instantiate(featherPrefab, spawnPos, Quaternion.identity);

            AcceleratingFeather accel = feather.AddComponent<AcceleratingFeather>();
            accel.direction = waveDirection;
            accel.initialSpeed = waveInitialSpeed;
            accel.acceleration = waveAcceleration;

            RotateFeatherToDirection(feather, waveDirection);
        }

        if (waveAttackSFX != null)
            AudioManager.Instance.PlaySFX(waveAttackSFX);

        yield return new WaitForSeconds(1.5f);
    }

    // ===== ATAQUE 5: OLA DE PLUMAS ASCENDENTE =====
    private IEnumerator Attack5_RisingFeatherWave()
    {
        for (int i = 0; i < risingFeatherCount; i++)
        {
            float xPos = Random.Range(-risingSpawnRangeX, risingSpawnRangeX);
            Vector3 spawnPos = new Vector3(xPos, risingSpawnY, 0);

            GameObject feather = Instantiate(featherPrefab, spawnPos, Quaternion.Euler(0, 0, -90));

            RisingFeather rising = feather.AddComponent<RisingFeather>();
            rising.risingSpeed = risingSpeed;
            rising.homeSpeed = risingHomeSpeed;
            rising.homeDistance = risingHomeDistance;
            rising.player = player;

            if (risingAttackSFX != null)
                AudioManager.Instance.PlaySFX(risingAttackSFX);

            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(2f);
    }

    // ===== ATAQUE 6: EXPLOSIÓN DE PLUMAS =====
    private IEnumerator Attack6_FeatherExplosion()
    {
        // Usar puntos específicos o generar aleatorios dentro de pantalla
        Vector3[] points;

        if (explosionSpawnPoints != null && explosionSpawnPoints.Length > 0)
        {
            points = new Vector3[explosionSpawnPoints.Length];
            for (int i = 0; i < explosionSpawnPoints.Length; i++)
            {
                points[i] = explosionSpawnPoints[i].position;
            }
        }
        else
        {
            // Fallback: generar puntos aleatorios seguros
            points = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                points[i] = new Vector3(
                    Random.Range(-6f, 6f),
                    Random.Range(-2f, 2f),
                    0
                );
            }
        }

        // Explotar desde cada punto
        foreach (Vector3 point in points)
        {
            float angleStep = 360f / explosionFeatherCount;

            for (int i = 0; i < explosionFeatherCount; i++)
            {
                float angle = i * angleStep + Random.Range(-10f, 10f);
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                GameObject feather = Instantiate(featherPrefab, point, Quaternion.identity);

                Rigidbody2D rb = feather.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float speedMultiplier = Random.Range(explosionFeatherSpeedMin, explosionFeatherSpeedMax);
                    rb.linearVelocity = direction * featherSpeed * speedMultiplier;
                }

                RotateFeatherToDirection(feather, direction);
            }

            if (explosionSFX != null)
                AudioManager.Instance.PlaySFX(explosionSFX);

            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(1f);
    }

    // ===== UTILIDADES =====
    private void RotateFeatherToDirection(GameObject feather, Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        feather.transform.rotation = Quaternion.Euler(0, 0, angle - 90); // -90 porque la pluma mira hacia abajo
    }
}

// ===== COMPONENTES AUXILIARES =====

// Pluma que cae y explota
public class FallingFeather : MonoBehaviour
{
    public float fallSpeed = 5f;
    public GameObject featherPrefab;
    public float featherSpeed = 8f;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.down * fallSpeed;
    }

    private void Update()
    {
        // Rotar según dirección de movimiento
        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Suelo") || collision.CompareTag("Player"))
        {
            ShootSideFeathers();
            Destroy(gameObject);
        }
    }

    private void ShootSideFeathers()
    {
        // Pluma izquierda
        GameObject leftFeather = Instantiate(featherPrefab, transform.position, Quaternion.identity);
        Rigidbody2D leftRb = leftFeather.GetComponent<Rigidbody2D>();
        if (leftRb != null)
            leftRb.linearVelocity = Vector2.left * featherSpeed;
        leftFeather.transform.rotation = Quaternion.Euler(0, 0, 180 - 90);

        // Pluma derecha
        GameObject rightFeather = Instantiate(featherPrefab, transform.position, Quaternion.identity);
        Rigidbody2D rightRb = rightFeather.GetComponent<Rigidbody2D>();
        if (rightRb != null)
            rightRb.linearVelocity = Vector2.right * featherSpeed;
        rightFeather.transform.rotation = Quaternion.Euler(0, 0, -90);
    }
}

// Pluma con aceleración
public class AcceleratingFeather : MonoBehaviour
{
    public Vector2 direction;
    public float initialSpeed;
    public float acceleration;

    private Rigidbody2D rb;
    private float currentSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0;
        currentSpeed = initialSpeed;
        rb.linearVelocity = direction * currentSpeed;
    }

    private void Update()
    {
        currentSpeed += acceleration * Time.deltaTime;
        rb.linearVelocity = direction * currentSpeed;

        // Rotar según dirección de movimiento
        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }
}

// Pluma ascendente con homing
public class RisingFeather : MonoBehaviour
{
    public float risingSpeed = 4f;
    public float homeSpeed = 2f;
    public float homeDistance = 4f;
    public Transform player;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.up * risingSpeed;
    }

    private void Update()
    {
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < homeDistance)
            {
                Vector2 directionToPlayer = (player.position - transform.position).normalized;
                Vector2 currentVelocity = rb.linearVelocity;
                rb.linearVelocity = Vector2.Lerp(currentVelocity, directionToPlayer * risingSpeed, homeSpeed * Time.deltaTime);
            }
        }

        // Rotar según dirección de movimiento
        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }
}

// Pluma con homing (para defensa melee)
public class HomingFeather : MonoBehaviour
{
    public Transform player;
    public float speed = 8f;
    public float homeStrength = 3f;
    public float activationRange = 5f;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0;

        // Velocidad inicial hacia el jugador
        if (player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * speed;
        }
    }

    private void Update()
    {
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < activationRange)
            {
                Vector2 directionToPlayer = (player.position - transform.position).normalized;
                Vector2 currentVelocity = rb.linearVelocity.normalized * speed;
                rb.linearVelocity = Vector2.Lerp(currentVelocity, directionToPlayer * speed, homeStrength * Time.deltaTime);
            }
        }

        // Rotar según dirección de movimiento
        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }
}