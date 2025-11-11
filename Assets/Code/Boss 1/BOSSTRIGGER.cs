using UnityEngine;
using System.Collections;

public class BossTrigger : MonoBehaviour
{
    [Header("Configuración del Jefe")]
    [SerializeField] private string bossID = "Boss1";
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Vector3 bossSpawnPosition;
    [SerializeField] private BossDoor puertaEntrada; // 🚪 puerta que se cierra detrás del jugador
    [SerializeField] private BossDoor[] puertasArena; // 🔒 puertas que encierran la arena del jefe

    [Header("Opciones")]
    [SerializeField] private float cooldownTiempo = 1f;
    [SerializeField] private GameObject player;

    [Header("Intro del Jefe")]
    [SerializeField] private float introDuracion = 3f;
    [SerializeField] private AudioClip musicaJefe;
    [SerializeField] private AudioClip sfxInicioBatalla;
    [SerializeField] private Animator cameraAnimator;
    [SerializeField] private string nombreJefe;

    [Header("🔍 DEBUG")]
    [SerializeField] private bool debugMode = true;

    private bool enCooldown = false;
    private bool enPelea = false;
    private BossLife spawnedBoss;
    private bool jugadorDentro = false;

    void Start()
    {
        // Si ya fue derrotado, desactivar trigger
        if (ControladorDatosJuego.Instance != null &&
            ControladorDatosJuego.Instance.datosjuego.jefesDerrotados.Contains(bossID))
        {
            if (debugMode)
                Debug.Log($"✅ Jefe {bossID} ya derrotado, trigger desactivado.");

            gameObject.SetActive(false);
            return;
        }

        if (bossSpawnPosition == Vector3.zero)
            bossSpawnPosition = transform.position + new Vector3(2f, 0, 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !enCooldown && !enPelea)
        {
            jugadorDentro = true;

            if (debugMode)
                Debug.Log("🎮 Jugador entró en zona de jefe");

            // Cierra la puerta detrás del jugador
            if (puertaEntrada != null)
            {
                puertaEntrada.CerrarPuerta();
                if (debugMode)
                    Debug.Log("🚪 Puerta de entrada cerrada");
            }

            StartCoroutine(IniciarSecuenciaJefe());
            StartCoroutine(ActivarCooldown());
        }
    }

    private IEnumerator IniciarSecuenciaJefe()
    {
        if (debugMode)
            Debug.Log("🎬 Iniciando secuencia de introducción del jefe...");

        enPelea = true;

        var playerController = player.GetComponent<PlayerMovement>();
        if (playerController != null)
        {
            playerController.canMove = false;
            if (debugMode)
                Debug.Log("🔒 Movimiento del jugador bloqueado");
        }

        // Cierra las puertas del área del jefe
        foreach (BossDoor puerta in puertasArena)
        {
            if (puerta != null)
            {
                puerta.CerrarPuerta();
                if (debugMode)
                    Debug.Log($"🔒 Puerta de arena cerrada: {puerta.name}");
            }
        }

        // Instanciar jefe pero desactivado
        GameObject bossObj = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
        spawnedBoss = bossObj.GetComponent<BossLife>();

        // 🔥 CRÍTICO: Asignar referencia del trigger al jefe
        if (spawnedBoss != null)
        {
            spawnedBoss.SetBossTrigger(this);
            if (debugMode)
                Debug.Log("✅ Referencia del trigger asignada al jefe");
        }
        else
        {
            Debug.LogError("❌ ERROR: BossLife no encontrado en el prefab del jefe!");
        }

        bossObj.SetActive(false);

        // Detener música actual
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            if (debugMode)
                Debug.Log("🎵 Música anterior detenida");
        }

        // Cámara (si hay animación)
        if (cameraAnimator != null)
        {
            cameraAnimator.SetTrigger("BossIntro");
            if (debugMode)
                Debug.Log("📹 Animación de cámara iniciada");
        }

        // Mostrar nombre del jefe
        if (BossNameUI.Instance != null)
        {
            BossNameUI.Instance.MostrarNombre(nombreJefe);
            if (debugMode)
                Debug.Log($"📛 Mostrando nombre: {nombreJefe}");
        }

        // Esperar duración de la intro
        yield return new WaitForSeconds(introDuracion);

        // Activar jefe
        bossObj.SetActive(true);
        if (debugMode)
            Debug.Log("👹 Jefe activado");

        // Reproducir efecto de inicio
        if (AudioManager.Instance != null && sfxInicioBatalla != null)
        {
            AudioManager.Instance.PlaySFX(sfxInicioBatalla);
            if (debugMode)
                Debug.Log("🔊 SFX de inicio reproducido");
        }

        // 🔥 CORREGIDO: Comenzar música del jefe
        if (AudioManager.Instance != null && musicaJefe != null)
        {
            AudioManager.Instance.PlayMusic(musicaJefe); // ✅ AHORA USA PlayMusic
            if (debugMode)
                Debug.Log("🎵 Música del jefe iniciada");
        }
        else if (musicaJefe == null && debugMode)
        {
            Debug.LogWarning("⚠️ No hay música de jefe asignada (esto está bien si no quieres música)");
        }

        // Habilitar movimiento del jugador
        if (playerController != null)
        {
            playerController.canMove = true;
            if (debugMode)
                Debug.Log("🔓 Movimiento del jugador desbloqueado");
        }

        if (debugMode)
            Debug.Log("⚔️ ¡Comienza la batalla de jefe!");
    }

    public void JefeDerrotado()
    {
        if (debugMode)
            Debug.Log("💀 Jefe derrotado - Iniciando limpieza");

        enPelea = false;

        // Abrir puertas del área del jefe
        int puertasAbiertas = 0;
        foreach (BossDoor puerta in puertasArena)
        {
            if (puerta != null)
            {
                puerta.AbrirPuerta();
                puertasAbiertas++;
                if (debugMode)
                    Debug.Log($"🔓 Puerta abierta: {puerta.name}");
            }
        }

        if (debugMode)
            Debug.Log($"🔓 Total de puertas abiertas: {puertasAbiertas}");

        // Guardar progreso
        if (ControladorDatosJuego.Instance != null)
        {
            if (!ControladorDatosJuego.Instance.datosjuego.jefesDerrotados.Contains(bossID))
            {
                ControladorDatosJuego.Instance.datosjuego.jefesDerrotados.Add(bossID);
                ControladorDatosJuego.Instance.GuardarDatos();
                if (debugMode)
                    Debug.Log($"💾 Progreso guardado: {bossID}");
            }
            else if (debugMode)
            {
                Debug.Log($"⚠️ El jefe {bossID} ya estaba marcado como derrotado");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("⚠️ ControladorDatosJuego.Instance es null - no se guardó el progreso");
        }

        // Desactivar trigger
        gameObject.SetActive(false);
        if (debugMode)
            Debug.Log("✅ BossTrigger desactivado");
    }

    private IEnumerator ActivarCooldown()
    {
        enCooldown = true;
        yield return new WaitForSeconds(cooldownTiempo);
        enCooldown = false;
    }

    // 🔍 DEBUG: Visualizar en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 spawnPos = (bossSpawnPosition == Vector3.zero)
            ? transform.position + new Vector3(2f, 0, 0)
            : bossSpawnPosition;

        Gizmos.DrawWireSphere(spawnPos, 1f);
        Gizmos.DrawLine(spawnPos, spawnPos + Vector3.up * 3f);
    }
}