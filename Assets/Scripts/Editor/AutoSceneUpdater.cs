#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Redline.UI;

namespace Redline.EditorTools
{
    [InitializeOnLoad]
    public static class AutoSceneUpdater
    {
        static AutoSceneUpdater()
        {
            EditorApplication.delayCall += UpdateUILayoutInScene;
            EditorApplication.delayCall += UpdateAppIcon;
        }

        private static void UpdateAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/App_icon.png");
            if (icon != null)
            {
                var unknownTarget = UnityEditor.Build.NamedBuildTarget.Unknown;
                var standaloneTarget = UnityEditor.Build.NamedBuildTarget.Standalone;
                var webglTarget = UnityEditor.Build.NamedBuildTarget.WebGL;
                var androidTarget = UnityEditor.Build.NamedBuildTarget.Android;
                var iosTarget = UnityEditor.Build.NamedBuildTarget.iOS;

                var icons = new Texture2D[] { icon };
                PlayerSettings.SetIcons(unknownTarget, icons, IconKind.Application);
                PlayerSettings.SetIcons(standaloneTarget, icons, IconKind.Application);
                PlayerSettings.SetIcons(webglTarget, icons, IconKind.Application);
                PlayerSettings.SetIcons(androidTarget, icons, IconKind.Application);
                PlayerSettings.SetIcons(iosTarget, icons, IconKind.Application);

                Debug.Log("<color=#00FF66><b>[Redline] App_icon.png assigned as Game Application Icon across all platforms!</b></color>");
            }
        }

        private static void UpdateUILayoutInScene()
        {
            var hud = Object.FindAnyObjectByType<RaceHUDController>();
            if (hud != null)
            {
                hud.ApplyCleanUILayout();
                EditorUtility.SetDirty(hud);
                if (!Application.isPlaying && hud.gameObject.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
                    EditorSceneManager.SaveScene(hud.gameObject.scene);
                    Debug.Log("<color=#00FF66><b>[Redline] Scene UI layout cleanly updated and saved!</b></color>");
                }
            }
        }
    }
}
#endif
