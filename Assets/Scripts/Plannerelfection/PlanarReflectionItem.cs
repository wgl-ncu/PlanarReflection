using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanarReflectionItem : MonoBehaviour
{
    private void Start()
    {
        PlanarReflectionManager.Instance.Register(transform);
        var renderer = GetComponent<MeshRenderer>();
        foreach (var mat in renderer.materials)
        {
            var keyWorld = PlanarReflectionManager.Instance.useReflectionMatrix ? "_PLANAR_REFLECTION_WITH_REFLECTION_MATRIX" : "_PLANAR_REFLECTION";
            var disKeyWorld = !PlanarReflectionManager.Instance.useReflectionMatrix ? "_PLANAR_REFLECTION_WITH_REFLECTION_MATRIX" : "_PLANAR_REFLECTION";
            mat.EnableKeyword(keyWorld);
            mat.DisableKeyword(disKeyWorld);
        }
    }
}
