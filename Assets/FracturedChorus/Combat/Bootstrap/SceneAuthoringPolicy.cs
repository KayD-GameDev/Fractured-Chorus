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
            "Không code trên scene — logic chỉ trong .cs. " +
            "Mọi GameObject UI/combat phải hiện trong Hierarchy; Play chỉ bind dữ liệu, không spawn ẩn / không dịch layout khi preserveSceneLayout bật. " +
            "Chỉnh Hierarchy → Save → Play phải khớp. Rebuild qua menu Fractured Chorus, không rebuild lúc Play.";
    }
}
