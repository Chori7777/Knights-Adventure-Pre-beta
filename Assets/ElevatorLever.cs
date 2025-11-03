using UnityEngine;
using UnityEngine.UI;

public class ElevatorLever : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private ElevatorSystem elevator;
    [SerializeField] private bool callToPointA = false;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Requiere llave?")]
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private Image keyIcon; // Icono que debe estar visible

    [Header("Visuales")]
    [SerializeField] private Sprite leverOffSprite;
    [SerializeField] private Sprite leverOnSprite;
    [SerializeField] private GameObject floatingTextPrefab;

    private bool isActivated = false;
    private bool playerInRange = false;
    private GameObject floatingTextInstance;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (floatingTextPrefab != null)
        {
            floatingTextInstance = Instantiate(floatingTextPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            floatingTextInstance.SetActive(false);
        }
    }

    private void Update()
    {
        DetectPlayer();

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            TryActivateLever();
        }

        if (floatingTextInstance != null && Camera.main != null)
        {
            floatingTextInstance.transform.LookAt(Camera.main.transform);
            floatingTextInstance.transform.Rotate(0, 180, 0);
        }
    }

    private void DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactionRadius, playerLayer);

        bool wasInRange = playerInRange;
        playerInRange = hit;

        if (floatingTextInstance != null)
            floatingTextInstance.SetActive(playerInRange);

        if (!wasInRange && playerInRange)
            Debug.Log("Jugador cerca de palanca");
    }

    private void TryActivateLever()
    {
        if (isActivated || elevator == null) return;

        if (requiresKey)
        {
            if (keyIcon == null || !keyIcon.enabled)
            {
                Debug.Log("No tenés la llave para usar esta palanca.");
                return;
            }
        }

        elevator.CallElevator(callToPointA);
        isActivated = true;
        UpdateLeverVisual();
        Debug.Log("Palanca activada, ascensor llamado.");
    }

    private void UpdateLeverVisual()
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = isActivated ? leverOnSprite : leverOffSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
