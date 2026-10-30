using UnityEngine;
using UnityEngine.SceneManagement;
using Worldtree.Domain;

namespace Worldtree.Presentation
{
    /// <summary>Entry point: applies global simulation settings, then loads the first scene.</summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] string firstScene = "Sandbox";

        void Awake()
        {
            Time.fixedDeltaTime = SimulationConfig.FixedStepSeconds;
            SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
        }
    }
}
