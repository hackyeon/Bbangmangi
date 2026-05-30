using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;
    public int enemyCount = 3;

    public Vector3[] spawnPositions =
    {
        new Vector3(3, 3, 3),
        new Vector3(-3, 3, 3),
        new Vector3(3, 3, -3)
    };

    void Start()
    {
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 position = spawnPositions[i % spawnPositions.Length];

            GameObject enemy = Instantiate(
                enemyPrefab,
                position,
                Quaternion.identity
            );

            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.target = player;

            EnemyAttackAI attackAI = enemy.GetComponent<EnemyAttackAI>();
            if (attackAI != null)
                attackAI.target = player;

            FallDetector fallDetector = enemy.GetComponent<FallDetector>();
            if (fallDetector != null)
                fallDetector.respawnPosition = position;
        }
    }
}