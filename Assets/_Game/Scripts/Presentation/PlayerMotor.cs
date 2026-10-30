using UnityEngine;
using UnityEngine.InputSystem;
using Worldtree.Domain;
using Worldtree.Domain.Movement;

namespace Worldtree.Presentation
{
    /// <summary>
    /// Drives a dynamic Rigidbody on the XY plane from <see cref="MovementModel"/> at the fixed step.
    /// Assumes unit scale and no rotation on the player (capsule collider, Z and rotation frozen).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] MovementConfigAsset configAsset;
        [SerializeField, Tooltip("Optional. Tests and other callers may use SetMove/QueueJump instead.")] InputActionAsset inputActions;
        [SerializeField] float planeZ;
        [SerializeField, Tooltip("Falling below this world Y returns the player to the spawn point.")] float fallResetY = -10f;
        [SerializeField] float groundCheckDistance = 0.05f;
        [SerializeField, Range(0f, 89f)] float maxGroundSlope = 50f;
        [SerializeField] LayerMask groundLayers = ~0;

        const float OneWayTopTolerance = 0.1f; // rising feet snap up by at most this when they reach the top
        const float OneWayReach = 1f;          // > the farthest the body travels in one step (25 m/s * 1/60 = 0.42)

        readonly Collider[] nearby = new Collider[16];
        readonly MovementModel model = new MovementModel();
        Rigidbody body;
        CapsuleCollider capsule;
        InputAction move;
        InputAction jump;
        float moveInput;
        bool jumpQueued;
        Vector3 spawn;

        public MovementModel Model => model;
        public int FallResets { get; private set; }

        /// <summary>Raised after a respawn with the teleport offset; the camera uses it to avoid gliding.</summary>
        public event System.Action<Vector3> Respawned;

        // Set before the component is enabled (e.g. on an inactive GameObject), same as the serialized fields.
        public MovementConfigAsset ConfigAsset { get => configAsset; set => configAsset = value; }
        public InputActionAsset InputActions { get => inputActions; set => inputActions = value; }
        public float FallResetY { get => fallResetY; set => fallResetY = value; }

        public void SetMove(float horizontal) => moveInput = horizontal;
        public void QueueJump() => jumpQueued = true;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            if (configAsset == null)
            {
                Debug.LogError($"{name}: PlayerMotor needs a MovementConfigAsset.", this);
                enabled = false;
                return;
            }
            var error = configAsset.Config.Validate();
            if (error != null)
            {
                Debug.LogError($"{name}: invalid movement config: {error}", this);
                enabled = false;
                return;
            }

            // Snap onto the plane before freezing Z: Unity ignores position writes along a frozen axis.
            var p = body.position;
            spawn = new Vector3(p.x, p.y, planeZ);
            transform.position = spawn;
            Physics.SyncTransforms();

            body.useGravity = false; // gravity comes from the model
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Zero friction so pressing into a wall never holds the player up; the model owns all horizontal speed.
            capsule.sharedMaterial = new PhysicsMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0, staticFriction = 0,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0, bounceCombine = PhysicsMaterialCombine.Minimum,
            };

            if (inputActions != null)
            {
                var map = inputActions.FindActionMap("Gameplay", true);
                move = map.FindAction("Move", true);
                jump = map.FindAction("Jump", true);
            }
        }

        void OnEnable() => inputActions?.FindActionMap("Gameplay")?.Enable();
        void OnDisable() => inputActions?.FindActionMap("Gameplay")?.Disable();

        void Update()
        {
            if (move == null) return;
            moveInput = move.ReadValue<float>();
            if (jump.WasPressedThisFrame()) jumpQueued = true; // latched: Update can run several times between fixed steps
        }

        void FixedUpdate()
        {
            UpdateOneWayPlatforms();
            var v = body.linearVelocity;
            model.VelocityX = v.x;
            model.VelocityY = v.y;
            model.Step(new MovementInput(moveInput, jumpQueued), IsGrounded(), SimulationConfig.FixedStepSeconds, configAsset.Config);
            jumpQueued = false;
            body.linearVelocity = new Vector3(model.VelocityX, model.VelocityY, 0f);

            if (body.position.y < fallResetY) Respawn();
        }

        public void Respawn()
        {
            var delta = spawn - body.position;
            body.position = spawn;
            body.linearVelocity = Vector3.zero;
            model.Reset();
            FallResets++;
            Respawned?.Invoke(delta);
        }

        /// <summary>
        /// A one-way platform is solid only while the feet are at or above its top. Casts ignore Physics.IgnoreCollision,
        /// so this runs before every step, while platforms are within a step's travel of the player.
        /// </summary>
        void UpdateOneWayPlatforms()
        {
            var feetY = body.position.y + capsule.center.y - capsule.height * 0.5f;
            var fallPerStep = Mathf.Max(0f, -body.linearVelocity.y) * Time.fixedDeltaTime;
            var half = new Vector3(capsule.radius + OneWayReach, capsule.height * 0.5f + OneWayReach, 1f);
            var count = Physics.OverlapBoxNonAlloc(body.position + capsule.center, half, nearby, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                if (!nearby[i].TryGetComponent<OneWayPlatform>(out _)) continue;
                // Resting feet sit within the contact offset of the top; a fast faller sinks up to one step's travel into it
                // before contact resolves. Both must still count as "above", or the landing is mistaken for a pass from below.
                var ignore = feetY < nearby[i].bounds.max.y - OneWayTopTolerance - fallPerStep;
                if (Physics.GetIgnoreCollision(capsule, nearby[i]) != ignore) Physics.IgnoreCollision(capsule, nearby[i], ignore);
            }
        }

        bool IsGrounded()
        {
            var radius = capsule.radius;
            var feet = body.position + capsule.center + Vector3.down * (capsule.height * 0.5f - radius);
            // Starts slightly inside the player's own collider, which casts ignore; ground is tested by the sphere's lowest reach.
            return Physics.SphereCast(feet + Vector3.up * 0.02f, radius * 0.95f, Vector3.down, out var hit,
                       groundCheckDistance + 0.02f, groundLayers, QueryTriggerInteraction.Ignore)
                   && !Physics.GetIgnoreCollision(capsule, hit.collider)
                   && hit.normal.y >= Mathf.Cos(maxGroundSlope * Mathf.Deg2Rad);
        }
    }
}
