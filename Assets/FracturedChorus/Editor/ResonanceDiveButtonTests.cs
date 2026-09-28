using FracturedChorus.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tests
{
    public class ResonanceDiveButtonTests
    {
        [Test]
        public void RuntimeCreate_DoesNotStackABlankWhitePlate()
        {
            var host = new GameObject("host", typeof(RectTransform));
            try
            {
                var button = ResonanceDiveButton.Ensure(host.transform, () => { }, fillParent: true);
                var plates = 0;
                Image plate = null;
                foreach (Transform child in button.transform)
                {
                    if (child.name != "Layer_Plate")
                    {
                        continue;
                    }

                    plates += 1;
                    plate = child.GetComponent<Image>();
                }

                Assert.AreEqual(1, plates);
                Assert.IsNotNull(plate);
                Assert.IsNotNull(plate.sprite);
                Assert.IsTrue(plate.enabled);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
