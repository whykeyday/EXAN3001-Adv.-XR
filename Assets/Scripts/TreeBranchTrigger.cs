using UnityEngine;

/// <summary>
/// Attach this to each branch of the Tree so that touching it triggers healing.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TreeBranchTrigger : MonoBehaviour
{
    public TreeHealer healer;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            if (healer != null)
            {
                healer.ReceiveTouch();
            }
        }
    }
}
