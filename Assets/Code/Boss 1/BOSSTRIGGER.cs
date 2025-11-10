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
            Debug.Log("Jefe derrotado, no se activará.");
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

            // Cierra la puerta detrás del jugador
            if (puertaEntrada != null)
                puertaEntrada.CerrarPuerta();

            StartCoroutine(IniciarSecuenciaJefe());
            StartCoroutine(ActivarCooldown());
        }
    }

    private IEnumerator IniciarSecuenciaJefe()
    {
        Debug.Log("🌀 Iniciando secuencia de introducción del jefe...");
        enPelea = true;

        var playerController = player.GetComponent<PlayerMovement>();
        if (playerController != null) playerController.canMove = false;

        // Cierra las puertas del área del jefe
        foreach (BossDoor puerta in puertasArena)
            if (puerta != null) puerta.CerrarPuerta();

        // Instanciar jefe pero desactivado
        GameObject bossObj = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
        spawnedBoss = bossObj.GetComponent<BossLife>();
        bossObj.SetActive(false);

        // Detener música actual
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        // Cámara (si hay animación)
        if (cameraAnimator != null)
            cameraAnimator.SetTrigger("BossIntro");

        // Mostrar nombre del jefe
        if (BossNameUI.Instance != null)
            BossNameUI.Instance.MostrarNombre(nombreJefe);

        // Esperar duración de la intro
        yield return new WaitForSeconds(introDuracion);

        // Activar jefe
        bossObj.SetActive(true);

        // Reproducir efecto de inicio (como en Undertale)
        if (AudioManager.Instance != null && sfxInicioBatalla != null)
            AudioManager.Instance.PlaySFX(sfxInicioBatalla);

        // Comenzar música del jefe
        if (AudioManager.Instance != null && musicaJefe != null)
            AudioManager.Instance.PlaySFX(sfxInicioBatalla);

        // Habilitar movimiento del jugador
        if (playerController != null) playerController.canMove = true;

        Debug.Log("⚔️ ¡Comienza la batalla de jefe!");
    }

    public void JefeDerrotado()
    {
        Debug.Log("💀 Jefe derrotado.");
        enPelea = false;

        // Abrir puertas del área del jefe
        foreach (BossDoor puerta in puertasArena)
            if (puerta != null) puerta.AbrirPuerta();

        // Guardar progreso
        if (ControladorDatosJuego.Instance != null &&
            !ControladorDatosJuego.Instance.datosjuego.jefesDerrotados.Contains(bossID))
        {
            ControladorDatosJuego.Instance.datosjuego.jefesDerrotados.Add(bossID);
            ControladorDatosJuego.Instance.GuardarDatos();
            Debug.Log("💾 Progreso guardado.");
        }

        // Desactivar trigger
        gameObject.SetActive(false);
    }

    private IEnumerator ActivarCooldown()
    {
        enCooldown = true;
        yield return new WaitForSeconds(cooldownTiempo);
        enCooldown = false;
    }
}
