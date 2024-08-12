using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EntitySpawner : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;  // Prefab of the cube to be spawned
    [SerializeField] private int numberOfCubes = 10; // Number of cubes to spawn in a row
    [SerializeField] private float distanceFromCamera = 30.0f; // Distance of the cubes from the Camera
    [SerializeField] private float distanceBetweenCubes = 1.0f; // Distance between each cube in Unity units
    [SerializeField] private float offsetDistance = 1.5f; // Distance to offset the next bunch of cubes
    [SerializeField] private Button destroyEntitiesButton; // Reference to the Destroy Entities button

    private int spawnCount = 0; // Tracks how many times entities have been spawned
    private List<GameObject> spawnedCubes = new List<GameObject>(); // List to track all spawned cubes

    void Start()
    {
        destroyEntitiesButton.gameObject.SetActive(false); // Hide the button initially
        destroyEntitiesButton.onClick.AddListener(DestroyEntities); // Add listener to the button
    }

    public void SpawnEntities()
    {
        if (cubePrefab == null)
        {
            Debug.LogError("Cube Prefab is not assigned!");
            return;
        }

        // Get the camera's position and forward direction
        Camera mainCamera = Camera.main;
        Vector3 startPosition = mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera;

        // Calculate the row and column in the matrix based on spawnCount
        int row = spawnCount / 2;
        int column = spawnCount % 2;

        // Determine the offset for the current bunch
        Vector3 bunchOffset = mainCamera.transform.up * offsetDistance * row + -mainCamera.transform.forward * offsetDistance * column;

        // Apply the bunch offset to the start position
        startPosition += bunchOffset;

        // Spawn cubes to the right and left alternately
        for (int i = 0; i < numberOfCubes; i++)
        {
            Vector3 spawnPosition;
            if (i % 2 == 0)
            {
                // Spawn to the right
                spawnPosition = startPosition + mainCamera.transform.right * ((i / 2) * distanceBetweenCubes);
            }
            else
            {
                // Spawn to the left
                spawnPosition = startPosition - mainCamera.transform.right * (((i + 1) / 2) * distanceBetweenCubes);
            }

            GameObject newCube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity);
            spawnedCubes.Add(newCube); // Track the newly spawned cube
        }

        spawnCount++; // Increment the spawn count for the next bunch

        // Show the destroy button when cubes are spawned
        destroyEntitiesButton.gameObject.SetActive(true);
    }

    private void DestroyEntities()
    {
        foreach (GameObject cube in spawnedCubes)
        {
            Destroy(cube); // Destroy each cube
        }

        spawnedCubes.Clear(); // Clear the list of cubes

        // Hide the destroy button after all cubes are destroyed
        destroyEntitiesButton.gameObject.SetActive(false);
        spawnCount = 0; // Reset the spawn count
    }
}
