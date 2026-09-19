using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondNavRowView : MonoBehaviour
    {
        private static readonly Color SelectedLabelColor = new Color(0.08f, 0.1f, 0.22f, 1f);

        [SerializeField] private Image plate;
        [SerializeField] private Text label;
        [SerializeField] private Button button;
        [SerializeField] private Sprite plateNormal;
        [SerializeField] private Sprite plateSelected;

        public Button Button => button;

        private void Awake()
        {
            WireReferences();
            ConfigureButton();
        }

        public void Bind(bool selected, Sprite normalOverride = null, Sprite selectedOverride = null)
        {
            WireReferences();
            ConfigureButton();

            var normal = normalOverride ?? plateNormal ?? BondsPackSprites.MenuNormal;
            var selectedPlate = selectedOverride ?? plateSelected ?? BondsPackSprites.MenuSelected;

            if (plate != null && normal != null && selectedPlate != null)
            {
                plate.sprite = selected ? selectedPlate : normal;
                plate.color = Color.white;
            }

            if (label != null)
            {
                label.color = selected ? SelectedLabelColor : FcColorTokens.Brand.TextPrimary;
            }
        }

        private void WireReferences()
        {
            if (plate == null)
            {
                plate = GetComponent<Image>();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (label == null)
            {
                label = transform.Find("Label")?.GetComponent<Text>();
            }
        }

        private void ConfigureButton()
        {
            if (button == null)
            {
                return;
            }

            button.transition = Selectable.Transition.None;
            button.interactable = true;
        }
    }
}
