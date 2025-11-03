using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class playerLife : MonoBehaviour
{
    private PlayerMovement controller;
    private PlayerHealthUI healthUI;
    private PlayerAnimationController animController;
    private Animator fallbackAnimator;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth = 5;
    public int Health => currentHealth;
    public int MaxHealth => maxHealth;

    [Header("Pociones")]
    [SerializeField] private int maxPotions = 5;
    [SerializeField] private int currentPotions = 3;
    [SerializeField] private int potionHealAmount = 1;
    [SerializeField] private float potionCooldown = 0.5f;
    private float lastPotionTime = -10f;
    public int Potions => currentPotions;
    public int MaxPotions => maxPotions;

    [Header("Sistema de Daño")]
    [SerializeField] private float invincibilityDuration = 1f;
    private float lastDamageTime = -10f;
    private bool isTakingDamage = false;
    public bool IsTakingDamage => isTakingDamage;

    [Header("Controles")]
    [SerializeField] private KeyCode usePotionKey = KeyCode.R;

    private bool isDead = false;

    [Header("Muerte")]
    [SerializeField] private string deathAnimationName = "Death";
    [SerializeField] private float deathFallbackDuration = 1f;

    // Nuevo: indicador de que el player terminó su inicialización lógica
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        // Obtener componentes locales (esto no toca la UI)
        controller = GetComponent<PlayerMovement>();
        animController = GetComponent<PlayerAnimationController>();
        fallbackAnimator = GetComponent<Animator>();

        if (animController != null && controller != null)
            animController.Initialize(controller);

        // Clamp de salud temprano
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    private IEnumerator Start()
    {
        // Intentar vincular con el HUD durante unos frames (esperar a que la UI exista)
        float timeout = 1.0f; // 1 segundo de paciencia
        float t = 0f;
        while (PlayerHealthUI.Instance == null && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (PlayerHealthUI.Instance != null)
        {
            healthUI = PlayerHealthUI.Instance;
            // Initialize es seguro (ver PlayerHealthUI modificado)
            healthUI.Initialize(this);
        }
        else
        {
            Debug.LogWarning("[playerLife] No se encontró PlayerHealthUI en Start() (continuando sin HUD).");
        }

        // Registrar para volver a vincular cuando se cargue escena (en caso de que HUD esté por escena)
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Marca que ya terminó la inicialización lógica
        isInitialized = true;

        // Actualizar UI final
        UpdateUI();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-vincular HUD si aparece en la nueva escena
        if (PlayerHealthUI.Instance != null)
        {
            healthUI = PlayerHealthUI.Instance;
            healthUI.Initialize(this);
        }
        UpdateUI();
    }

    private void Update()
    {
        HandlePotionInput();
    }

    private void HandlePotionInput()
    {
        if (Input.GetKeyDown(usePotionKey))
            TryUsePotion();
    }

    private void TryUsePotion()
    {
        if (!CanUsePotion()) return;

        currentPotions--;
        currentHealth = Mathf.Min(currentHealth + potionHealAmount, maxHealth);
        lastPotionTime = Time.time;
        UpdateUI();
    }

    private bool CanUsePotion()
    {
        if (Time.time - lastPotionTime < potionCooldown) return false;
        if (currentPotions <= 0) return false;
        if (currentHealth >= maxHealth) return false;
        return true;
    }

    public void AddPotion(int amount = 1)
    {
        currentPotions = Mathf.Min(currentPotions + amount, maxPotions);
        UpdateUI();
    }
    public void TakeDamage(Vector2 attackerPosition, int damage)
    {
        if (!CanTakeDamage()) return;

        lastDamageTime = Time.time;
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateUI();

        if (controller != null)
        {
            controller.TakeDamage(attackerPosition);
        }
        else
        {
            Debug.LogError("[playerLife] Error al llamar controller.TakeDamage(): PlayerMovement no encontrado o no inicializado");
        }

        if (animController != null)
            animController.TriggerDamage();
        else if (fallbackAnimator != null)
            fallbackAnimator.SetBool("damage", true);

        isTakingDamage = true;

        if (currentHealth <= 0)
            StartCoroutine(HandleDeathSequence());
    }

    private bool CanTakeDamage()
    {
        if (isDead) return false;
        if (currentHealth <= 0) return false;
        if (Time.time - lastDamageTime < invincibilityDuration) return false;
        return true;
    }

    public void StopDamageAnimation()
    {
        if (animController != null)
            animController.StopDamage();
        else if (fallbackAnimator != null)
            fallbackAnimator.SetBool("damage", false);

        isTakingDamage = false;
    }

    private IEnumerator HandleDeathSequence()
    {
        if (isDead) yield break;
        isDead = true;

        DisableAllControls();
        StopDamageAnimation();
        yield return null;

        if (animController != null)
        {
            animController.TriggerDeath();
        }
        else if (fallbackAnimator != null)
        {
            fallbackAnimator.ResetTrigger("DoubleJump");
            fallbackAnimator.ResetTrigger("Throw");
            fallbackAnimator.SetBool("damage", false);
            fallbackAnimator.SetTrigger("Death");

            float clipLength = deathFallbackDuration;
            var rac = fallbackAnimator.runtimeAnimatorController;
            if (rac != null)
            {
                foreach (var clip in rac.animationClips)
                {
                    if (clip.name == deathAnimationName)
                    {
                        clipLength = clip.length;
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(Mathf.Max(0.01f, clipLength));
        }
        else
        {
            yield return new WaitForSeconds(deathFallbackDuration);
        }

        OnDeathComplete();
    }

    public void OnDeathAnimationEnd()
    {
        if (!isDead) isDead = true;
        OnDeathComplete();
        AudioManager.Instance.StopMusicImmediately();
    }

    private void OnDeathComplete()
    {
        SceneManager.LoadScene("GameOver");
        AudioManager.Instance.StopMusicImmediately();
    }

    private void DisableAllControls()
    {
        if (controller == null) return;

        controller.canMove = false;
        controller.canJump = false;
        controller.canAttack = false;
        controller.canDash = false;
        controller.canWallCling = false;
        controller.canBlock = false;
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateUI();
    }

    public void HealFull()
    {
        if (isDead) return;
        currentHealth = maxHealth;
        UpdateUI();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HealthPotion"))
        {
            AddPotion();
            Destroy(collision.gameObject);
        }

        if (collision.CompareTag("Consumable"))
        {
            Destroy(collision.gameObject);
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (healthUI != null)
            healthUI.UpdateDisplay();
    }

    // Métodos de setters
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateUI();
    }

    public void SetMaxHealth(int max)
    {
        maxHealth = max;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateUI();
    }

    public void SetPotions(int potions)
    {
        currentPotions = Mathf.Clamp(potions, 0, maxPotions);
        UpdateUI();
    }

    public void SetMaxPotions(int max)
    {
        maxPotions = max;
        currentPotions = Mathf.Min(currentPotions, maxPotions);
        UpdateUI();
    }
}
