using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedBreak : MonoBehaviour
{
    public float durationOfReduction = 3f;

    private void OnTriggerEnter(Collider other)
    {
        OppentCar oppentCar = other.GetComponent<OppentCar>();
        if (oppentCar != null)
        {
            oppentCar.acceleration = Random.Range(0.5f, 1f);
            oppentCar.currentSpeed = Random.Range(25f, 28f);

            StartCoroutine(ResetAcceleration(oppentCar));
        }
    }
    IEnumerator ResetAcceleration (OppentCar oppentCar)
    {
        yield return new WaitForSeconds(durationOfReduction);
        oppentCar.ResetAcceleration();

    }
}
