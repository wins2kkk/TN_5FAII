using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BootPad : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        OppentCar oppentCar = other.GetComponent<OppentCar>();
        if (oppentCar != null)
        {
            oppentCar.acceleration = Random.Range(4f, 5f);
            oppentCar.maxSpeed = Random.Range(35f, 42f);

            
        }
    }
}
