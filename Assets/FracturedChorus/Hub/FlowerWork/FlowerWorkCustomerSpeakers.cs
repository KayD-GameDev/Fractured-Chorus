using FracturedChorus.Narrative.Vn;

namespace FracturedChorus.Hub.FlowerWork
{
    public static class FlowerWorkCustomerSpeakers
    {
        public static string ResolveSpeakerId(FlowerWorkScenarioSO scenario)
        {
            if (scenario != null && !string.IsNullOrWhiteSpace(scenario.customerSpeakerId))
            {
                return scenario.customerSpeakerId.Trim();
            }

            return VnSpeakerIds.FlowerCustomerGentleman;
        }
    }
}
