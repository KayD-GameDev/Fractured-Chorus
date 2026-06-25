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
            "Chỉnh object trong Hierarchy → Save scene → Play phải khớp. " +
            "Dùng menu Fractured Chorus để rebuild layout, không rebuild lúc Play.";
    }
}
