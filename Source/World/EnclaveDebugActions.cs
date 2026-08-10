using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public static class EnclaveDebugActions
    {
        [DebugAction(
            "Ideology Expanded: Enclaves",
            "Quick test enclave (12 pilgrims)",
            allowedGameStates = AllowedGameStates.PlayingOnWorld
        )]
        public static void QuickTestEnclave()
        {
            EnclaveDevTools.CreateQuickTestEnclave();
        }

        [DebugAction(
            "Ideology Expanded: Enclaves",
            "Give 2,000 test silver",
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void GiveTestSilver()
        {
            EnclaveDevTools.GiveTestSilver(
                Find.CurrentMap?.Parent as PilgrimCamp
            );
        }

        [DebugAction(
            "Ideology Expanded: Enclaves",
            "Show current enclave test state",
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ShowCurrentEnclaveTestState()
        {
            EnclaveDevTools.ShowTestState(
                Find.CurrentMap?.Parent as PilgrimCamp
            );
        }
    }
}
