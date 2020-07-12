using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace SOS2HS
{
    public class StatWorker_MaxHeatOutputPerSecond : StatWorker
    {
        public override bool IsDisabledFor(Thing thing)
        {
            if (!base.IsDisabledFor(thing))
            {
                return !thing.def.comps
                    .Where(cp => cp is CompProperties_ShipHeat)
                    .Select(cp => cp as CompProperties_ShipHeat)
                    .All(cp => !cp.ventHeatToSpace);
            }
            return true;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (req.Thing == null)
            {
                Log.Error(string.Concat("Getting HeatVented stat for ", req.Def, " without concrete thing. This always returns 1."));
                return 1;
            }
            return HeatStatistics.GetMaxHeatOutputPerSecond(req, applyPostProcess);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();

            var heatPushed = HeatStatistics.GetMaxHeatPushed();
            stringBuilder.AppendLine("StatsReport_SOS2HS_MaxHeatPushed".Translate());
            stringBuilder.AppendLine("  " + heatPushed + " C.m^-2");

            var heatPushTick = HeatStatistics.GetHeatVentTick(req);
            stringBuilder.AppendLine("StatsReport_SOS2HS_HeatPushTickInterval".Translate());
            stringBuilder.AppendLine("  " + heatPushTick + " ");

            RoomGroup roomGroup = req.Thing.Position.GetRoomGroup(req.Thing.Map);
            float surface = 0f;
            if (roomGroup != null && !roomGroup.UsesOutdoorTemperature)
            {
                surface = roomGroup.CellCount;
            }
            stringBuilder.AppendLine("StatsReport_SOS2HS_RoomSurface".Translate());
            if (surface == 0)
            {
                stringBuilder.AppendLine("  " + surface + " m^2 (outdoors)");
            }
            else
            {
                stringBuilder.AppendLine("  " + surface + " m^2");
            }

            float heatPushedPerSecond = heatPushed / heatPushTick * 60;
            stringBuilder.AppendLine("StatsReport_SOS2HS_HeatPushedPerSecond".Translate());
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_HeatPushedPerSecondEquation".Translate() + ")");
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_HeatPushedPerSecondEquationNumbers".Translate(heatPushed, heatPushTick) + ")");
            stringBuilder.AppendLine("  " + heatPushedPerSecond + " C.s^-1.m^-2");

            float heatOutputPerSecond = 0f;
            if (surface != 0)
            {
                heatOutputPerSecond = heatPushedPerSecond / surface;
            }
            stringBuilder.AppendLine("StatsReport_SOS2HS_HeatOutputPerSecond".Translate());
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_HeatOutputPerSecondEquation".Translate() + ")");
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_HeatOutputPerSecondEquationNumbers".Translate(heatPushedPerSecond, surface) + ")");
            stringBuilder.AppendLine("  " + heatOutputPerSecond + " C.s^-1");

            return stringBuilder.ToString();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return string.Format("{0} C.s^-1", HeatStatistics.GetMaxHeatOutputPerSecond(optionalReq).ToString("0.###"));
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            if (base.ShouldShowFor(req))
            {
                if (!req.HasThing)
                {
                    return false;
                }
                return req.Thing.def.comps
                    .Where(cp => cp is CompProperties_ShipHeat)
                    .Select(cp => cp as CompProperties_ShipHeat)
                    .All(cp => !cp.ventHeatToSpace);
            }
            return false;
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
        {
            yield return new Dialog_InfoCard.Hyperlink();
        }
    }
}