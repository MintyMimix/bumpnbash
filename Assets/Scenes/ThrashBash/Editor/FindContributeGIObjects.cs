#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class FindContributeGIObjects
{
    [MenuItem("Tools/Select Contribute GI Objects")]
    public static void SelectContributeGI()
    {
        int CONVERT_GI_BIT = 1 << 2; // 0x04
        List<GameObject> giObjects = new List<GameObject>();

        foreach (var renderer in Object.FindObjectsOfType<MeshRenderer>(true))
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
            if ((flags & StaticEditorFlags.ContributeGI) != 0)
                giObjects.Add(renderer.gameObject);
        }

        Selection.objects = giObjects.ToArray();
        Debug.Log($"Selected {giObjects.Count} objects with Contribute GI enabled.");
    }
}
#endif
