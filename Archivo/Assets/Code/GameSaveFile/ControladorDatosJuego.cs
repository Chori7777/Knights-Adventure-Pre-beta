using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControladorDatosJuego : MonoBehaviour
{
    //Instancia Singleton
    private static ControladorDatosJuego instance;
    public static ControladorDatosJuego Instance => instance;

    [Header("Jugador")]
    public GameObject player;

    [Header("Datos")]
    public DatosJuego datosjuego = new DatosJuego();

    private string saveFilePath;
    private const string SAVE_FILE_NAME = "/datosjuego.json";
    private const float RETRY_DELAY = 0.1f;

    // PlayerPrefs Keys
    private const string KEY_DOUBLE_JUMP = "canDoubleJump";
    private const string KEY_DASH = "canDash";
    private const string KEY_WALL_CLING = "canWallCling";
    private const string KEY_THROW = "canThrowProjectile";
    private const string KEY_MAX_POTIONS = "maxPotions";

    // Checkpoint interno
    private Vector3 checkpointPos = Vector3.zero;
    private Vector3 checkpointCamara = Vector3.zero;
    private string checkpointEscena = "";

    private void Awake()
    {
        InitializeSingleton();
        InitializeSaveSystem();
    }

    private void InitializeSingleton()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSaveSystem()
    {
        saveFilePath = Application.persistentDataPath + SAVE_FILE_NAME;
        CargarDatos();
    }

    // -------------------------------------------------------------------
    // CARGA DE DATOS
    // -------------------------------------------------------------------
    public void CargarDatos()
    {
        if (!SaveFileExists())
        {
            CreateNewSaveData();
            return;
        }

        LoadSaveData();
        ScheduleApplyData();
    }

    private bool SaveFileExists() => File.Exists(saveFilePath);

    private void CreateNewSaveData()
    {
        Debug.Log("No se encontró archivo de guardado, creando uno nuevo");
        datosjuego = new DatosJuego();
    }

    private void LoadSaveData()
    {
        try
        {
            string contenido = File.ReadAllText(saveFilePath);
            datosjuego = JsonUtility.FromJson<DatosJuego>(contenido);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al cargar datos: {e.Message}");
            CreateNewSaveData();
        }
    }

    private void ScheduleApplyData() => Invoke(nameof(AplicarDatosAlJugador), RETRY_DELAY);

    // -------------------------------------------------------------------
    // GUARDADO DE DATOS
    // -------------------------------------------------------------------
    public void GuardarDatos()
    {
        if (!FindPlayer())
        {
            Debug.LogWarning("No se encontró el jugador para guardar");
            return;
        }

        CaptureAllData();
        WriteSaveFile();
        Debug.Log("Datos guardados correctamente");
    }

    private bool FindPlayer()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        return player != null;
    }

    private void CaptureAllData()
    {
        CapturePlayerData();
        CaptureSceneData();
        CaptureCameraData();
    }

    private void CapturePlayerData()
    {
        CapturePosition();
        CaptureHealthData();
        CaptureAbilitiesData();
    }

    private void CapturePosition() => datosjuego.posicion = player.transform.position;

    private void CaptureHealthData()
    {
        playerLife vidaScript = player.GetComponent<playerLife>();
        if (vidaScript == null) return;

        datosjuego.vidaActual = vidaScript.Health;
        datosjuego.vidaMaxima = vidaScript.MaxHealth;
        datosjuego.cantidadpociones = vidaScript.Potions;
        datosjuego.maxPotions = vidaScript.MaxPotions;

        PlayerPrefs.SetInt(KEY_MAX_POTIONS, vidaScript.MaxPotions);
    }

    private void CaptureAbilitiesData()
    {
        PlayerMovement movScript = player.GetComponent<PlayerMovement>();
        if (movScript == null) return;

        PlayerPrefs.SetInt(KEY_DOUBLE_JUMP, movScript.canDoubleJump ? 1 : 0);
        PlayerPrefs.SetInt(KEY_DASH, movScript.canDash ? 1 : 0);
        PlayerPrefs.SetInt(KEY_WALL_CLING, movScript.canWallCling ? 1 : 0);
        PlayerPrefs.SetInt(KEY_THROW, movScript.canThrowProjectile ? 1 : 0);
    }

    private void CaptureSceneData() => datosjuego.escenaActual = SceneManager.GetActiveScene().name;

    private void CaptureCameraData()
    {
        Camera cam = Camera.main;
        if (cam != null)
            datosjuego.posicionCamara = cam.transform.position;
    }

    private void WriteSaveFile()
    {
        try
        {
            string cadenaJSON = JsonUtility.ToJson(datosjuego, true);
            File.WriteAllText(saveFilePath, cadenaJSON);
        }
        catch (System.Exception)
        {
            Debug.LogError("Error al guardar datos");
        }
    }

    // -------------------------------------------------------------------
    // APLICACIÓN DE DATOS AL JUGADOR
    // -------------------------------------------------------------------
    private void AplicarDatosAlJugador()
    {
        if (!TryFindPlayer())
        {
            RetryApplyData();
            return;
        }

        ApplyAllData();
        Debug.Log("Datos aplicados al jugador");
    }

    private bool TryFindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        return player != null;
    }

    private void RetryApplyData()
    {
        Debug.LogWarning("Jugador no encontrado, reintentando...");
        Invoke(nameof(AplicarDatosAlJugador), RETRY_DELAY);
    }

    private void ApplyAllData()
    {
        ApplyPosition();
        ApplyHealthData();
        ApplyAbilitiesData();
        ApplyCameraData();
    }

    private void ApplyPosition() => player.transform.position = datosjuego.posicion;

    private void ApplyHealthData()
    {
        playerLife vidaScript = player.GetComponent<playerLife>();
        if (vidaScript == null) return;

        vidaScript.SetHealth(datosjuego.vidaActual);
        vidaScript.SetMaxHealth(datosjuego.vidaMaxima);
        vidaScript.SetPotions(datosjuego.cantidadpociones);

        if (datosjuego.maxPotions > 0)
            vidaScript.SetMaxPotions(datosjuego.maxPotions);
        else if (PlayerPrefs.HasKey(KEY_MAX_POTIONS))
            vidaScript.SetMaxPotions(PlayerPrefs.GetInt(KEY_MAX_POTIONS));
    }

    private void ApplyAbilitiesData()
    {
        PlayerMovement movScript = player.GetComponent<PlayerMovement>();
        if (movScript == null) return;

        if (PlayerPrefs.HasKey(KEY_DOUBLE_JUMP))
            movScript.canDoubleJump = PlayerPrefs.GetInt(KEY_DOUBLE_JUMP) == 1;
        if (PlayerPrefs.HasKey(KEY_DASH))
            movScript.canDash = PlayerPrefs.GetInt(KEY_DASH) == 1;
        if (PlayerPrefs.HasKey(KEY_WALL_CLING))
            movScript.canWallCling = PlayerPrefs.GetInt(KEY_WALL_CLING) == 1;
        if (PlayerPrefs.HasKey(KEY_THROW))
            movScript.canThrowProjectile = PlayerPrefs.GetInt(KEY_THROW) == 1;
    }

    private void ApplyCameraData()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 newPosition = new Vector3(
            datosjuego.posicionCamara.x,
            datosjuego.posicionCamara.y,
            cam.transform.position.z
        );

        cam.transform.position = newPosition;
    }

    // -------------------------------------------------------------------
    // SISTEMA DE CHECKPOINT Y RESPAWN
    // -------------------------------------------------------------------
    public void GuardarCheckpoint(Vector3 playerPosition)
    {
        checkpointPos = playerPosition;
        checkpointEscena = SceneManager.GetActiveScene().name;

        Camera cam = Camera.main;
        if (cam != null)
            checkpointCamara = cam.transform.position;

        datosjuego.posicion = checkpointPos;
        datosjuego.posicionCamara = checkpointCamara;
        datosjuego.escenaActual = checkpointEscena;

        GuardarDatos();
        Debug.Log($"✅ Checkpoint guardado en {checkpointPos}");
    }

    public bool HayCheckpointGuardado()
    {
        return checkpointPos != Vector3.zero || datosjuego.posicion != Vector3.zero;
    }

    public Vector3 ObtenerCheckpointPos() => (checkpointPos != Vector3.zero) ? checkpointPos : datosjuego.posicion;
    public Vector3 ObtenerCheckpointCamara() => (checkpointCamara != Vector3.zero) ? checkpointCamara : datosjuego.posicionCamara;
    public string ObtenerCheckpointEscena() => !string.IsNullOrEmpty(checkpointEscena) ? checkpointEscena : datosjuego.escenaActual;

    public void RespawnearJugadorEnCheckpoint()
    {
        if (!HayCheckpointGuardado())
        {
            Debug.LogWarning("⚠️ No hay checkpoint guardado, reiniciando escena.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        string escenaObjetivo = ObtenerCheckpointEscena();
        SceneManager.LoadScene(escenaObjetivo);
        instance.StartCoroutine(EsperarYColocarJugador());
    }

    private System.Collections.IEnumerator EsperarYColocarJugador()
    {
        yield return new WaitForSeconds(0.2f);

        GameObject nuevoPlayer = GameObject.FindGameObjectWithTag("Player");
        if (nuevoPlayer != null)
        {
            nuevoPlayer.transform.position = ObtenerCheckpointPos();

            playerLife vida = nuevoPlayer.GetComponent<playerLife>();
            if (vida != null)
                vida.SetHealth(vida.MaxHealth);

            Camera cam = Camera.main;
            if (cam != null)
                cam.transform.position = ObtenerCheckpointCamara();

            Debug.Log($"🔁 Jugador respawneado en checkpoint {ObtenerCheckpointPos()}");
        }
    }

    // -------------------------------------------------------------------
    // VARIOS
    // -------------------------------------------------------------------
    public void AgregarMonedas(int cantidad)
    {
        datosjuego.cantidadMonedas += cantidad;
        GuardarDatos();

        if (PlayerHealthUI.Instance != null)
            PlayerHealthUI.Instance.ActualizarMonedas(datosjuego.cantidadMonedas);
    }

    public int ObtenerMonedas() => datosjuego.cantidadMonedas;

    public void ReiniciarMonedas()
    {
        datosjuego.cantidadMonedas = 0;
        GuardarDatos();

        if (PlayerHealthUI.Instance != null)
            PlayerHealthUI.Instance.ActualizarMonedas(0);
    }

    public void ResetearDatos()
    {
        datosjuego = new DatosJuego();
        ClearAllPlayerPrefs();
        checkpointPos = Vector3.zero;
        checkpointEscena = "";
        checkpointCamara = Vector3.zero;
        Debug.Log("Datos y checkpoints reseteados");
    }

    private void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteKey(KEY_DOUBLE_JUMP);
        PlayerPrefs.DeleteKey(KEY_DASH);
        PlayerPrefs.DeleteKey(KEY_WALL_CLING);
        PlayerPrefs.DeleteKey(KEY_THROW);
        PlayerPrefs.DeleteKey(KEY_MAX_POTIONS);
        PlayerPrefs.Save();
    }

    public bool ExistePartidaGuardada() => SaveFileExists();

    public void BorrarPartidaGuardada()
    {
        if (SaveFileExists())
        {
            File.Delete(saveFilePath);
            ResetearDatos();
            Debug.Log("Partida guardada borrada");
        }
    }
}
