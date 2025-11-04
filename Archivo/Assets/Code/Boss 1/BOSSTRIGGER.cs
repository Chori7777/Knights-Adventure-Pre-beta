using UnityEngine;
using System.Collections;

public class BossTrigger : MonoBehaviour
{
    [Header("Configuración del Jefe")]
    [SerializeField] private string bossID = "Boss1";        // ID único
    [SerializeField] private GameObject bossPrefab;          // Prefab del jefe
    [SerializeField] private Vector3 bossSpawnPosition;      // Posición de spawn
    [SerializeField] private BossDoor[] doorsToClose;        // Puertas del boss

    [Header("Opciones")]
    [SerializeField] private float cooldownTiempo = 1f;
    [SerializeField] private GameObject player;

    private bool enCooldown = false;
    private bool enPelea = false;

    //  Referencia directa al jefe instanciado
    private BossLife spawnedBoss;

    void Start()
    {
        // Revisar si el jefe ya fue derrotado
        if (ControladorDatosJuego.Instance != null &&
            ControladorDatosJuego.Instance.datosjuego.jefesDerrotados.Contains(bossID))
        {
            Debug.Log("Jefe derrotado, no se activara");
            gameObject.SetActive(false);
        }

        if (bossSpawnPosition == Vector3.zero)
            bossSpawnPosition = transform.position + new Vector3(2f, 0, 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !enCooldown && !enPelea)
        {
            IniciarBatalla();
            StartCoroutine(ActivarCooldown());
        }
    }

    private void IniciarBatalla()
    {
        Debug.Log(" Iniciando batalla de jefe");
        enPelea = true;

        // Spawnear jefe y guardar referencia directa
        if (bossPrefab != null)
        {
            GameObject bossObj = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
            spawnedBoss = bossObj.GetComponent<BossLife>();
            if (spawnedBoss != null)
                spawnedBoss.SetBossTrigger(this); // Asigna referencia al trigger
        }

        CerrarPuertas();
    }

    private void CerrarPuertas()
    {
        foreach (BossDoor puerta in doorsToClose)
        {
            if (puerta != null)
                puerta.CerrarPuerta();
        }
    }

    // Este método será llamado por el jefe al morir
    public void JefeDerotado()
    {
        Debug.Log("Jefe fue derrotado");
        enPelea = false;
        AbrirPuertas();

        // Guarda que el jefe fue derrotado
        if (ControladorDatosJuego.Instance != null &&
            !ControladorDatosJuego.Instance.datosjuego.jefesDerrotados.Contains(bossID))
        {
            ControladorDatosJuego.Instance.datosjuego.jefesDerrotados.Add(bossID);
            ControladorDatosJuego.Instance.GuardarDatos();
            Debug.Log(" Progreso guardado");
        }

        // Desactiva el trigger
        gameObject.SetActive(false);
    }

    private void AbrirPuertas()
    {
        foreach (BossDoor puerta in doorsToClose)
        {
            if (puerta != null)
                puerta.AbrirPuerta();
        }
    }

    private IEnumerator ActivarCooldown()
    {
        enCooldown = true;
        yield return new WaitForSeconds(cooldownTiempo);
        enCooldown = false;
    }
}
