using System;
using System.ComponentModel;
using System.IO;
using UnityEditor;

namespace AAGen.AssetDependencies
{
    /// <summary>
    /// Represents a graph node containing asset data.
    /// Implements IEquatable to ensure compatibility with dictionary processing methods in the Graph class.
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(AssetNodeTypeConverter))]
    public class AssetNode : IEquatable<AssetNode>
    {
        public AssetNode(GUID guid) //no default ctor since it has no meaning without guid
        {
            Guid = guid;
        }
        
        public readonly GUID Guid;
        
        /// <summary>
        /// Returns the path of an asset. This method uses the current state of the asset database to return a path for
        /// an entry in the dependency graph. It is only valid if the dependency graph is up to date.
        /// </summary>
        public string AssetPath => AssetDatabase.GUIDToAssetPath(Guid); 
        public string FileName => Path.GetFileName(AssetPath);

        public static AssetNode FromAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;
            
            var guidString = AssetDatabase.AssetPathToGUID(assetPath, AssetPathToGUIDOptions.OnlyExistingAssets); 
            return FromGuidString(guidString);
        }
        
        public static AssetNode FromGuidString(string guidString)
        {
            return GUID.TryParse(guidString, out var guid) ? new AssetNode(guid) : null;
        }
        
        public override string ToString()
        {
            bool isDebug = ProjectSettingsProvider.DebugMode;
            var debugInfo = AssetPath;
            return isDebug ? $"{Guid.ToString()}|{debugInfo}" : Guid.ToString(); 
        }

        public static AssetNode FromString(string value)
        {
            var parts = value.Split('|');
            var guidString = parts[0];
            return FromGuidString(guidString);
        }

        public bool Equals(AssetNode other)
        {
            return other != null && Guid.Equals(other.Guid);
        }

        public override bool Equals(object obj)
        {
            return obj is AssetNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }
    
    /// <summary>
    /// A custom JSON converter for AssetNode. Since Newtonsoft JSON does not support using custom types as dictionary keys,
    /// this converter provides a solution to that limitation.
    /// </summary>
    public class AssetNodeTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                return AssetNode.FromString(stringValue);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is AssetNode node)
            {
                return node.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
