using UnityEngine;

namespace Worldtree.Presentation
{
    /// <summary>Trigger volume that returns the player to their spawn point on contact.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class FallResetZone : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<PlayerMotor>(out var motor))
                motor.Respawn();
        }
    }
}
