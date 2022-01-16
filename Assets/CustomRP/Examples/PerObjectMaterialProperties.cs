using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int cutoffId = Shader.PropertyToID("_Cutoff");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");

    private static MaterialPropertyBlock propertyBlock;

    [SerializeField]
    private Color baseColor = Color.white;

    [SerializeField, Range(0f, 1f)] 
    private float cutoff = 0.5f;
    [SerializeField, Range(0f, 1f)] 
    private float metallic = 0f;
    [SerializeField, Range(0f, 1f)] 
    private float smoothness = 0f;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        // baseColor = new Color(Random.value, Random.value, Random.value);
        // cutoff = Random.value;
        // metallic = Random.value;
        // smoothness = Random.value;
        propertyBlock.SetFloat(metallicId, metallic);
        propertyBlock.SetFloat(smoothnessId, smoothness);
        propertyBlock.SetColor(baseColorId, baseColor);
        propertyBlock.SetFloat(cutoffId, cutoff);
        GetComponent<Renderer>().SetPropertyBlock(propertyBlock);
    }
}
