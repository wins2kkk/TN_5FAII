using UnityEditor;
using UnityEngine;

public class ReplaceWithPrefab : EditorWindow
{
    GameObject prefabToReplaceWith;

    [MenuItem("Tools/Thay bằng Prefab")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceWithPrefab>("Thay bằng Prefab");
    }

    void OnGUI()
    {
        GUILayout.Label("Chọn Prefab để thay vào đối tượng đã chọn", EditorStyles.boldLabel);
        prefabToReplaceWith = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabToReplaceWith, typeof(GameObject), false);

        if (GUILayout.Button("Thay"))
        {
            ReplaceSelectedObjects();
        }
    }

    void ReplaceSelectedObjects()
    {
        if (prefabToReplaceWith == null)
        {
            Debug.LogError("Chưa chọn Prefab!");
            return;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToReplaceWith);
            newObj.transform.position = obj.transform.position;
            newObj.transform.rotation = obj.transform.rotation;
            newObj.transform.localScale = obj.transform.localScale;

            Undo.RegisterCreatedObjectUndo(newObj, "Thay thế bằng Prefab");
            Undo.DestroyObjectImmediate(obj);
        }
    }
}
