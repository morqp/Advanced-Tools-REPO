using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.VFX;
using static UnityEngine.EventSystems.EventTrigger;

public class TestManager : MonoBehaviour
{
    [Header("random seed for reproducibility")]
    public int randomSeed = 12345;

    [Header("prefab to test (convex or compound)")]
    public GameObject testPrefab;

    [Header("spawn count settings")]
    public int minimumSpawnCount = 200;
    public int maximumSpawnCount = 5000;
    public int spawnCountIncrement = 250;

    [Header("extra big jumps")]
    public int[] extraSpawnCounts = { 6000, 7000, 8000, 9000, 10000 };

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
    private List<int> spawnCountList;

    void Start()
    {
        // prepare the csv path
        csvFilePath = Path.Combine(Application.persistentDataPath, csvFileName);

        //make spawn count list by adding the extra steps to the increments
        spawnCountList = new List<int>();
        for (int i = minimumSpawnCount; i <= maximumSpawnCount; i+=spawnCountIncrement)
        {
            spawnCountList.Add(i);
        }
        foreach (int extraincrements in extraSpawnCounts)
        {
            if (!spawnCountList.Contains(extraincrements) && extraincrements > maximumSpawnCount)
            {
                spawnCountList.Add(extraincrements);
            }
        }

       
        StartCoroutine(RunTests());
    }

    private IEnumerator RunTests()
    {
        foreach( int currentCount  in spawnCountList)
        {
            Random.InitState(randomSeed);

            SpawnTestObjects(currentCount);
            Debug.Log("starting test for  " + currentCount + "  instances");

            // warming up
            yield return new WaitForSeconds(warmupDuration);

            // clear any data
            frameTimeList.Clear();
            collisionCountList.Clear();
            float elapsedTime = 0f;

            // measure
            while (elapsedTime < measurementDuration)
            {
                float deltaTime = Time.deltaTime;
                frameTimeList.Add(deltaTime);

                // record all collisions logged in this frame
                collisionCountList.Add(CollisionLogger.globalCollisionCount);
                CollisionLogger.globalCollisionCount = 0;

                elapsedTime += deltaTime;
                yield return null;
            }

            // write results
            float averageDeltaTime = frameTimeList.Average();
            int totalChecks = collisionCountList.Sum();
            WriteCsvLine(testPrefab.name, currentCount, averageDeltaTime, totalChecks);
            Debug.Log("logged result for  " + currentCount + "  instances");

            CleanupTestObjects();
            yield return null;
        }

        Debug.Log("all tests complete. results at: " + csvFilePath);
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

    private void WriteCsvLine(string testCase, int spawnCount, float averageDeltaTime, int totalChecks)
    {
        if (!csvHeaderWritten)
        {
            string headerLine = "testCase,spawnCount,averageDeltaTime,totalCollisionChecks";
            File.WriteAllText(csvFilePath, headerLine + "\n");
            csvHeaderWritten = true;
        }

        string dataLine = testCase + ","
                        + spawnCount + ","
                        + averageDeltaTime.ToString("F4") + ","
                        + totalChecks + "\n";
        File.AppendAllText(csvFilePath, dataLine);
    }
}
