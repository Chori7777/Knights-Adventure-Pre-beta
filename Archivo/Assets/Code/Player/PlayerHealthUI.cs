using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public static PlayerHealthUI Instance;

    private playerLife player;
    //Vidas y pociones
    [Header("Texto")]
    public TextMeshProUGUI potionText;
    public TextMeshProUGUI coinText;

    [Header("Espada")]
    public Image swordHandle;
    public Image[] swordMiddleParts;
    public Image swordTip;

    [Header("Sprites Espada Llena")]
    public Sprite handleFullSprite;
    public Sprite middleFullSprite;
    public Sprite tipFullSprite;

    [Header("Sprites Espada Vacía")]
    public Sprite handleEmptySprite;
    public Sprite middleEmptySprite;
    public Sprite tipEmptySprite;

    [Header("Caballero")]
    public Image knightImage;
    public Image knightHeadImage;

    [Header("Sprites Caballero")]
    public Sprite knight5HealthSprite;
    public Sprite knight4HealthSprite;
    public Sprite knight3HealthSprite;
    public Sprite knight2HealthSprite;
    public Sprite knight1HealthSprite;

    [Header("Configuración")]
    public float knightMoveDistancePerHealth = 50f;
    public float headOffsetX = 60f;
    public float headOffsetY = 0f;

    private int HEAD_OFFSET = 2;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // opcional: mantener el HUD entre escenas si querés
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Inicializa la UI desde playerLife. Ahora es segura: chequea nulls y no asume ControladorDatosJuego.
    public void Initialize(playerLife p)
    {
        if (p == null)
        {
            Debug.LogWarning("[PlayerHealthUI] Initialize recibió player null.");
            return;
        }

        player = p;

        // Intentar actualizar, pero proteger contra nulls internos
        try
        {
            UpdateDisplay();

            // ControladorDatosJuego puede ser null en ciertos escenarios (por ej. en tests o bootstrap)
            var controlador = ControladorDatosJuego.Instance;
            if (controlador != null)
            {
                ActualizarMonedas(controlador.ObtenerMonedas());
            }
            else
            {
                // Si no existe, dejamos el texto en "0" o lo que esté asignado
                if (coinText != null) coinText.text = "0";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerHealthUI] Excepción en Initialize(): {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void UpdateDisplay()
    {
        if (player == null)
        {
            // no hay player asignado, nada que hacer
            return;
        }

        UpdatePotionText();
        UpdateKnightSprite();
        UpdateSword();
        UpdateHeadPosition();
        UpdateKnightPosition();
    }

    void UpdatePotionText()
    {
        if (potionText != null && player != null)
        {
            potionText.text = player.Potions + "/" + player.MaxPotions;
        }
    }

    public void ActualizarMonedas(int cantidad)
    {
        if (coinText != null)
        {
            coinText.text = cantidad.ToString();
        }
    }

    void UpdateKnightSprite()
    {
        if (player == null) return;
        if (knightImage == null || knightHeadImage == null) return;

        Sprite currentSprite = knight1HealthSprite;

        int h = Mathf.Clamp(player.Health, 0, player.MaxHealth);

        // Elegir sprite en base a salud (seguro si alguno es null no rompe)
        if (h >= 5 && knight5HealthSprite != null) currentSprite = knight5HealthSprite;
        else if (h == 4 && knight4HealthSprite != null) currentSprite = knight4HealthSprite;
        else if (h == 3 && knight3HealthSprite != null) currentSprite = knight3HealthSprite;
        else if (h == 2 && knight2HealthSprite != null) currentSprite = knight2HealthSprite;
        else if (knight1HealthSprite != null) currentSprite = knight1HealthSprite;

        if (currentSprite != null)
        {
            knightImage.sprite = currentSprite;
            knightHeadImage.sprite = currentSprite;
        }
    }

    void UpdateSword()
    {
        if (player == null) return;

        if (swordTip != null)
        {
            if (player.Health >= player.MaxHealth && tipFullSprite != null)
                swordTip.sprite = tipFullSprite;
            else if (tipEmptySprite != null)
                swordTip.sprite = tipEmptySprite;
        }

        if (swordMiddleParts != null)
        {
            for (int i = 0; i < swordMiddleParts.Length; i++)
            {
                if (swordMiddleParts[i] != null)
                {
                    int healthThreshold = player.MaxHealth - HEAD_OFFSET - i;

                    if (player.Health > healthThreshold && middleFullSprite != null)
                        swordMiddleParts[i].sprite = middleFullSprite;
                    else if (middleEmptySprite != null)
                        swordMiddleParts[i].sprite = middleEmptySprite;
                }
            }
        }

        if (swordHandle != null)
        {
            if (player.Health > 0 && handleFullSprite != null)
                swordHandle.sprite = handleFullSprite;
            else if (handleEmptySprite != null)
                swordHandle.sprite = handleEmptySprite;
        }
    }

    void UpdateHeadPosition()
    {
        if (player == null) return;
        if (knightHeadImage == null) return;

        RectTransform headRect = knightHeadImage.GetComponent<RectTransform>();
        if (headRect == null) return;

        if (swordMiddleParts == null || swordMiddleParts.Length == 0)
        {
            // posicion por defecto en la punta si no hay segmentos
            if (swordTip != null)
            {
                RectTransform tipRect = swordTip.GetComponent<RectTransform>();
                if (tipRect != null)
                {
                    Vector3 offset = new Vector3(headOffsetX, headOffsetY, 0);
                    headRect.position = tipRect.position + offset;
                }
            }
            return;
        }

        int segmentIndex = (player.MaxHealth - player.Health) - HEAD_OFFSET;

        if (segmentIndex >= 0 && segmentIndex < swordMiddleParts.Length && swordMiddleParts[segmentIndex] != null)
        {
            RectTransform segmentRect = swordMiddleParts[segmentIndex].GetComponent<RectTransform>();
            if (segmentRect != null) headRect.position = segmentRect.position;
        }
        else if (player.Health > 0 && swordTip != null)
        {
            RectTransform tipRect = swordTip.GetComponent<RectTransform>();
            if (tipRect != null)
            {
                Vector3 offset = new Vector3(headOffsetX, headOffsetY, 0);
                headRect.position = tipRect.position + offset;
            }
        }
    }

    void UpdateKnightPosition()
    {
        if (player == null) return;
        if (knightImage == null) return;

        RectTransform knightRect = knightImage.GetComponent<RectTransform>();
        if (knightRect == null) return;

        float healthLost = player.MaxHealth - player.Health;
        float moveAmount = healthLost * knightMoveDistancePerHealth;

        Vector2 newPos = knightRect.anchoredPosition;
        newPos.x = -moveAmount;
        knightRect.anchoredPosition = newPos;
    }
}
