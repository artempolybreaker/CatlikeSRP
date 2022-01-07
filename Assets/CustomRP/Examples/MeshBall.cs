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
            matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * Radius, Quaternion.identity, Vector3.one);
            colors[i] = new Vector4(Random.value, Random.value, Random.value, 1.0f);
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