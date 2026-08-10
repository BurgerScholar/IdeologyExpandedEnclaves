using HarmonyLib;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public sealed class IdeologyExpandedEnclavesMod : Mod
    {
        public IdeologyExpandedEnclavesMod(ModContentPack content)
            : base(content)
        {
            new Harmony("BrandonArnold.IdeologyExpandedEnclaves")
                .PatchAll();

            Log.Message("Ideology Expanded: Enclaves loaded successfully.");
        }
    }
}
