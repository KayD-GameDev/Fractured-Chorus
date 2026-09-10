#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace FracturedChorus.Editor
{
    [InitializeOnLoad]
    internal static class CharacterBuildHealOnce
    {
        private const string ClearFlagPath = "Temp/fc_clear_characterbuild_sandbox";
        private const string SeedFlagPath = "Temp/fc_seed_characterbuild_sandbox_header";

        static CharacterBuildHealOnce()
        {
            EditorApplication.delayCall += TryRun;
        }

        private static void TryRun()
        {
            if (File.Exists(ClearFlagPath))
            {
                File.Delete(ClearFlagPath);
                CharacterBuildSceneSetupEditor.ClearSandbox();
                return;
            }

            if (!File.Exists(SeedFlagPath))
            {
                return;
            }

            File.Delete(SeedFlagPath);
            CharacterBuildSceneSetupEditor.SeedSandboxHeader();
        }
    }
}
#endif
