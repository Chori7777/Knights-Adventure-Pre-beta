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
    //Basicamente en todos los valores de arriba
//Se le da al codigo las imagenes de la espada llena y la espada vacia, junto a la cabeza del caballero, para poder ir
//Cambiandolo cuando es necesario
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    //agarra playerlife
    public void Initialize(playerLife p)
    {
        player = p;
        UpdateDisplay();
        ActualizarMonedas(ControladorDatosJuego.Instance.ObtenerMonedas());
    }
    //Se encarga de actualizar el HUD
    public void UpdateDisplay()
    {
        if (player == null)
        {
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
        if (potionText != null)
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
        if (knightImage == null || knightHeadImage == null) return;

        Sprite currentSprite = knight1HealthSprite;

        if (player.Health == 5)
        {
            currentSprite = knight5HealthSprite;
        }
        else if (player.Health == 4)
        {
            currentSprite = knight4HealthSprite;
        }
        else if (player.Health == 3)
        {
            currentSprite = knight3HealthSprite;
        }
        else if (player.Health == 2)
        {
            currentSprite = knight2HealthSprite;
        }
        else
        {
            currentSprite = knight1HealthSprite;
        }

        knightImage.sprite = currentSprite;
        knightHeadImage.sprite = currentSprite;
    }

    void UpdateSword()
    {
        if (swordTip != null)
        {
            if (player.Health >= player.MaxHealth)
            {
                swordTip.sprite = tipFullSprite;
            }
            else
            {
                swordTip.sprite = tipEmptySprite;
            }
        }

        if (swordMiddleParts != null)
        {
            for (int i = 0; i < swordMiddleParts.Length; i++)
            {
                if (swordMiddleParts[i] != null)
                {
                    int healthThreshold = player.MaxHealth - HEAD_OFFSET - i;

                    if (player.Health > healthThreshold)
                    {
                        swordMiddleParts[i].sprite = middleFullSprite;
                    }
                    else
                    {
                        swordMiddleParts[i].sprite = middleEmptySprite;
                    }
                }
            }
        }

        if (swordHandle != null)
        {
            if (player.Health > 0)
            {
                swordHandle.sprite = handleFullSprite;
            }
            else
            {
                swordHandle.sprite = handleEmptySprite;
            }
        }
    }

    void UpdateHeadPosition()
    {
        if (knightHeadImage == null) return;

        RectTransform headRect = knightHeadImage.GetComponent<RectTransform>();
        if (headRect == null) return;

        int segmentIndex = (player.MaxHealth - player.Health) - HEAD_OFFSET;

        if (segmentIndex >= 0 && segmentIndex < swordMiddleParts.Length && swordMiddleParts[segmentIndex] != null)
        {
            RectTransform segmentRect = swordMiddleParts[segmentIndex].GetComponent<RectTransform>();
            headRect.position = segmentRect.position;
        }
        else if (player.Health > 0 && swordTip != null)
        {
            RectTransform tipRect = swordTip.GetComponent<RectTransform>();
            Vector3 offset = new Vector3(headOffsetX, headOffsetY, 0);
            headRect.position = tipRect.position + offset;
        }
    }

    void UpdateKnightPosition()
    {
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
//basicamente todos estos scripts lo que hacen es recibir los cambios de la vida del jugador, monedas
//Y pociones e implementarlos en el HUD,ademas del cambio de sprites