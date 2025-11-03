using UnityEngine;

public class DebugCheckpoint : MonoBehaviour
{
    void Update()
    {
        // Presiona "C" para ver info del checkpoint
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (ControladorDatosJuego.Instance != null)
            {
                var datos = ControladorDatosJuego.Instance.datosjuego;
                Debug.Log("=== INFO CHECKPOINT ===");
                Debug.Log($"Escena: {datos.escenaActual}");
                Debug.Log($"Posición: {datos.posicion}");
                Debug.Log($"Vida: {datos.vidaActual}/{datos.vidaMaxima}");
            }
            else
            {
                Debug.LogError("⚠️ ControladorDatosJuego NO EXISTE");
            }
        }
    }
}