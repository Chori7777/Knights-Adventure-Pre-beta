using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoMuerte;

    private string[] frasesMuerte = new string[]
    {
        "Decepcionante",
        "Es esto lo que eres ahora?",
        "...",
        "Hace frio...",
        "Fue intencional?",
        "No importa cuantas veces mueras, siempre volveras",
        "Hola",
    };

    void Start()
    {
        MostrarTextoMuerte();

        // Verificar que ControladorDatosJuego existe
        if (ControladorDatosJuego.Instance == null)
        {
            Debug.LogError("⚠️ ControladorDatosJuego no encontrado! Debe estar en la primera escena con DontDestroyOnLoad");
        }
        else
        {
            Debug.Log($"✅ Checkpoint disponible - Escena: {ControladorDatosJuego.Instance.datosjuego.escenaActual}");
        }
    }

    public void MostrarTextoMuerte()
    {
        int randomIndex = Random.Range(0, frasesMuerte.Length);
        textoMuerte.text = frasesMuerte[randomIndex];
    }

    // Volver al último checkpoint
    public void Reintentar()
    {
        Debug.Log("=== REINTENTAR DESDE CHECKPOINT ===");

        if (ControladorDatosJuego.Instance == null)
        {
            Debug.LogError("⚠️ No se puede reintentar: ControladorDatosJuego no existe");
            return;
        }

        // Cargar datos del checkpoint
        ControladorDatosJuego.Instance.CargarDatos();

        // Obtener escena guardada
        string escenaCheckpoint = ControladorDatosJuego.Instance.datosjuego.escenaActual;

        if (string.IsNullOrEmpty(escenaCheckpoint))
        {
            Debug.LogError("⚠️ No hay escena guardada en el checkpoint");
            return;
        }

        Debug.Log($"✅ Cargando checkpoint en escena: {escenaCheckpoint}");

        // Cargar la escena del checkpoint
        SceneManager.LoadScene(escenaCheckpoint);
    }

    // Reiniciar el nivel completo (volver al inicio del nivel actual)
    public void ReiniciarNivel()
    {
        Debug.Log("=== REINICIAR NIVEL COMPLETO ===");

        if (ControladorDatosJuego.Instance == null)
        {
            Debug.LogError("⚠️ ControladorDatosJuego no existe");
            return;
        }

        // Obtener la última escena donde estaba el jugador
        string escenaActual = ControladorDatosJuego.Instance.datosjuego.escenaActual;

        if (string.IsNullOrEmpty(escenaActual))
        {
            Debug.LogError("⚠️ No se puede reiniciar: no hay escena guardada");
            return;
        }

        // Resetear posición al inicio del nivel (opcional)
        // ControladorDatosJuego.Instance.datosjuego.posicion = Vector3.zero;

        Debug.Log($"✅ Reiniciando nivel: {escenaActual}");

        // Recargar la escena
        SceneManager.LoadScene(escenaActual);
    }

    // Rendirse y volver al menú
    public void Rendirse()
    {
        Debug.Log("=== RENDIRSE ===");

        // Opcional: Resetear datos del juego
        // if (ControladorDatosJuego.Instance != null)
        // {
        //     ControladorDatosJuego.Instance.ResetearDatos();
        // }

        // Ir al menú principal
        SceneManager.LoadScene("MenuPrincipal"); // Ajusta el nombre de tu menú
    }
}