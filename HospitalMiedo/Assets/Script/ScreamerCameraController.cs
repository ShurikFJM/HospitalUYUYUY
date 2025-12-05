using UnityEngine;

public class ScreamerCameraController : MonoBehaviour
{
    public Animator animator; // optional, assign animator with stab loop
    public AudioSource audioSource; // should be set to 3D or 2D as needed
    public float autoDestroyAfter = 0f; // 0 = don't auto destroy (GameOver will handle)

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>();

        if (animator != null)
        {
            // ensure the stab animation is looping in the Animator Controller
            // or use animator.Play("StabLoop");
        }

        if (audioSource != null)
        {
            audioSource.Play();
        }

        if (autoDestroyAfter > 0f)
            Destroy(gameObject, autoDestroyAfter);
    }
}
