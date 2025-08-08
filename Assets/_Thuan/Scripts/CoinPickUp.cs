using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinPickUp : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip energyPickupSound;
    public int coinAmount = 1;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CoinManager.Instance.AddCoins(coinAmount);

            // 🔊 Phát âm thanh
            if (audioSource != null && energyPickupSound != null && AudioManager.Instance != null)
            {
                audioSource.volume = AudioManager.Instance.effectsVolume;
                audioSource.PlayOneShot(energyPickupSound);
            }
            Destroy(gameObject);
        }

    }
}
