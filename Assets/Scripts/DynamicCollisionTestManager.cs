using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DynamicCollisionTestManager : MonoBehaviour
{
    [Header("prefab to test (convex or compound)")]
    public GameObject testPrefab;

    [Header("spawn count settings")]
    public int minimumSpawnCount = 200;
    public int maximumSpawnCount = 1200;
    public int spawnCountIncrement = 300;

    [Header("spawn area bounds")]
    [SerializeField] private Vector3 spawnAreaMin = new Vector3(-10f, 1f, -10f);
    [SerializeField] private Vector3 spawnAreaMax = new Vector3(10f, 10f, 10f);

    [Header("timing (in seconds)")]
    [SerializeField] private float warmupDuration = 5f;
    [SerializeField] private float measurementDuration = 20f;

    [Header("output file settings")]
    [SerializeField] private string csvFileName = "collision_test_results.csv";

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<float> frameTimeList = new List<float>();
    private List<int> collisionCountList = new List<int>();

    private bool csvHeaderWritten = false;
    private string csvFilePath;

    void Start()
    {
        // prepare the csv path
        csvFilePath = Path.Combine(Application.persistentDataPath, csvFileName);
        StartCoroutine(RunAllTests());
    }

    private IEnumerator RunAllTests()
    {
        for (int currentCount = minimumSpawnCount;
                 currentCount <= maximumSpawnCount;
                 currentCount += spawnCountIncrement)
        {
            SpawnTestObjects(currentCount);

            // warm-up period (no logging)
            yield return new WaitForSeconds(warmupDuration);

            // clear previous data
            frameTimeList.Clear();
            collisionCountList.Clear();
            float elapsedTime = 0f;

            // measurement loop
            while (elapsedTime < measurementDuration)
            {
                float deltaTime = Time.deltaTime;
                frameTimeList.Add(deltaTime);

                // record all collisions logged this frame
                collisionCountList.Add(CollisionLogger.globalCollisionCount);
                CollisionLogger.globalCollisionCount = 0;

                elapsedTime += deltaTime;
                yield return null;
            }

            // compute and write results
            float averageFps = 1f / frameTimeList.Average();
            int totalChecks = collisionCountList.Sum();
            WriteCsvLine(testPrefab.name, currentCount, averageFps, totalChecks);

            CleanupTestObjects();
            yield return null;
        }

        Debug.Log("all dynamic-prefab tests complete. results at: " + csvFilePath);
    }

    private void SpawnTestObjects(int numberToSpawn)
    {
        spawnedObjects.Clear();

        for (int i = 0; i < numberToSpawn; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                Random.Range(spawnAreaMin.z, spawnAreaMax.z)
            );

            GameObject newInstance = Instantiate(testPrefab, randomPosition, Random.rotation);
            Rigidbody instanceRigidbody = newInstance.GetComponent<Rigidbody>();
            if (instanceRigidbody != null)
            {
                instanceRigidbody.AddForce(Random.onUnitSphere * 5f, ForceMode.Impulse);
            }

            spawnedObjects.Add(newInstance);
        }
    }

    private void CleanupTestObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    private void WriteCsvLine(string colliderType, int spawnCount, float averageFps, int totalChecks)
    {
        if (!csvHeaderWritten)
        {
            string headerLine = "colliderType,spawnCount,averageFps,totalCollisionChecks";
            File.WriteAllText(csvFilePath, headerLine + "\n");
            csvHeaderWritten = true;
        }

        string dataLine = string.Format(
            "{0},{1},{2:F1},{3}\n",
            colliderType,
            spawnCount,
            averageFps,
            totalChecks
        );
        File.AppendAllText(csvFilePath, dataLine);
    }
}
