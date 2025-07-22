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
    public int hp = 100;
    public int maxHp = 100;

    [SyncVar(hook = nameof(OnScoreChanged))]
    private int score = 0;

    private PlayerUIManager localUI;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (camTransform != null)
            camTransform.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        localUI = Object.FindFirstObjectByType<PlayerUIManager>();

        if (localUI != null)
        {
            localUI.UpdateUI(gameObject.name, (float)hp / maxHp, score);
        }
    }


    void Start()
    {
        _rb = GetComponent<Rigidbody>();

        if (!isLocalPlayer)
        {
            // Desactivar la cámara para otros jugadores
            if (camTransform != null)
                camTransform.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer || !isAlive) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        _moveDirection = new Vector3(h, 0, v);

        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 worldMove = Quaternion.LookRotation(flatForward) * _moveDirection;
        _rb.AddForce(worldMove * moveSpeed, ForceMode.Impulse);

        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > speedLimit)
        {
            Vector3 limited = horizontalVelocity.normalized * speedLimit;
            _rb.linearVelocity = new Vector3(limited.x, _rb.linearVelocity.y, limited.z);
        }

        _rb.AddForce(Vector3.down * 50f, ForceMode.Acceleration);

        float inputSpeed = new Vector2(h, v).magnitude;
        CmdSetSpeed(inputSpeed);
    }

    void Update()
    {
        if (!isLocalPlayer || !isAlive) return;

        _yaw += Input.GetAxis("Mouse X") * camSpeed;
        _pitch -= Input.GetAxis("Mouse Y") * camSpeed;
        _pitch = Mathf.Clamp(_pitch, -maxPitch, maxPitch);

        transform.eulerAngles = new Vector3(0, _yaw, 0);
        camTransform.localEulerAngles = new Vector3(_pitch, 0, 0);

        if (Input.GetMouseButtonDown(0))
        {
            Disparar();
        }
    }

    void Disparar()
    {
        Ray ray = new Ray(camTransform.position, camTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.CompareTag("Enemigo"))
            {
                NetworkIdentity netId = hit.collider.GetComponent<NetworkIdentity>();
                if (netId != null)
                {
                    CmdDispararEnemigo(netId);
                }
            }
        }
    }

    [Command]
    void CmdDispararEnemigo(NetworkIdentity enemyId)
    {
        if (enemyId != null && enemyId.TryGetComponent<Enemigo>(out Enemigo enemigo))
        {
            enemigo.TakeDamage(10, netIdentity);
        }
    }

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
        }
    }

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
        if (!isLocalPlayer) return;

        float hpPercent = (float)newHp / maxHp;
        localUI?.UpdateUI(gameObject.name, hpPercent, score);
    }

    void OnScoreChanged(int oldScore, int newScore)
    {
        if (!isLocalPlayer) return;

        localUI?.UpdateUI(gameObject.name, (float)hp / maxHp, newScore);
    }

    void OnAliveChanged(bool oldAlive, bool newAlive)
    {
        if (!isLocalPlayer) return;

        if (!newAlive)
        {
            Debug.Log("Jugador muerto");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (camTransform != null)
                camTransform.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Jugador revivido");
            hp = maxHp;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (camTransform != null)
                camTransform.gameObject.SetActive(true);
        }
    }

    [Command]
    void CmdSetSpeed(float speed)
    {
        netSpeed = speed;
    }

    public bool EstaVivo()
    {
        return isAlive;
    }

}
