using UnityEngine;

namespace Worldtree.Presentation
{
    /// <summary>Marks a solid collider the player can jump up through and stand on. See PlayerMotor.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class OneWayPlatform : MonoBehaviour { }
}
