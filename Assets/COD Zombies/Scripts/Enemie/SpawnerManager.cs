using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpawnerManager : NetworkBehaviour
{
    public static SpawnerManager Instance; // ✅ Singleton

    [Header("Configuración Spawns")]
    public List<Transform> spawnPoints = new List<Transform>();
    public GameObject enemyPrefab;

    [Header("Rondas")]
    public int enemiesPerRound = 10;
    private int enemiesSpawned = 0;
    private int enemiesAlive = 0;
    private int currentRound = 0;

    [Header("Delay")]
    public float delayBetweenRondas = 5f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(StartRounds());
    }

    IEnumerator StartRounds()
    {
        while (true)
        {
            currentRound++;
            enemiesSpawned = 0;
            enemiesAlive = 0;

            Debug.Log($"Ronda {currentRound} iniciada");

            while (enemiesSpawned < enemiesPerRound)
            {
                SpawnEnemy();
                enemiesSpawned++;
                enemiesAlive++;
                yield return new WaitForSeconds(0.5f);
            }

            while (enemiesAlive > 0)
            {
                yield return null;
            }

            Debug.Log($"Ronda {currentRound} finalizada");

            yield return new WaitForSeconds(delayBetweenRondas);
        }
    }

    [Server]
    void SpawnEnemy()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No hay puntos de spawn asignados.");
            return;
        }

        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject enemy = Instantiate(enemyPrefab, spawn.position, spawn.rotation);
        NetworkServer.Spawn(enemy);

        Enemigo enemigoScript = enemy.GetComponent<Enemigo>();
        if (enemigoScript != null)
        {
            enemigoScript.OnEnemyDeath += EnemyDied; // ✅ Suscribirse al evento
        }
    }

    [Server]
    public void EnemyDied()
    {
        enemiesAlive--;
        Debug.Log($"Enemigo murió. Quedan: {enemiesAlive}");
    }
}
