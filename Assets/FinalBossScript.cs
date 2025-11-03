using System.Collections;
using UnityEngine;

public class FinalBossController : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private Transform[] teleportPositions;
    [SerializeField] private float teleportInterval = 4f;
    [SerializeField] private float attackInterval = 3f;
    private int currentTeleportIndex = 0;

    [Header("Sphere Attack")]
    [SerializeField] private GameObject purpleSphere;
    [SerializeField] private float sphereSpeed = 5f;

    [Header("Meteor Rain")]
    [SerializeField] private GameObject meteorSphere;
    [SerializeField] private Transform meteorSpawnPoint;
    [SerializeField] private float meteorFallSpeed = 8f;
    [SerializeField] private GameObject explosionEffect;

    [Header("Mage Summon")]
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private Transform leftMageSpawn;
    [SerializeField] private Transform rightMageSpawn;

    [Header("Following Sphere")]
    [SerializeField] private GameObject followingSphere;
    [SerializeField] private float followDuration = 2f;

    [Header("Pyramid Attack")]
    [SerializeField] private GameObject pyramidSphere;
    [SerializeField] private Transform leftPyramidSpawn;
    [SerializeField] private Transform rightPyramidSpawn;
    [SerializeField] private float pyramidSphereSpeed = 6f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;

    private bool isAttacking = false;
    private bool isTeleporting = false;

    private void Start()
    {
        StartCoroutine(TeleportRoutine());
        StartCoroutine(AttackRoutine());
    }

    // --- TELEPORT ---
    private IEnumerator TeleportRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(teleportInterval);
            if (!isTeleporting)
                StartCoroutine(HandleTeleport());
        }
    }

    private IEnumerator HandleTeleport()
    {
        isTeleporting = true;
        if (animator != null)
            animator.SetTrigger("Disappear");

        yield return new WaitForSeconds(0.8f);

        int newIndex;
        do
        {
            newIndex = Random.Range(0, teleportPositions.Length);
        } while (newIndex == currentTeleportIndex);

        currentTeleportIndex = newIndex;
        transform.position = teleportPositions[newIndex].position;

        if (animator != null)
            animator.SetTrigger("Appear");

        yield return new WaitForSeconds(0.5f);
        isTeleporting = false;
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);
            if (!isAttacking && !isTeleporting)
                StartCoroutine(ExecuteRandomAttack());
        }
    }

    private IEnumerator ExecuteRandomAttack()
    {
        isAttacking = true;

        int randomAttack = Random.Range(1, 6);

        switch (randomAttack)
        {
            case 1:
                yield return StartCoroutine(Attack1_DirectSpheres());
                break;
            case 2:
                yield return StartCoroutine(Attack2_MeteorRain());
                break;
            case 3:
                yield return StartCoroutine(Attack3_SummonMages());
                break;
            case 4:
                yield return StartCoroutine(Attack4_FollowingSphere());
                break;
            case 5:
                yield return StartCoroutine(Attack5_HorizontalPyramid());
                break;
        }

        isAttacking = false;
    }

    // --- ATAQUES ---
    private IEnumerator Attack1_DirectSpheres()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            GameObject sphere = Instantiate(purpleSphere, transform.position, Quaternion.identity);
            Rigidbody2D rb = sphere.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = dir * sphereSpeed;
            yield return new WaitForSeconds(0.4f);
        }
    }

    // 🔥 ATAQUE MEJORADO: LLUVIA DOBLE DE METEOROS
    private IEnumerator Attack2_MeteorRain()
    {
        int meteorCount = 16; // el doble de antes

        for (int i = 0; i < meteorCount; i++)
        {
            float randomX = Random.Range(-9f, 9f);
            Vector3 spawnPos = new Vector3(randomX, meteorSpawnPoint.position.y, 0);

            GameObject meteor = Instantiate(meteorSphere, spawnPos, Quaternion.identity);
            Rigidbody2D rb = meteor.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Ángulo más variado para que no caigan todos igual
                Vector2 diagonal = new Vector2(Random.Range(-0.6f, 0.6f), -1f).normalized;
                rb.linearVelocity = diagonal * meteorFallSpeed;
            }

            meteor.AddComponent<MeteorExplosion>().explosionPrefab = explosionEffect;
            yield return new WaitForSeconds(0.2f);
        }

        // 🌩️ segunda tanda aleatoria
        if (Random.value > 0.5f)
        {
            yield return new WaitForSeconds(0.8f);
            for (int i = 0; i < 8; i++)
            {
                float randomX = Random.Range(-9f, 9f);
                Vector3 spawnPos = new Vector3(randomX, meteorSpawnPoint.position.y, 0);
                GameObject meteor = Instantiate(meteorSphere, spawnPos, Quaternion.identity);
                Rigidbody2D rb = meteor.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.down * meteorFallSpeed;
                meteor.AddComponent<MeteorExplosion>().explosionPrefab = explosionEffect;
            }
        }
    }

    // Invocación de magos
    private IEnumerator Attack3_SummonMages()
    {
        Instantiate(magePrefab, leftMageSpawn.position, Quaternion.identity);
        yield return new WaitForSeconds(0.5f);
        Instantiate(magePrefab, rightMageSpawn.position, Quaternion.identity);
        yield return new WaitForSeconds(2f);
    }

    // 🌪️ ATAQUE MEJORADO: ESFERA SIGUE / CAE / HORIZONTAL
    private IEnumerator Attack4_FollowingSphere()
    {
        int pattern = Random.Range(1, 4); // 1: vertical, 2: horizontal, 3: mixto

        if (pattern == 1)
        {
            yield return StartCoroutine(SingleVerticalSphere());
        }
        else if (pattern == 2)
        {
            yield return StartCoroutine(SingleHorizontalSphere());
        }
        else
        {
            // Mixto: uno vertical y uno horizontal a la vez
            StartCoroutine(SingleVerticalSphere());
            yield return StartCoroutine(SingleHorizontalSphere());
        }
    }

    private IEnumerator SingleVerticalSphere()
    {
        Vector3 spawnPos = new Vector3(player.position.x, player.position.y + 4f, 0);
        GameObject sphere = Instantiate(followingSphere, spawnPos, Quaternion.identity);
        yield return new WaitForSeconds(0.3f);
        Rigidbody2D rb = sphere.GetComponent<Rigidbody2D>() ?? sphere.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.down * 10f;
    }

    private IEnumerator SingleHorizontalSphere()
    {
        bool fromLeft = Random.value > 0.5f;
        Vector3 spawnPos = new Vector3(fromLeft ? -10f : 10f, player.position.y + 0.5f, 0);
        GameObject sphere = Instantiate(followingSphere, spawnPos, Quaternion.identity);
        Rigidbody2D rb = sphere.GetComponent<Rigidbody2D>() ?? sphere.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearVelocity = (fromLeft ? Vector2.right : Vector2.left) * 10f;
        yield return null;
    }

    // 🧱 ATAQUE MEJORADO: PIRÁMIDE HORIZONTAL
    private IEnumerator Attack5_HorizontalPyramid()
    {
        int rows = 5;

        for (int row = 0; row < rows; row++)
        {
            // Dispara desde izquierda hacia derecha (horizontal)
            GameObject leftSphere = Instantiate(pyramidSphere, leftPyramidSpawn.position, Quaternion.identity);
            Rigidbody2D leftRb = leftSphere.GetComponent<Rigidbody2D>();
            if (leftRb != null)
            {
                float spread = 0.2f * row;
                Vector2 dir = new Vector2(1f, spread).normalized;
                leftRb.linearVelocity = dir * pyramidSphereSpeed;
            }

            // Derecha hacia izquierda
            GameObject rightSphere = Instantiate(pyramidSphere, rightPyramidSpawn.position, Quaternion.identity);
            Rigidbody2D rightRb = rightSphere.GetComponent<Rigidbody2D>();
            if (rightRb != null)
            {
                float spread = 0.2f * row;
                Vector2 dir = new Vector2(-1f, spread).normalized;
                rightRb.linearVelocity = dir * pyramidSphereSpeed;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }
}

// Script auxiliar para explosión
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
