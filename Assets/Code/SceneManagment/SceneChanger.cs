using UnityEngine;
using UnityEngine.SceneManagement;

public class PuertaTransicion : MonoBehaviour
{
    [SerializeField] private string escenaDestino;
    [SerializeField] private Vector2 posicionSpawn; // Posición exacta de spawn

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Guarda la posición de spawn
            GameManager.instance.datos.posicion = posicionSpawn;
            Debug.Log("Guardando spawn: " + posicionSpawn);

            // Cambia de escena
            SceneManager.LoadScene(escenaDestino);
        }
    }
}