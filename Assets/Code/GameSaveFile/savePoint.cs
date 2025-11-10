using UnityEngine;

public class savePoint : MonoBehaviour
{
    [Header("Configuración del Checkpoint")]
    [SerializeField] private AudioClip checkpoint;
    [SerializeField] private bool curarAlGuardar = true;

    private bool activado = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (activado) return;
        if (!collision.CompareTag("Player")) return;

        // Confirmamos que el ControladorDatosJuego esté activo
        if (ControladorDatosJuego.Instance == null)
        {
            Debug.LogError("❌ No existe instancia de ControladorDatosJuego en la escena!");
            return;
        }

        // Guardamos la posición del checkpoint
        Vector3 posicionCheckpoint = transform.position;
        ControladorDatosJuego.Instance.GuardarCheckpoint(posicionCheckpoint);
        Debug.Log($"✅ Checkpoint guardado en {posicionCheckpoint}");

        // Sonido del checkpoint
        if (checkpoint != null)
        {
            AudioSource.PlayClipAtPoint(checkpoint, transform.position);
        }

        // Curar jugador si está activado
        if (curarAlGuardar)
        {
            playerLife vida = collision.GetComponent<playerLife>();
            if (vida != null)
            {
                vida.HealFull();
                Debug.Log("💖 Vida restaurada al guardar en el checkpoint");
            }
        }

        // Marcamos como activado
        activado = true;
    }
}