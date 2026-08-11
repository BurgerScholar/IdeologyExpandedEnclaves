using System;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum EnclaveIdeologyType
    {
        Unassigned,
        Communal,
        Isolationist,
        Martial,
        Mercantile,
        Nature,
        Spiritual,
        Transhumanist
    }

    public class EnclaveIdeologyProfile : IExposable
    {
        public EnclaveIdeologyType Type;

        public bool IsValid =>
            Type != EnclaveIdeologyType.Unassigned &&
            Enum.IsDefined(typeof(EnclaveIdeologyType), Type);

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref Type,
                "type",
                EnclaveIdeologyType.Unassigned
            );

            if (
                Scribe.mode == LoadSaveMode.PostLoadInit &&
                !Enum.IsDefined(typeof(EnclaveIdeologyType), Type)
            )
            {
                Type = EnclaveIdeologyType.Unassigned;
            }
        }
    }
}
