namespace FracturedChorus.Combat.Bootstrap
{
    /// <summary>
    /// Scene Hierarchy là nguồn sự thật cho layout/visual.
    /// Bootstrap chỉ gắn logic combat; không snap lại Transform, màu, active state đã chỉnh trong scene.
    /// Menu Editor (Rebuild Grid, Setup Scene) mới được phép tái tạo layout.
    /// </summary>
    public static class SceneAuthoringPolicy
    {
        public const string DocHint =
            "Do not put logic on scene objects — keep logic in .cs only. " +
            "All UI/combat GameObjects must live in the Hierarchy; Play mode only binds data, does not spawn hidden objects / does not shift layout when preserveSceneLayout is enabled. " +
            "Edit Hierarchy → Save → Play should match. Rebuild via Fractured Chorus menu, not during Play.";
    }
}
