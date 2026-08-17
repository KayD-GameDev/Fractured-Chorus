using UnityEngine;

namespace FracturedChorus.Data
{
    public enum TreasureRewardKind
    {
        HealPotion = 0,
        PlaceCounterPlus1 = 1,
        Notes = 2
    }

    [CreateAssetMenu(fileName = "TreasureReward", menuName = "Fractured Chorus/Treasure Reward")]
    public sealed class TreasureRewardSO : ScriptableObject
    {
        public const string CadenceFlaskId = "cadence_flask";
        public const string MetronomeCharmId = "metronome_charm";
        public const string VaultNotesId = "vault_notes";

        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private string description;
        [SerializeField] private TreasureRewardKind kind;
        [SerializeField] private Sprite icon;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public TreasureRewardKind Kind => kind;
        public Sprite Icon => icon;

        public string KindLabel => kind switch
        {
            TreasureRewardKind.HealPotion => "HP",
            TreasureRewardKind.PlaceCounterPlus1 => "COUNTER +1",
            TreasureRewardKind.Notes => "NOTES",
            _ => kind.ToString()
        };

        public static TreasureRewardSO CreateRuntime(
            string rewardId,
            string rewardTitle,
            string rewardDescription,
            TreasureRewardKind rewardKind)
        {
            var asset = CreateInstance<TreasureRewardSO>();
            asset.EditorAssign(rewardId, rewardTitle, rewardDescription, rewardKind);
            return asset;
        }

        public static TreasureRewardSO[] CreateDefaultCatalog()
        {
            return new[]
            {
                CreateRuntime(
                    CadenceFlaskId,
                    "Cadence Flask",
                    "Bình máu. Hồi HP toàn party sau combat.",
                    TreasureRewardKind.HealPotion),
                CreateRuntime(
                    MetronomeCharmId,
                    "Metronome Charm",
                    "Khi đặt lên board, skill đó +1 counter.",
                    TreasureRewardKind.PlaceCounterPlus1),
                CreateRuntime(
                    VaultNotesId,
                    "Vault Notes",
                    "Nhặt Notes từ rương.",
                    TreasureRewardKind.Notes)
            };
        }

        public void EditorAssign(
            string rewardId,
            string rewardTitle,
            string rewardDescription,
            TreasureRewardKind rewardKind,
            Sprite rewardIcon = null)
        {
            id = rewardId;
            title = rewardTitle;
            description = rewardDescription;
            kind = rewardKind;
            icon = rewardIcon;
        }
    }
}
