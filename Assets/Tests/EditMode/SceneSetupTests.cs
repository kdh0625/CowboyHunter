using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CowboyHunter.Tests
{
    public class SceneSetupTests
    {
        [Test]
        public void EveryBuildScene_HasPixelPerfectCamera_480x270_PPU16()
        {
            Assert.IsNotEmpty(EditorBuildSettings.scenes);
            foreach (var s in EditorBuildSettings.scenes)
            {
                var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Additive);
                try
                {
                    PixelPerfectCamera pp = null;
                    foreach (var root in scene.GetRootGameObjects())
                        pp ??= root.GetComponentInChildren<PixelPerfectCamera>();

                    Assert.IsNotNull(pp, s.path);
                    Assert.AreEqual(480, pp.refResolutionX, s.path);
                    Assert.AreEqual(270, pp.refResolutionY, s.path);
                    Assert.AreEqual(16, pp.assetsPPU, s.path);
                }
                finally
                {
                    if (EditorSceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
