using System;
using Verse;

namespace IdeologyExpandedEnclaves
{
    public enum EnclaveNeedType
    {
        Food,
        Medicine,
        BuildingMaterials,
        Textiles,
        Components
    }

    public enum EnclaveNeedSeverity
    {
        None,
        Low,
        Moderate,
        Severe,
        Critical
    }

    public enum EnclaveShortageLevel
    {
        None,
        Minor,
        Serious,
        Emergency
    }

    public sealed class EnclaveNeedRecord : IExposable
    {
        private EnclaveNeedType type;
        private EnclaveNeedSeverity severity;
        private int targetAmount;
        private int estimatedSupply;
        private int lastEvaluationTick = -1;

        public EnclaveNeedType Type => type;
        public EnclaveNeedSeverity Severity => severity;
        public int TargetAmount => targetAmount;
        public int EstimatedSupply => estimatedSupply;
        public int LastEvaluationTick => lastEvaluationTick;
        public bool IsShortage =>
            severity >= EnclaveNeedSeverity.Moderate;
        public EnclaveShortageLevel ShortageLevel =>
            EnclaveNeedsUtility.GetShortageLevel(severity);

        public EnclaveNeedRecord()
        {
        }

        public EnclaveNeedRecord(
            EnclaveNeedType type,
            int targetAmount,
            int estimatedSupply,
            EnclaveNeedSeverity severity,
            int lastEvaluationTick
        )
        {
            this.type = type;
            this.targetAmount = Math.Max(1, targetAmount);
            this.estimatedSupply = Math.Max(0, estimatedSupply);
            this.severity = severity;
            this.lastEvaluationTick = lastEvaluationTick;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type", EnclaveNeedType.Food);
            Scribe_Values.Look(
                ref severity,
                "severity",
                EnclaveNeedSeverity.None
            );
            Scribe_Values.Look(ref targetAmount, "targetAmount", 1);
            Scribe_Values.Look(
                ref estimatedSupply,
                "estimatedSupply",
                1
            );
            Scribe_Values.Look(
                ref lastEvaluationTick,
                "lastEvaluationTick",
                -1
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!Enum.IsDefined(typeof(EnclaveNeedType), type))
                {
                    type = EnclaveNeedType.Food;
                }

                if (
                    !Enum.IsDefined(
                        typeof(EnclaveNeedSeverity),
                        severity
                    )
                )
                {
                    severity = EnclaveNeedSeverity.None;
                }

                targetAmount = Math.Max(1, targetAmount);
                estimatedSupply = Math.Max(0, estimatedSupply);
            }
        }

        internal void ApplyEvaluation(
            int newTargetAmount,
            int newEstimatedSupply,
            EnclaveNeedSeverity newSeverity,
            int evaluationTick
        )
        {
            targetAmount = Math.Max(1, newTargetAmount);
            estimatedSupply = Math.Max(0, newEstimatedSupply);
            severity = newSeverity;
            lastEvaluationTick = evaluationTick;
        }
    }
}
