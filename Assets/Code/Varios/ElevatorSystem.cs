using UnityEngine;
using DG.Tweening;
public class ElevatorSystem : MonoBehaviour
{

    //Este script usa algo externo de unity, en la pagina de assets
    //"TWEENING" es un metodo que por lo que tengo entendido, usa calculos matematicos para suavizar y naturalizar movimiento
    //Por ejemplo: movimientos de ascensores, pinchos, para hacerlos ver mas naturales y menos estaticos
    [Header("Puntos de Movimiento")]
    [SerializeField] private Transform pointA; // Punto A
    [SerializeField] private Transform pointB; // Punto B
    [SerializeField] private bool startAtPointA = true; 

    [Header("Configuración")]
    [SerializeField] private float moveSpeed = 2f; // Velocidad en por segnudo
    [SerializeField] private Ease easeType = Ease.InOutSine; // Tipo de interpolación, esto despliega un Menu que te deja elegir el tipo
    [SerializeField] private float waitTimeAtFloor = 1f; // Tiempo de espera en las paradas que hace

    [Header("Activación")]
    [SerializeField] private bool moveOnPlayerEnter = true; // Cuando el jugador esta encima se activa
    [SerializeField] private bool canCallWithButton = true; // Puede llamarse con boton que aun no coloque
    [SerializeField] private LayerMask playerLayer; // Layer del jugador para detectarlo

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioClip moveSound; //Audios que todavia no implemente, pero ya estan ahi para simplemente colocarlos
    [SerializeField] private AudioClip arriveSound;
    private AudioSource audioSource;

    // booleanos
    private bool isAtPointA = true;
    private bool isMoving = false;
    private bool playerOnElevator = false;
    private Tween currentTween;

    // Referencias
    private Transform playerTransform;
    private Vector3 lastElevatorPosition;

    private void Start()
    {
        // Posicionar ascensor en punto inicial
        if (startAtPointA && pointA != null)
        {
            transform.position = pointA.position;
            isAtPointA = true;
        }
        else if (!startAtPointA && pointB != null)
        {
            transform.position = pointB.position;
            isAtPointA = false;
        }

        // el Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (moveSound != null || arriveSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        lastElevatorPosition = transform.position;

        // Crear puntos si no existen
    }


    private void Update()
    {
        // Mover jugador junto con el ascensor
        if (playerOnElevator && playerTransform != null)
        {
            Vector3 elevatorMovement = transform.position - lastElevatorPosition;
            playerTransform.position += elevatorMovement;
        }

        lastElevatorPosition = transform.position;
    }
    //Como lo detecta al jugador
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            // Verificar que el jugador esté encima
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) // El jugador está encima
                {
                    playerOnElevator = true;
                    playerTransform = collision.transform;

                    Debug.Log("?? Jugador subió al ascensor");

                    // Mover automáticamente si está configurado
                    if (moveOnPlayerEnter && !isMoving)
                    {
                        MoveToOppositeFloor();
                    }

                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform == playerTransform)
        {
            playerOnElevator = false;
            playerTransform = null;
        }
    }

    public void MoveToOppositeFloor()
    {
        if (isMoving) return;

        if (isAtPointA)
        {
            MoveToPointB();
        }
        else
        {
            MoveToPointA();
        }
    }

    public void MoveToPointA()
    {
        if (isMoving || isAtPointA) return;
        StartMove(pointA.position, true);
    }

    public void MoveToPointB()
    {
        if (isMoving || !isAtPointA) return;
        StartMove(pointB.position, false);
    }

    private void StartMove(Vector3 targetPosition, bool movingToA)
    {
        isMoving = true;

        // Calcular duración basada en distancia y velocidad
        float distance = Vector3.Distance(transform.position, targetPosition);
        float duration = distance / moveSpeed;

        // Reproducir el sonido cuando haya uno
        if (audioSource != null && moveSound != null)
        {
            audioSource.PlayOneShot(moveSound);
        }

        // se le suma el DotTween
        currentTween = transform.DOMove(targetPosition, duration)
            .SetEase(easeType)
            .OnComplete(() => OnArriveAtFloor(movingToA));

    }

    private void OnArriveAtFloor(bool arrivedAtA)
    {
        isMoving = false;
        isAtPointA = arrivedAtA;

       

        // Reproducir sonido de llegada
        if (audioSource != null && arriveSound != null)
        {
            audioSource.PlayOneShot(arriveSound);
        }

        // Esperar un momento en el piso
        DOVirtual.DelayedCall(waitTimeAtFloor, () =>
        {
        }  );
    }
//El funcionamiento del boton todavia no implementado
    public void CallElevator(bool callToPointA)
    {
        if (!canCallWithButton)
        {
            return;
        }

        if (isMoving)
        {
            return;
        }

        // Si el ascensor ya está en ese piso, no hacer nada
        if (callToPointA && isAtPointA)
        {
            Debug.Log("Ya esta aqui");
            return;
        }
        if (!callToPointA && !isAtPointA)
        {
            Debug.Log("Ya esta aqui");
            return;
        }

        // Mover al piso solicitado
        if (callToPointA)
        {
            MoveToPointA();
        }
        else
        {
            MoveToPointB();
        }
    }
    private void OnDrawGizmos()
    {
        if (pointA == null || pointB == null) return;

        // Línea punto A punto B
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pointA.position, pointB.position);

        // Punto A (inferior)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pointA.position, 0.5f);
        Gizmos.DrawWireCube(pointA.position, Vector3.one * 0.3f);

        // Punto B (superior)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pointB.position, 0.5f);
        Gizmos.DrawWireCube(pointB.position, Vector3.one * 0.3f);

        // Posición actual del ascensor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
//booleanos publicos
    public bool IsMoving => isMoving;
    public bool IsAtPointA => isAtPointA;
    public bool PlayerOnElevator => playerOnElevator;
}