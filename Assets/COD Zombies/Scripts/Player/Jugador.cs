using UnityEngine;
using Mirror;

public class Jugador : NetworkBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 10f;
    public float speedLimit = 5f;

    [SyncVar(hook = nameof(OnAliveChanged))]
    private bool isAlive = true;

    [Header("Cámara")]
    public float camSpeed = 2f;
    public float maxPitch = 80f;
    public Transform camTransform;

    [Header("Animaciones")]
    public Animator animator;

    private Rigidbody _rb;
    private Vector3 _moveDirection;
    private float _yaw, _pitch;

    [SyncVar(hook = nameof(OnSpeedChanged))]
    private float netSpeed;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int hp = 100;
    private int maxHp = 100;

    [SyncVar]
    private int score = 0;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();

        if (isLocalPlayer)
        {
            camTransform.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            camTransform.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer || !isAlive) return;

        // Input de movimiento
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        _moveDirection = new Vector3(h, 0, v);

        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 worldMove = Quaternion.LookRotation(flatForward) * _moveDirection;
        _rb.AddForce(worldMove * moveSpeed, ForceMode.Impulse);

        // Limitar velocidad
        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > speedLimit)
        {
            Vector3 limited = horizontalVelocity.normalized * speedLimit;
            _rb.linearVelocity = new Vector3(limited.x, _rb.linearVelocity.y, limited.z);
        }

        _rb.AddForce(Vector3.down * 50f, ForceMode.Acceleration);

        // Enviar velocidad al servidor
        float inputSpeed = new Vector2(h, v).magnitude;
        CmdSetSpeed(inputSpeed);
    }

    void Update()
    {
        if (!isLocalPlayer || !isAlive) return;

        // Control de cámara
        _yaw += Input.GetAxis("Mouse X") * camSpeed;
        _pitch -= Input.GetAxis("Mouse Y") * camSpeed;
        _pitch = Mathf.Clamp(_pitch, -maxPitch, maxPitch);

        transform.eulerAngles = new Vector3(0, _yaw, 0);
        camTransform.localEulerAngles = new Vector3(_pitch, 0, 0);

        // Disparar con clic izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            Disparar();
        }
    }

    void Disparar()
    {
        Ray ray = new Ray(camTransform.position, camTransform.forward);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.CompareTag("Enemigo"))
            {
                Debug.Log("¡Disparo al enemigo!");
                hit.collider.GetComponent<Enemigo>()?.TakeDamage(10);
            }
        }
    }

    // Método para recibir daño
    [Server]
    public void TakeDamage(int amount)
    {
        if (!isAlive) return;

        hp -= amount;
        if (hp <= 0)
        {
            hp = 0;
            isAlive = false;
            Debug.Log($"Jugador {name} ha muerto.");
            // Aquí puedes agregar lógica de muerte (desactivar controles, etc)
        }
    }

    // Método para sumar puntos
    [Server]
    public void AddPoints(int amount)
    {
        score += amount;
        Debug.Log($"Jugador {name} obtuvo {amount} puntos. Total: {score}");
    }

    // Hooks SyncVar

    void OnSpeedChanged(float oldSpeed, float newSpeed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", newSpeed);
        }
    }

    void OnHealthChanged(int oldHp, int newHp)
    {
        Debug.Log($"HP cambió de {oldHp} a {newHp}");
        // Aquí podrías actualizar UI o efectos visuales
    }

    void OnAliveChanged(bool oldAlive, bool newAlive)
    {
        if (!newAlive)
        {
            Debug.Log("Jugador muerto");
            if (isLocalPlayer)
            {
                // Por ejemplo: bloquear controles, mostrar UI muerte
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            camTransform.gameObject.SetActive(false);
            // Desactivar otras cosas si quieres
        }
        else
        {
            Debug.Log("Jugador revivido");
            hp = maxHp;
            if (isLocalPlayer)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                camTransform.gameObject.SetActive(true);
            }
            // Reactivar otras cosas
        }
    }

    // Comandos

    [Command]
    void CmdSetSpeed(float speed)
    {
        netSpeed = speed;
    }
}
