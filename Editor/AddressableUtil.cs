using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace AAGen
{
    public static class AddressableUtil
    {
        public static AddressableAssetGroupTemplate FindDefaultAddressableGroupTemplate()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
                throw new ("AddressableAssetSettings not found. Ensure Addressables are initialized.");
            
            var templates = settings.GroupTemplateObjects;

            if (templates == null || templates.Count == 0)
                throw new ("No group templates found in Addressable Settings.");
            
            // Assuming the first template is the default one
            var defaultTemplate = templates[0];
            return defaultTemplate as AddressableAssetGroupTemplate;
        }
    }
}