using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");
    private static int baseMapId = Shader.PropertyToID("_BaseMap");

    [SerializeField] private Mesh mesh = default;
    [SerializeField] private Material material = default;
    [SerializeField] private Texture baseTexture = default;

    public float Radius;

    private Matrix4x4[] matrices = new Matrix4x4[1023];
    private Vector4[] colors = new Vector4[1023];
    private float[] metallicValues = new float[1023];
    private float[] smoothnessValues = new float[1023];

    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        for (int i = 0; i < matrices.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * Radius,
                Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f), 
                Vector3.one * Random.Range(0.5f, 1.5f));
            colors[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1f));
            metallicValues[i] = Random.value < 0.25 ? 1 : 0;
            smoothnessValues[i] = Random.Range(0.05f, 0.95f);
        }
    }

    private void Update()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetVectorArray(baseColorId, colors);
            propertyBlock.SetFloatArray(metallicId, metallicValues);
            propertyBlock.SetFloatArray(smoothnessId, smoothnessValues);
            if (baseTexture != default)
            {
                propertyBlock.SetTexture(baseMapId, baseTexture);
            }
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, matrices.Length, propertyBlock);
    }
}