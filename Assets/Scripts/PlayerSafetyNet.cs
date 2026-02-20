using UnityEngine;

/// <summary>
/// Attach to the XR Origin / Player object.
/// Teleports the player back to a spawn point and zeroes out velocity
/// whenever they fall below the Y threshold.
/// </summary>
public class PlayerSafetyNet : MonoBehaviour
{
    [Tooltip("Y position that counts as 'fallen off the map'.")]
    [SerializeField] private float fallThreshold = -5.0f;

    [Tooltip("Position the player resets to after falling.")]
    [SerializeField] private Vector3 respawnPoint = new Vector3(0f, 1f, 0f);

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        transform.position = respawnPoint;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
