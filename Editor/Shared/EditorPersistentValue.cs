using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using UnityEditor;
using UnityEngine;

namespace AAGen
{
    /// <summary>
    /// Provides functionality for persisting values in the Unity editor using EditorPrefs.
    /// </summary>
    /// <typeparam name="T">A serializable type.</typeparam>
    public struct EditorPersistentValue<T>
    {
        private readonly string _persistenceKey;
        private readonly Action _onValueChanged;

        public EditorPersistentValue(T defaultValue, [NotNull] string persistenceKey, Action onValueChanged = null)
        {
            _persistenceKey = persistenceKey;
            _onValueChanged = onValueChanged;
        }

        public T Value
        {
            get => Load();
            set
            {
                if (object.Equals(Value, value))
                    return;
                
                Save(value);
                _onValueChanged?.Invoke();
            }
        }

        private void Save(T value)
        {
            try
            {
                EditorPrefs.SetString(_persistenceKey, JsonConvert.SerializeObject(value));
            }
            catch (Exception e)
            {
                Debug.LogError($"Save failed!: {e}");
            }
        }

        private T Load()
        {
            try
            {
                if (EditorPrefs.HasKey(_persistenceKey))
                    return JsonConvert.DeserializeObject<T>(EditorPrefs.GetString(_persistenceKey));
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed: {e}");
            }

            return default;
        }

        public void ClearPersistentData()
        {
            EditorPrefs.DeleteKey(_persistenceKey);
        }
    }
}
