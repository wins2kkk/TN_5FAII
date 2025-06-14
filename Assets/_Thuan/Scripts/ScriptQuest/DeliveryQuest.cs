﻿using UnityEngine;
using System.Collections.Generic;

public class DeliveryQuest : MonoBehaviour
{
    [Header("Cài đặt nhiệm vụ")]
    public GameObject package;
    public string playerTag = "Player"; // Tag của xe player
    public float radius = 2f;
  //  public int reward = 100;

    [Header("Vị trí ngẫu nhiên")]
    public Transform[] pickupPoints;
    public Transform[] deliveryPoints;
    public float minDistance = 10f;

    [Header("Waypoint & Indicator")]
    public GameObject pickupIndicatorPrefab;
    public GameObject deliveryIndicatorPrefab;

    private Transform player; // Sẽ được tìm tự động
    private Transform currentPickup, currentDelivery;
    private GameObject pickupIndicator, deliveryIndicator, waypoint;
    private bool hasPickedUp, questActive;

    void Start()
    {
        if (package) package.SetActive(false);
        FindPlayer(); // Tìm player khi start
    }

    private void FindPlayer()
    {
        // Tìm player có tag "Player"
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log($"Found player: {player.name}");
        }
        else
        {
            Debug.LogError("No player found with tag: " + playerTag);
        }
    }

    void Update()
    {
        if (!questActive) return;

        // Đảm bảo có player reference
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float distance = Vector3.Distance(player.position, hasPickedUp ? currentDelivery.position : currentPickup.position);
        if (distance <= radius)
        {
            if (!hasPickedUp) Pickup();
            //else Deliver();
        }
    }

    public void StartQuest()
    {
        // Tìm lại player trước khi bắt đầu quest (đề phòng xe đã thay đổi)
        FindPlayer();

        if (player == null)
        {
            Debug.LogError("Cannot start quest: No player found!");
            return;
        }

        if (pickupPoints.Length == 0 || deliveryPoints.Length == 0) return;

        currentPickup = GetRandomPoint(pickupPoints);
        currentDelivery = ViTriGiaoHang(currentPickup);
        if (currentDelivery == null) return;

        package.transform.position = currentPickup.position;
        package.SetActive(true);
        ShowIndicator(currentPickup, ref pickupIndicator, pickupIndicatorPrefab);
        ShowWaypoint(currentPickup);
        hasPickedUp = false;
        questActive = true;
        Debug.Log("📦 Nhiệm vụ bắt đầu!");
    }

    void Pickup()
    {
        hasPickedUp = true;
        package.SetActive(false);
        HideIndicator(ref pickupIndicator);
        ShowIndicator(currentDelivery, ref deliveryIndicator, deliveryIndicatorPrefab);
        ShowWaypoint(currentDelivery);
        Debug.Log("📍 Đã nhận hàng!");
    }

    //void Deliver()
    //{
    //    questActive = false;
    //    HideIndicator(ref deliveryIndicator);
    //    HideWaypoint();
    //    QuestManager.instance?.CompleteQuest();
    //    Debug.Log($"✅ Giao hàng thành công! Nhận {reward} xu.");
    //}

    Transform GetRandomPoint(Transform[] points) => points[Random.Range(0, points.Length)];

    Transform ViTriGiaoHang(Transform from)
    {
        List<Transform> valid = new List<Transform>();
        foreach (var point in deliveryPoints)
            if (Vector3.Distance(from.position, point.position) >= minDistance)
                valid.Add(point);
        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : null;
    }

    void ShowIndicator(Transform target, ref GameObject indicator, GameObject prefab)
    {
        if (prefab && target)
        {
            indicator = Instantiate(prefab, target.position, Quaternion.identity, target);
        }
    }

    void HideIndicator(ref GameObject indicator)
    {
        if (indicator) Destroy(indicator);
    }

    void ShowWaypoint(Transform target)
    {
        if (WaypointManager.Instance)
        {
            waypoint = WaypointManager.Instance.CreatePointer(target.position);
        }
    }

    void HideWaypoint()
    {
        if (WaypointManager.Instance)
            WaypointManager.Instance.RemoveWaypoint();
    }
}