using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ControladorDatosJuego : MonoBehaviour
{
   //Instancias
    private static ControladorDatosJuego instance;
    public static ControladorDatosJuego Instance => instance;

    // todo sobre el guardado e info del jugador esta aqui, para ser usada mas adelante
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

    //Cuando el juego inicia

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


    // Cargar datos del juego


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

    private bool SaveFileExists()
    {
        return File.Exists(saveFilePath);
    }
    //Sirve para crear y establecer los datos del juego si todavia no existian
    private void CreateNewSaveData()
    {
        Debug.Log("No se encontró archivo de guardado,creando");
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
            //Esto sirve por si hay algun error y facilitar su deteccion
        }
    }

 

    private void ScheduleApplyData()
    {
        Invoke(nameof(AplicarDatosAlJugador), RETRY_DELAY);
    }
//Se guarda y escribe en el archivo de guardado

    public void GuardarDatos()
    {
        if (!FindPlayer())
        {
            Debug.LogWarning("No se encontró el jugador para guardar");
            return;
        }

        CaptureAllData();
        WriteSaveFile();
        LogSavedData();
    }

    private bool FindPlayer()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        return player != null;
    }
    //Aca es donde se recompila todo lo que se guarda, tanto escenas como camara
    private void CaptureAllData()
    {
        CapturePlayerData();
        CaptureSceneData();
        CaptureCameraData();
    }
    //Aca es donde se guarda toda la informacion del jugador
    private void CapturePlayerData()
    {
        CapturePosition();
        CaptureHealthData();
        CaptureAbilitiesData();
    }
    //Todo esto guarda la vida, la posicion,las habilidades y la camara
    private void CapturePosition()
    {
        datosjuego.posicion = player.transform.position;
    }

    private void CaptureHealthData()
    {
        playerLife vidaScript = player.GetComponent<playerLife>();
        if (vidaScript == null) return;

        // Usar las nuevas propiedades públicas
        datosjuego.vidaActual = vidaScript.Health;
        datosjuego.vidaMaxima = vidaScript.MaxHealth;
        datosjuego.cantidadpociones = vidaScript.Potions;
        datosjuego.maxPotions = vidaScript.MaxPotions; // ✅ NUEVO

        // Backup en PlayerPrefs
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

    private void CaptureSceneData()
    {
        datosjuego.escenaActual = SceneManager.GetActiveScene().name;
    }

    private void CaptureCameraData()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            datosjuego.posicionCamara = cam.transform.position;
        }
    }
    //Escribir en el .JSON
    private void WriteSaveFile()
    {
        try
        {
            string cadenaJSON = JsonUtility.ToJson(datosjuego, true);
            File.WriteAllText(saveFilePath, cadenaJSON);
        }
        catch (System.Exception)
        {
            Debug.LogError(" Error al guardar datos");
        }
    }

    private void LogSavedData()
    {
        Debug.Log("Los datos se cargaron de la forma correcta");
    }

  //Seccion para encontrar al jugador y aplicarle los cambios que se guardan

    private void AplicarDatosAlJugador()
    {
        if (!TryFindPlayer())
        {
            RetryApplyData();
            return;
        }

        ApplyAllData();
        Debug.Log(" Datos aplicados al jugador");
    }

    private bool TryFindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        return player != null;
    }

    private void RetryApplyData()
    {
        Debug.LogWarning("Jugador no encontrado");
        Invoke(nameof(AplicarDatosAlJugador), RETRY_DELAY);
    }
    //se aplican todos los cambios
    private void ApplyAllData()
    {
        ApplyPosition();
        ApplyHealthData();
        ApplyAbilitiesData();
        ApplyCameraData();
    }

    private void ApplyPosition()
    {
        player.transform.position = datosjuego.posicion;
    }
    //Resuelve el bug de la salud al cargar partida
    private void ApplyHealthData()
    {
        playerLife vidaScript = player.GetComponent<playerLife>();
        if (vidaScript == null) return;

        vidaScript.SetHealth(datosjuego.vidaActual);
        vidaScript.SetMaxHealth(datosjuego.vidaMaxima);
        vidaScript.SetPotions(datosjuego.cantidadpociones);

        // Restaurar pociones máximas desde datos o PlayerPrefs
        if (datosjuego.maxPotions > 0)
        {
            vidaScript.SetMaxPotions(datosjuego.maxPotions); 
        }
        else if (PlayerPrefs.HasKey(KEY_MAX_POTIONS))
        {
            vidaScript.SetMaxPotions(PlayerPrefs.GetInt(KEY_MAX_POTIONS));
        }
    }
    // Resuelve el bug de las habilidades al cargar partida
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
    //Resuelve el bug de la camara al cargar partida
    private void ApplyCameraData()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 newPosition = new Vector3
        (
            datosjuego.posicionCamara.x,
            datosjuego.posicionCamara.y,
            cam.transform.position.z 
        );

        cam.transform.position = newPosition;
    }


    // Sistema para guardar Checkpoint.
    // se guarda la posicion del jugador  la escena actual y la posicion de la camara


    public void GuardarCheckpoint(Vector3 playerPosition)
    {
        datosjuego.posicion = playerPosition;
        datosjuego.escenaActual = SceneManager.GetActiveScene().name;

        // Capturar posición de cámara
        Camera cam = Camera.main;
        if (cam != null)
        {
            datosjuego.posicionCamara = cam.transform.position;
        }

        GuardarDatos();
        Debug.Log("Checkpoint guardado");
    }

    public void AgregarMonedas(int cantidad)
    {
        datosjuego.cantidadMonedas += cantidad;
        GuardarDatos();

        // Avisar al HUD
        if (PlayerHealthUI.Instance != null)
        {
            PlayerHealthUI.Instance.ActualizarMonedas(datosjuego.cantidadMonedas);
        }
    }

    public int ObtenerMonedas()
    {
        return datosjuego.cantidadMonedas;
    }

    public void ReiniciarMonedas()
    {
        datosjuego.cantidadMonedas = 0;
        GuardarDatos();

        if (PlayerHealthUI.Instance != null)
        {
            PlayerHealthUI.Instance.ActualizarMonedas(0);
        }
    }
    //Caso hipotetico donde se quieran resetear los datos del juego

    public void ResetearDatos()
    {
        datosjuego = new DatosJuego();
        ClearAllPlayerPrefs();

        Debug.Log("Datos borrados");
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
//Usos diversos 
    public bool ExistePartidaGuardada()
    {
        return SaveFileExists();
    }

    public void BorrarPartidaGuardada()
    {
        if (SaveFileExists())
        {
            File.Delete(saveFilePath);
            ResetearDatos();
            Debug.Log(" Partida guardada borrada");
        }
    }
   
}

