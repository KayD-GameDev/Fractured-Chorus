using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class LoadingScreenNoSyncLoadTests
    {
        [Test]
        public void GameplayScripts_DoNotCallSyncLoadScene()
        {
            var root = Path.Combine(Application.dataPath, "FracturedChorus");
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Editor/"))
                {
                    continue;
                }

                if (normalized.EndsWith("LoadingScreenController.cs"))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.IsFalse(Regex.IsMatch(text, @"SceneManager\.LoadScene\s*\("), file);
            }
        }
    }
}
