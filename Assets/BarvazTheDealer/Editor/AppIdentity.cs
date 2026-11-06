using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BarvazTheDealer.EditorTools
{
    /// <summary>App name and icon shared by the Mac and Android builds.</summary>
    public static class AppIdentity
    {
        public const string ProductName = "Caribi Barvaz";
        public const string IconPath = "Assets/BarvazTheDealer/Icon/app_icon.png";

        public static void Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = "Barvaz";
            PlayerSettings.bundleVersion = "1.1";

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogWarning("[Barvaz] App icon not found at " + IconPath);
                return;
            }
            // Default icon: used by every platform that has no icon of its own (Mac and Android here).
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }
    }
}
