using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace AAGen.Runtime
{
    public class BootLoader : MonoBehaviour
    {
        public AssetReference m_SceneToLoad;
        
        void Start()
        {
            Addressables.LoadSceneAsync(m_SceneToLoad, LoadSceneMode.Single);
        }
    }
}
