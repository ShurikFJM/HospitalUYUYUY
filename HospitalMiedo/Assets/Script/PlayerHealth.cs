using UnityEngine;
using System.Collections;

public class VRPlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public float damageInterval = 1f;
    private bool isTakingDamage = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void ApplyDamage(int amount)
    {
        if (!isTakingDamage)
            StartCoroutine(DamageOverTime(amount));
    }

    private IEnumerator DamageOverTime(int amount)
    {
        isTakingDamage = true;

        while (true)
        {
            currentHealth -= amount;
            Debug.Log("VR Player Health: " + currentHealth);

            if (currentHealth <= 0)
            {
                Debug.Log("VR PLAYER IS DEAD");
                // Aquí cargas escena, UI de muerte, etc.
                yield break;
            }

            yield return new WaitForSeconds(damageInterval);
        }
    }

    public void StopDamage()
    {
        isTakingDamage = false;
        StopAllCoroutines();
    }
}