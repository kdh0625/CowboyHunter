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

        // 화면 스크립트의 인스펙터 연결이 비어 있으면 플레이 중 NullReferenceException이 난다.
        [Test]
        public void EveryBuildScene_ScreenScripts_HaveNoMissingReferences()
        {
            foreach (var s in EditorBuildSettings.scenes)
            {
                var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (mb == null || mb.GetType().Namespace != "CowboyHunter.UI") continue;
                        var it = new SerializedObject(mb).GetIterator();
                        it.NextVisible(true);
                        while (it.NextVisible(true))
                        {
                            if (it.propertyType == SerializedPropertyType.ObjectReference)
                                Assert.IsNotNull(it.objectReferenceValue, $"{s.path}: {mb.GetType().Name}.{it.propertyPath}");
                        }
                    }
                }
                finally
                {
                    if (EditorSceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
