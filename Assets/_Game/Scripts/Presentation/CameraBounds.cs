using Unity.Cinemachine;
using UnityEngine;
using Worldtree.Domain.Camera;

namespace Worldtree.Presentation
{
    /// <summary>
    /// Cinemachine extension that keeps the orthographic view inside a world-space XY rectangle, so the camera
    /// never shows beyond the level. Plain rectangle instead of a collider: no physics query, no Physics2D.
    /// </summary>
    [ExecuteAlways]
    public sealed class CameraBounds : CinemachineExtension
    {
        [Tooltip("World-space XY rectangle the view must stay inside.")]
        public Rect Bounds = new Rect(-100, -100, 200, 200);

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body || !state.Lens.Orthographic) return;
            var pos = state.GetCorrectedPosition();
            var halfH = state.Lens.OrthographicSize;
            var halfW = halfH * state.Lens.Aspect;
            var clamped = new Vector3(
                ViewBounds.ClampAxis(pos.x, halfW, Bounds.xMin, Bounds.xMax),
                ViewBounds.ClampAxis(pos.y, halfH, Bounds.yMin, Bounds.yMax),
                pos.z);
            state.PositionCorrection += clamped - pos;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Bounds.center, new Vector3(Bounds.width, Bounds.height, 0));
        }
    }
}
