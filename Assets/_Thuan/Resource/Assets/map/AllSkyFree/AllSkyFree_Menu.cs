#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AllSkyFree_Menu
{
    [MenuItem("Window/AllSkyFree/Apply Skybox")]
    public static void ApplySkybox()
    {
        Debug.Log("Skybox Applied");
    }
}
#endif