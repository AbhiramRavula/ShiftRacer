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
            EditorApplication.delayCall += ApplyAppIconAndLogos;
        }

        public static void ApplyAppIconAndLogos()
        {
            var iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/App_icon.png");
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/App_icon.png");

            if (iconTex != null)
            {
                var unknownTarget = UnityEditor.Build.NamedBuildTarget.Unknown;
                var standaloneTarget = UnityEditor.Build.NamedBuildTarget.Standalone;
                var webglTarget = UnityEditor.Build.NamedBuildTarget.WebGL;
                var androidTarget = UnityEditor.Build.NamedBuildTarget.Android;
                var iosTarget = UnityEditor.Build.NamedBuildTarget.iOS;

                var icons = new Texture2D[] { iconTex };
                PlayerSettings.SetIcons(unknownTarget, icons, IconKind.Application);
                PlayerSettings.SetIcons(standaloneTarget, icons, IconKind.Application);
                PlayerSettings.SetIcons(webglTarget, icons, IconKind.Application);
                PlayerSettings.SetIcons(androidTarget, icons, IconKind.Application);
                PlayerSettings.SetIcons(iosTarget, icons, IconKind.Application);

                if (iconSprite != null)
                {
                    try
                    {
                        var splashLogo = PlayerSettings.SplashScreenLogo.Create(2.5f, iconSprite);
                        PlayerSettings.SplashScreen.logos = new PlayerSettings.SplashScreenLogo[] { splashLogo };
                    }
                    catch { }
                }

                Debug.Log("<color=#00FF66><b>[Redline] App_icon.png locked as Game Application Icon & Logo across all build targets!</b></color>");
            }
        }

        private static void UpdateUILayoutInScene()
        {
            Redline.Core.RacerVisualSync.TuneTrackBloomAndLighting();

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

    /// <summary>
    /// Automatic pre-build processor: Ensures App_icon.png is strictly applied as the application
    /// icon and splash logo before every single build (PC, WebGL, mobile).
    /// </summary>
    public class BuildIconEnforcer : UnityEditor.Build.IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            AutoSceneUpdater.ApplyAppIconAndLogos();
        }
    }
}
#endif
