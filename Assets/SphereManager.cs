using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


public class SphereManager : MonoBehaviour
{
    [SerializeField] int seed = 0;
    [SerializeField] double mean = 0;
    [SerializeField] double std = 0.5;

    [SerializeField] int maxCount = 1;
    [SerializeField] float minRadius = 0.1f;
    [SerializeField] float maxRadius = 1.0f;

    [SerializeField] private Material[] _materials;

    private void Start()
    {
        GaussianRandom gen = new GaussianRandom(seed, mean, std);

        // Generate spheres
        for (int i = 0; i < maxCount; i++)
        {
            float radius = UnityEngine.Random.Range(minRadius, maxRadius);
            float posX = UnityEngine.Random.Range((float)(mean - 3 * std), (float)(mean + 3 * std));
            float posY = radius / 2;
            float posZ = UnityEngine.Random.Range((float)(mean - 3 * std), (float)(mean + 3 * std));

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = Vector3.one * radius;
            sphere.transform.position = new Vector3(posX, posY, posZ);
            sphere.transform.SetParent(gameObject.transform);

            // Assign random materials
            Material newMat = new Material(Shader.Find("Standard"));
            newMat.SetColor("_Color", UnityEngine.Random.ColorHSV());
            newMat.SetFloat("_Metallic", UnityEngine.Random.Range(0.0f, 1.0f));
            newMat.SetFloat("_Glossiness", UnityEngine.Random.Range(0.0f, 1.0f));
            sphere.GetComponent<MeshRenderer>().material = newMat;
        }
        Physics.SyncTransforms();  // Update colliders

        // Remove intersecting spheres
        List<Transform> removal = new List<Transform>();
        for (int i = 0; i < maxCount-1; i++)
        {
            for (int j = i + 1; j < maxCount; j++)
            {
                Transform s1 = transform.GetChild(i);
                Transform s2 = transform.GetChild(j);
                Bounds b1 = s1.GetComponent<SphereCollider>().bounds;
                Bounds b2 = s2.GetComponent<SphereCollider>().bounds;

                if (s1 != s2 &&
                    (!removal.Contains(s1) || !removal.Contains(s2)) &&
                    b1.Intersects(b2))
                {
                    removal.Add(s1);
                }
            }
        }
        foreach (Transform s in removal)
        {
            Destroy(s.gameObject);
        }
        UnityEngine.Debug.Log("Spheres generated: " + transform.childCount);
    }
}

/**
 * Generate Gaussian distribution with Box-Muller transform
 * https://stackoverflow.com/a/218600
 */
class GaussianRandom
{
    private System.Random gen;
    private double mean;
    private double stddev;

    public GaussianRandom(int seed, double mean, double stddev)
    {
        this.gen = new System.Random(seed);
        this.mean = mean;
        this.stddev = stddev;
    }

    public double Next()
    {
        double u1 = 1.0 - gen.NextDouble();
        double u2 = 1.0 - gen.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * 
            Math.Sin(2.0 * Math.PI * u2);
        double randNormal = mean + stddev * randStdNormal;
        return randNormal;
    }
}