using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject hudPrefab;

    private GameObject playerInstance;
    private GameObject hudInstance;

    private void Awake()
    {
        // Singleton para que solo exista uno
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGame()
    {
        // Instanciar jugador si no existe
        if (GameObject.FindWithTag("Player") == null && playerPrefab != null)
        {
            playerInstance = Instantiate(playerPrefab);
            playerInstance.name = "Player"; // opcional, para no tener "(Clone)"
        }
        else
        {
            playerInstance = GameObject.FindWithTag("Player");
        }

        // Instanciar HUD si no existe
        if (GameObject.FindWithTag("HUD") == null && hudPrefab != null)
        {
            hudInstance = Instantiate(hudPrefab);
            hudInstance.name = "HUD";
        }
        else
        {
            hudInstance = GameObject.FindWithTag("HUD");
        }
    }
}
