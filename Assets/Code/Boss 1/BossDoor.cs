using UnityEngine;

public class BossDoor : MonoBehaviour
{
    [Header("Estado Inicial")]
    [SerializeField] private bool empiezaAbierta = true;

    [Header("🔍 DEBUG")]
    [SerializeField] private bool debugMode = true;

    [Header("Collider (Opcional)")]
    [SerializeField] private Collider2D doorCollider; // Arrastra el collider aquí si quieres que se active/desactive

    private Animator animator;
    private bool estaAbierta;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError($"❌ {gameObject.name}: La puerta no tiene Animator!");
        }

        // Si no asignaste el collider manualmente, intenta encontrarlo
        if (doorCollider == null)
        {
            doorCollider = GetComponent<Collider2D>();
        }

        // Establecer estado inicial
        estaAbierta = empiezaAbierta;

        if (debugMode)
        {
            Debug.Log($"🚪 {gameObject.name} iniciada. Estado: {(estaAbierta ? "ABIERTA" : "CERRADA")}");
        }

        // Configurar collider según estado inicial
        if (doorCollider != null)
        {
            doorCollider.enabled = !estaAbierta; // Si está abierta, desactivar collider
        }

        // Establecer animación inicial
        if (animator != null)
        {
            if (estaAbierta)
            {
                animator.Play("Door_Open", 0, 1f); // Reproducir animación de abierta al final
            }
            else
            {
                animator.Play("Door_Closed", 0, 1f); // Reproducir animación de cerrada al final
            }
        }
    }

    public void AbrirPuerta()
    {
        if (debugMode)
        {
            Debug.Log($"🔓 {gameObject.name}: AbrirPuerta() llamado. Estado actual: {(estaAbierta ? "ABIERTA" : "CERRADA")}");
        }

        if (estaAbierta)
        {
            if (debugMode)
                Debug.LogWarning($"⚠️ {gameObject.name}: La puerta ya está abierta, no se hace nada.");
            return;
        }

        if (animator != null)
        {
            animator.SetTrigger("DoorOpen");
            if (debugMode)
                Debug.Log($"✅ {gameObject.name}: Trigger 'DoorOpen' activado");
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name}: No hay Animator asignado!");
        }

        estaAbierta = true;

        // Desactivar collider cuando se abre
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
            if (debugMode)
                Debug.Log($"🚫 {gameObject.name}: Collider desactivado");
        }
    }

    public void CerrarPuerta()
    {
        if (debugMode)
        {
            Debug.Log($"🔒 {gameObject.name}: CerrarPuerta() llamado. Estado actual: {(estaAbierta ? "ABIERTA" : "CERRADA")}");
        }

        if (!estaAbierta)
        {
            if (debugMode)
                Debug.LogWarning($"⚠️ {gameObject.name}: La puerta ya está cerrada, no se hace nada.");
            return;
        }

        if (animator != null)
        {
            animator.SetTrigger("DoorClosed");
            if (debugMode)
                Debug.Log($"✅ {gameObject.name}: Trigger 'DoorClosed' activado");
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name}: No hay Animator asignado!");
        }

        estaAbierta = false;

        // Activar collider cuando se cierra
        if (doorCollider != null)
        {
            doorCollider.enabled = true;
            if (debugMode)
                Debug.Log($"🛡️ {gameObject.name}: Collider activado");
        }
    }

    // 🔍 Método público para verificar el estado desde otros scripts
    public bool EstaAbierta()
    {
        return estaAbierta;
    }

    // 🔧 Método público para forzar un estado (útil para debugging)
    public void ForzarEstado(bool abrir)
    {
        if (abrir)
            AbrirPuerta();
        else
            CerrarPuerta();
    }
}