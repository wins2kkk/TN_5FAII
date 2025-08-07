using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class OppentCarWaypoint : MonoBehaviour
{
    [Header("Opponent Car")]
    public OppentCar opponentCar;
    public Waypointt currentWaypoint;

    void Start()
    {
        opponentCar.LocateDestination(currentWaypoint.GetPosition());
    }
    private void Update()
    {
        if (opponentCar.destinationReached)
        {
            currentWaypoint = currentWaypoint.nextWaypoint;
            opponentCar.LocateDestination(currentWaypoint.GetPosition());
        }
    }
}
