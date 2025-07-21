using UnityEngine;
using Mirror;

public class Enemigo : NetworkBehaviour
{
    [Header("Stats")]
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 20;
    public int maxHealth = 20;
    public int damage = 5;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    [Header("References")]
    public Animator animator;
    private Rigidbody _rb;

    private float _attackTimer = 0f;
    private Transform targetPlayer;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        health = maxHealth;
    }

    void Update()
    {
        if (!isServer) return; // Solo servidor controla IA

        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy || Vector3.Distance(transform.position, targetPlayer.position) > 20f)
        {
            FindClosestPlayer();
        }

        _attackTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (!isServer) return;

        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);

            if (distance > attackRange)
            {
                MoveTowards(targetPlayer.position);
                SetAnimationMoving(true);
            }
            else
            {
                SetAnimationMoving(false);
                if (_attackTimer <= 0f)
                {
                    Attack(targetPlayer);
                    _attackTimer = attackCooldown;
                }
            }
        }
        else
        {
            SetAnimationMoving(false);
        }
    }

    void FindClosestPlayer()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestPlayer = null;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null) continue;
            GameObject playerGO = conn.identity.gameObject;
            if (playerGO == null || !playerGO.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, playerGO.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestPlayer = playerGO.transform;
            }
        }
        targetPlayer = closestPlayer;
    }

    void MoveTowards(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;
        Vector3 move = direction * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(transform.position + move);

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.fixedDeltaTime);
    }

    void Attack(Transform player)
    {
        Jugador jugador = player.GetComponent<Jugador>();
        if (jugador != null)
        {
            jugador.TakeDamage(damage);
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    [Server]
    public void TakeDamage(int amount, Jugador killer = null)
    {
        health -= amount;

        if (health <= 0)
        {
            Die(killer);
        }
    }

    [Server]
    void Die(Jugador killer)
    {
        if (killer != null)
        {
            killer.AddPoints(10);
        }

        RpcPlayDeath();

        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    void RpcPlayDeath()
    {
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
    }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        // UI o efectos aquí
    }

    void SetAnimationMoving(bool moving)
    {
        if (animator != null)
        {
            animator.SetBool("Moving", moving);
        }
    }
}
