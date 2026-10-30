using UnityEngine;
using Worldtree.Domain.Movement;

namespace Worldtree.Presentation
{
    /// <summary>Designer-editable movement tuning; the domain only sees the plain struct.</summary>
    [CreateAssetMenu(menuName = "Worldtree/Movement Config", fileName = "PlayerMovement")]
    public sealed class MovementConfigAsset : ScriptableObject
    {
        public MovementConfig Config = MovementConfig.Default;

        void OnValidate()
        {
            var error = Config.Validate();
            if (error != null) Debug.LogError($"{name}: {error}", this);
        }
    }
}
