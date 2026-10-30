using Unity.Cinemachine;
using UnityEngine;

namespace Worldtree.Presentation
{
    /// <summary>Tells the camera the follow target teleported on respawn, so it jumps with it instead of gliding across the level.</summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class CameraWarpOnRespawn : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;

        CinemachineCamera vcam;

        public PlayerMotor Motor { set => motor = value; }

        void OnEnable()
        {
            vcam = GetComponent<CinemachineCamera>();
            if (motor != null) motor.Respawned += OnRespawned;
        }

        void OnDisable()
        {
            if (motor != null) motor.Respawned -= OnRespawned;
        }

        void OnRespawned(Vector3 delta) => vcam.OnTargetObjectWarped(motor.transform, delta);
    }
}
