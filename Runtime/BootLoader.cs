using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace AAGen.Runtime
{
    /// <summary>
    /// Represents a <see cref="Component"/> that can load a scene at the start of an empty bootstrapper scene.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        /// <summary>
        /// The scene to load.
        /// </summary>
        [SerializeField]
        private AssetReference m_SceneToLoad;
        
        /// <summary>
        /// Called by the Unity system the first time that this component is ready to update itself, according to the time slice. 
        /// </summary>
        private void Start()
        {
            // Closes all currently loaded scenes and loads a single scene asynchronously.
            Addressables.LoadSceneAsync(m_SceneToLoad, LoadSceneMode.Single);
        }
    }
}
