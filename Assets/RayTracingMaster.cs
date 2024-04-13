/**
 * http://blog.three-eyed-games.com/2018/05/03/gpu-ray-tracing-in-unity-part-1/
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Sphere
{
    public Vector3 position;
    public float radius;
    public Vector3 albedo;
    public Vector3 specular;
};


public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    public Light DirectionalLight; 
    
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;
    private ComputeBuffer _sphereBuffer;

    private RenderTexture _target;
    private Camera _camera;

    private uint _currentSample = 0;
    private Material _addMaterial;

    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    private void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();

        // Add random number of spheres
        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();

            // Radius and radius
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            // Delete intersecting spheres
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)  // AABB intersection
                    goto SkipSphere;
            }

            // Set albedo and specular colour
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;

            spheres.Add(sphere);  // Add sphere to list

        SkipSphere:
            continue;
        }

        // Assign sphere to compute buffer
        _sphereBuffer = new ComputeBuffer(spheres.Count, 40);  // 40 is the stride of the buffer (byte size of sphere in memory)
        _sphereBuffer.SetData(spheres);                        // Calculated by number of floats in Sphere struct * flote byte size (4 bytes) 
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>(); 
    }

    private void Update()
    {
        if (transform.hasChanged || DirectionalLight.transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
            DirectionalLight.transform.hasChanged = false;
        }
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);

        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", 
            new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release texture if have
            if (_target != null)
            {
                _target.Release();
            }

            // Render target for raytracing
            _target = new RenderTexture(Screen.width, Screen.height, 0
                , RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
            _currentSample = 0;  // Reset samples for antialiasing when camera moves
        }
    }

    
    private void Render(RenderTexture dest)
    {
        // Create render target
        InitRenderTexture();

        // Set Target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        
        Graphics.Blit(_target, dest, _addMaterial);
        _currentSample++;
    }

    // Called when camera finishes rendering
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        SetShaderParameters();
        Render(dest);
    }
}