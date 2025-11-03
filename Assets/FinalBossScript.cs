using System.Collections;
using UnityEngine;

public class FinalBossController : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private Transform[] teleportPositions;
    [SerializeField] private float teleportSpeed = 10f; // Velocidad de "animación" de teleport

    [Header("Sphere Attack Settings")]
    [SerializeField] private GameObject purpleSphere;
    [SerializeField] private float sphereSpeed = 5f;
    [SerializeField] private AudioClip sphereAttackSFX; // Sonido por cada esfera

    [Header("Meteor Rain Settings")]
    [SerializeField] private GameObject meteorSphere;
    [SerializeField] private Transform meteorSpawnPoint;
    [SerializeField] private float meteorFallSpeed = 8f;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private AudioClip meteorAttackSFX; // Sonido por cada meteorito

    [Header("Mage Summon Settings")]
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private Transform leftMageSpawn;
    [SerializeField] private Transform rightMageSpawn;
    [SerializeField] private AudioClip summonMageSFX; // Sonido por cada invocación

    [Header("Following Sphere Settings")]
    [SerializeField] private GameObject followingSphere;
    [SerializeField] private float followDuration = 2f;
    [SerializeField] private AudioClip followingSphereSFX; // Sonido por generación de esfera

    [Header("Pyramid Attack Settings")]
    [SerializeField] private GameObject pyramidSphere;
    [SerializeField] private Transform leftPyramidSpawn;
    [SerializeField] private Transform rightPyramidSpawn;
    [SerializeField] private float pyramidSphereSpeed = 4f;
    [SerializeField] private AudioClip pyramidAttackSFX; // Sonido por cada esfera de pirámide

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;  // Animator propio del boss

    [Header("Teleport Audio")]
    [SerializeField] private AudioClip teleportSFX;

    [Header("Attack Timing")]
    [SerializeField] private float timeBetweenAttacks = 3f;

    private bool isAttacking = false;

    private void Start()
    {
        StartCoroutine(AttackCycle());
    }
    [SerializeField] private BossLife bossLife; // Referencia al script de vida del boss

    private IEnumerator AttackCycle()
    {
        while (true)
        {
            float waitTime = Random.Range(timeBetweenAttacks * 0.5f, timeBetweenAttacks);
            yield return new WaitForSeconds(waitTime);

            if (!isAttacking)
            {
                TeleportToRandomPosition();

                if (bossLife.health > bossLife.maxHealth / 2)
                {
                    // Vida normal: repetir ataques 3 veces excepto magos
                    int attackNumber = Random.Range(1, 6);
                    int repeatCount = (attackNumber == 3) ? 1 : 3;

                    for (int i = 0; i < repeatCount; i++)
                    {
                        yield return StartCoroutine(ExecuteAttack(attackNumber));

                        if (attackNumber != 3)
                            yield return new WaitForSeconds(Random.Range(0.3f, 0.7f));
                    }
                }
                else
                {
                    // Modo furia: combinaciones de ataques, sin invocar magos
                    int firstAttack = Random.Range(1, 6);
                    while (firstAttack == 3) firstAttack = Random.Range(1, 6);

                    int secondAttack = Random.Range(1, 6);
                    while (secondAttack == 3 || secondAttack == firstAttack)
                        secondAttack = Random.Range(1, 6);

                    // Ejecutar ataques combinados con pausa corta
                    yield return StartCoroutine(ExecuteAttack(firstAttack));
                    yield return new WaitForSeconds(0.2f);
                    yield return StartCoroutine(ExecuteAttack(secondAttack));
                }
            }
        }
    }



    private void TeleportToRandomPosition()
    {
        int newIndex = Random.Range(0, teleportPositions.Length);
        transform.position = teleportPositions[newIndex].position;

        // Animación de teleport
        if (animator != null)
            animator.SetTrigger("Teleport");

        // Sonido de teleport
        if (teleportSFX != null)
            AudioManager.Instance.PlaySFX(teleportSFX);

        Debug.Log($"Final Boss → Teleport a posición {newIndex + 1}");
    }

    private IEnumerator ExecuteAttack(int attackNumber)
    {
        isAttacking = true;

        // Animación según ataque
        if (animator != null)
            animator.SetTrigger("Attack" + attackNumber);

        // Ejecutar ataque
        switch (attackNumber)
        {
            case 1: yield return StartCoroutine(Attack1_DirectSpheres()); break;
            case 2: yield return StartCoroutine(Attack2_MeteorRain()); break;
            case 3: yield return StartCoroutine(Attack3_SummonMages()); break;
            case 4: yield return StartCoroutine(Attack4_FollowingSphere()); break;
            case 5: yield return StartCoroutine(Attack5_InvertedPyramid()); break;
        }

        isAttacking = false;
    }

    // ===== ATAQUE 1 =====
    private IEnumerator Attack1_DirectSpheres()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            GameObject sphere = Instantiate(purpleSphere, transform.position, Quaternion.identity);
            Rigidbody2D rb = sphere.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = direction * sphereSpeed;

            // Sonido por cada esfera
            if (sphereAttackSFX != null)
                AudioManager.Instance.PlaySFX(sphereAttackSFX);

            yield return new WaitForSeconds(0.5f);
        }
    }

    // ===== ATAQUE 2 =====
    private IEnumerator Attack2_MeteorRain()
    {
        for (int i = 0; i < 8; i++)
        {
            float randomX = Random.Range(-8f, 8f);
            Vector3 spawnPos = new Vector3(randomX, meteorSpawnPoint.position.y, 0);

            GameObject meteor = Instantiate(meteorSphere, spawnPos, Quaternion.identity);
            Rigidbody2D rb = meteor.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                Vector2 diagonalDirection = new Vector2(Random.Range(-1f, 1f), -1f).normalized;
                rb.linearVelocity = diagonalDirection * meteorFallSpeed;
            }

            meteor.AddComponent<MeteorExplosion>().explosionPrefab = explosionEffect;

            // Sonido por cada meteorito
            if (meteorAttackSFX != null)
                AudioManager.Instance.PlaySFX(meteorAttackSFX);

            yield return new WaitForSeconds(0.3f);
        }
    }

    // ===== ATAQUE 3 =====
    private IEnumerator Attack3_SummonMages()
    {
        Instantiate(magePrefab, leftMageSpawn.position, Quaternion.identity);
        if (summonMageSFX != null)
            AudioManager.Instance.PlaySFX(summonMageSFX);

        yield return new WaitForSeconds(0.5f);

        Instantiate(magePrefab, rightMageSpawn.position, Quaternion.identity);
        if (summonMageSFX != null)
            AudioManager.Instance.PlaySFX(summonMageSFX);

        yield return new WaitForSeconds(2f);
    }

    // ===== ATAQUE 4 =====
    private IEnumerator Attack4_FollowingSphere()
    {
        Vector3 spawnPos = new Vector3(player.position.x, player.position.y + 3f, 0);
        GameObject sphere = Instantiate(followingSphere, spawnPos, Quaternion.identity);

        if (followingSphereSFX != null)
            AudioManager.Instance.PlaySFX(followingSphereSFX);

        float elapsedTime = 0f;
        while (elapsedTime < followDuration && sphere != null)
        {
            sphere.transform.position = new Vector3(player.position.x, sphere.transform.position.y, 0);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (sphere != null)
        {
            Rigidbody2D rb = sphere.GetComponent<Rigidbody2D>();
            if (rb == null) rb = sphere.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb. linearVelocity = Vector2.down * 10f;
        }
    }

    // ===== ATAQUE 5 =====
    private IEnumerator Attack5_InvertedPyramid()
    {
        int rows = 5;
        for (int row = 0; row < rows; row++)
        {
            GameObject leftSphere = Instantiate(pyramidSphere, leftPyramidSpawn.position, Quaternion.identity);
            GameObject rightSphere = Instantiate(pyramidSphere, rightPyramidSpawn.position, Quaternion.identity);

            Rigidbody2D leftRb = leftSphere.GetComponent<Rigidbody2D>();
            Rigidbody2D rightRb = rightSphere.GetComponent<Rigidbody2D>();

            if (leftRb != null)
            {
                float angle = 60f + (row * 10f);
                Vector2 leftDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                leftRb.linearVelocity = leftDir * pyramidSphereSpeed;
            }

            if (rightRb != null)
            {
                float angle = 120f - (row * 10f);
                Vector2 rightDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                rightRb.linearVelocity = rightDir * pyramidSphereSpeed;
            }

            // Sonido por cada esfera de pirámide
            if (pyramidAttackSFX != null)
                AudioManager.Instance.PlaySFX(pyramidAttackSFX);

            yield return new WaitForSeconds(0.4f);
        }
    }
}

// ===== EXPLOSIÓN DE METEORITOS =====
public class MeteorExplosion : MonoBehaviour
{
    public GameObject explosionPrefab;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
