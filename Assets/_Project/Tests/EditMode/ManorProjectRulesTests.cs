#if UNITY_INCLUDE_TESTS
using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Manor.Tests
{
    public class ManorProjectRulesTests
    {
        [Test]
        public void ProductionScenesAreConfigured()
        {
            Assert.That(EditorBuildSettings.scenes.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(EditorBuildSettings.scenes[0].path, Does.Contain("SCN_Boot_启动场景"));
            Assert.That(EditorBuildSettings.scenes[1].path, Does.Contain("SCN_MainMenu_主菜单"));
            Assert.That(EditorBuildSettings.scenes[2].path, Does.Contain("SCN_ManorDemo_庄园Demo"));
        }

        [Test]
        public void RequiredProductionScenesExist()
        {
            Assert.That(File.Exists("Assets/_Project/Scenes/Production/SCN_Boot_启动场景.unity"), Is.True);
            Assert.That(File.Exists("Assets/_Project/Scenes/Production/SCN_MainMenu_主菜单.unity"), Is.True);
            Assert.That(File.Exists("Assets/_Project/Scenes/Production/SCN_ManorDemo_庄园Demo.unity"), Is.True);
        }
    }
}
#endif
