using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock propertyBlock;

    [SerializeField]
    private Color baseColor = Color.white;

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
        propertyBlock.SetColor(baseColorId, baseColor);
        GetComponent<Renderer>().SetPropertyBlock(propertyBlock);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
