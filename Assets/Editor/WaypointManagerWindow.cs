using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public class WaypointManagerWindow : EditorWindow
{
    public Transform waypointOrigin;
    [MenuItem("Waypoint/Waypoints Editor Tools")]

    public static void ShowWindow()
    {
        GetWindow<WaypointManagerWindow>("Waypoints Editor Tools");
    }



    void OnGUI()
    {
        SerializedObject obj = new SerializedObject(this);

        EditorGUILayout.PropertyField(obj.FindProperty("waypointOrigin"));

        if (waypointOrigin == null)
        {
            EditorGUILayout.HelpBox("Please assign a Waypoint origin transform. ", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            CreateButtons();
            EditorGUILayout.EndVertical();
        }

        obj.ApplyModifiedProperties();
        void CreateButtons()
        {
            if (GUILayout.Button("Create Waypoint"))
            {
                CreateButton();
            }
        }
    }
    void CreateButton()
    {
        GameObject waypointObject = new GameObject("Waypoint" + waypointOrigin.childCount, typeof(Waypointt));

        waypointObject.transform.SetParent(waypointOrigin, false);

        Waypointt waypoint = waypointObject.GetComponent<Waypointt>();

        if(waypointOrigin.childCount > 1)
        {
            waypoint.previousWaypoint = waypointOrigin.GetChild(waypointOrigin.childCount - 2).GetComponent<Waypointt>();
            waypoint.previousWaypoint.nextWaypoint = waypoint;

            waypoint.transform.position = waypoint.previousWaypoint.transform.position;
            waypoint.transform.forward = waypoint.previousWaypoint.transform.forward;
        }
        Selection.activeGameObject = waypoint.gameObject;
    }
}
