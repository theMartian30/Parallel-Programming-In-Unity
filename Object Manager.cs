using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Math = Unity.Mathematics;

public class ObjectManager : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int numberOfObjects = 100;
    [SerializeField] private Transform centralPoint;
    [SerializeField] private Material nearMaterial;
    [SerializeField] private Material farMaterial;

    private GameObject[] allObjects;
    private NativeArray<Math.float3> positions;
    private NativeArray<float> distances;

    void Start()
    {
        float startTime = Time.realtimeSinceStartup;
        allObjects = new GameObject[numberOfObjects];
        positions = new NativeArray<Math.float3>(numberOfObjects, Allocator.Persistent);
        distances = new NativeArray<float>(numberOfObjects, Allocator.Persistent);

        // Spawn objects and store positions
        for (int i = 0; i < numberOfObjects; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-1100, 1100), 0, Random.Range(-3100, 3100));
            allObjects[i] = Instantiate(prefab, pos, Quaternion.identity);
            allObjects[i].AddComponent<MeshRenderer>();
            positions[i] = pos;
        }

        UpdateMaterials();
        float endTime = Time.realtimeSinceStartup;
        Debug.Log("Execution Time: " + (endTime - startTime) + " seconds");
    }

    void UpdateMaterials()
    {
        var job = new CalculateDistancesJob
        {
            positions = positions,
            centralPosition = centralPoint.position,
            distances = distances
        };
        var handle = job.Schedule(numberOfObjects, 64);
        handle.Complete();


        // Apply materials based on distances
        for (int i = 0; i < numberOfObjects; i++)
        {
            Material selectedMaterial = distances[i] < 1500.0f ? nearMaterial : farMaterial;
            MeshRenderer[] renderers = allObjects[i].GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.material = selectedMaterial;
            }
        }
    }

    [BurstCompile]
    private struct CalculateDistancesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Math.float3> positions;
        [ReadOnly] public Math.float3 centralPosition;
        public NativeArray<float> distances;

        public void Execute(int index)
        {
            distances[index] = Math.math.distance(centralPosition, positions[index]);
        }
    }

    void OnDestroy()
    {
        positions.Dispose();
        distances.Dispose();
    }
}
