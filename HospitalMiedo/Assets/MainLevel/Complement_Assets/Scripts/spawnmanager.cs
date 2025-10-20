using UnityEngine;

public class spawnmanager : MonoBehaviour
{
    public GameObject[] spawnPoints; // zonas de spawneo
    public GameObject[] enemies; // variantes de enemigos
    public int waveCount; //Cantidad de enemigos por oleada
    public int wave; //oleada actual
    public int enemiesCount; // cantidad de enemigos en escena
    public bool spawning; //volver a spawnear enemigos
    private int enemySpawned; // enemigos en escena

    void Start()
    {
        waveCount = 2;
        wave = 1;
        spawning = false;
        enemySpawned = 0;
        SpawnEnemies();
    }

    void Update()
    {
       
        if (!spawning && enemySpawned == 0)
        {
            wave++;
            waveCount += 2; // Aumenta la dificultad por oleada
            SpawnEnemies();
        }
    }

    void SpawnEnemies()
    {
        spawning = true;
        enemySpawned = 0;

        for (int i = 0; i < waveCount; i++)
        {
            // Selecciona un punto de spawn y un enemigo aleatorio
            GameObject spawnPoint = spawnPoints[Random.Range(1, spawnPoints.Length)];
            GameObject enemyPrefab = enemies[Random.Range(0, enemies.Length)];

            // Instancia el enemigo en la posición del punto de spawn
            Instantiate(enemyPrefab, spawnPoint.transform.position, Quaternion.identity);
            enemySpawned++;
        }

        enemiesCount = enemySpawned;
        spawning = false;
    }

    public void EnemyDefeated()
    {
        enemySpawned--;
    }
}