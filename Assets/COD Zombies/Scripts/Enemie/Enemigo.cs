using UnityEngine;
using Mirror;
using System;

public class Enemigo : NetworkBehaviour
{
    [Header("Movimiento")]
    public float speed = 2f;
    public float detectionRange = 20f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    [Header("Vida")]
    [SyncVar]
    public int vida = 100;

    [Header("Animaciones")]
    public Animator animator;

    private Transform targetPlayer;
    private float nextUpdateTime = 0f;
    private float updateRate = 1f;
    private float lastAttackTime;

    public event Action OnEnemyDeath;

    private void Update()
    {
        if (!isServer) return;

        // Verificar si el objetivo actual está inactivo o muerto
        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy || targetPlayer.GetComponent<Jugador>()?.hp <= 0)
        {
            FindClosestPlayer(); // Buscar otro
        }

        if (Time.time >= nextUpdateTime)
        {
            FindClosestPlayer();
            nextUpdateTime = Time.time + updateRate;
        }

        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);

            if (distance <= attackRange)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    lastAttackTime = Time.time;
                    AttackPlayer();
                }

                SetAnimationMoving(false);
            }
            else if (distance <= detectionRange)
            {
                Vector3 direction = (targetPlayer.position - transform.position).normalized;
                transform.position += direction * speed * Time.deltaTime;
                SetAnimationMoving(true);
            }
            else
            {
                SetAnimationMoving(false);
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
        Transform closest = null;

        foreach (Jugador player in FindObjectsByType<Jugador>(FindObjectsSortMode.None))
        {
            if (!player.isActiveAndEnabled || player.hp <= 0) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = player.transform;
            }
        }

        targetPlayer = closest;
    }

    void AttackPlayer()
    {
        if (targetPlayer == null) return;

        Jugador jugador = targetPlayer.GetComponent<Jugador>();
        if (jugador != null)
        {
            jugador.TakeDamage(10);
        }

        RpcPlayHit();
    }

    [Command]
    public void CmdTakeDamage(int amount, NetworkIdentity attackerId)
    {
        TakeDamage(amount, attackerId);
    }

    public void TakeDamage(int amount, NetworkIdentity attackerId)
    {
        if (!isServer) return;

        vida -= amount;

        if (vida <= 0)
        {
            RpcPlayDeath();

            if (attackerId != null)
            {
                Jugador jugador = attackerId.GetComponent<Jugador>();
                if (jugador != null)
                {
                    jugador.AddPoints(10);
                }
            }

            OnEnemyDeath?.Invoke();

            Destroy(gameObject, 1.5f);
        }
        else
        {
            RpcPlayHit();
        }
    }

    void SetAnimationMoving(bool moving)
    {
        if (animator != null)
            animator.SetBool("Moving", moving);

        RpcSetMovingAnim(moving);
    }

    [ClientRpc]
    void RpcSetMovingAnim(bool moving)
    {
        if (!isServer && animator != null)
            animator.SetBool("Moving", moving);
    }

    [ClientRpc]
    void RpcPlayDeath()
    {
        if (animator != null)
            animator.SetTrigger("Death");
    }

    [ClientRpc]
    void RpcPlayHit()
    {
        if (animator != null)
            animator.SetTrigger("Hit");
    }
}
