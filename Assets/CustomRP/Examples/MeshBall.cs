using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Mesh mesh = default;
    [SerializeField] private Material material = default;

    public float Radius;

    private Matrix4x4[] matrices = new Matrix4x4[1023];
    private Vector4[] colors = new Vector4[1023];

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
        }
    }

    private void Update()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetVectorArray(baseColorId, colors);
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, matrices.Length, propertyBlock);
    }
}