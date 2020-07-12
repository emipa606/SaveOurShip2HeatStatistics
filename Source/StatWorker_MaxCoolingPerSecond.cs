using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace SOS2HS
{
    public class StatWorker_MaxCoolingPerSecond : StatWorker
    {
        public override bool IsDisabledFor(Thing thing)
        {
            if (!base.IsDisabledFor(thing))
            {
                return !thing.def.comps
                    .Where(cp => cp is CompProperties_ShipHeat)
                    .Select(cp => cp as CompProperties_ShipHeat)
                    .Any(cp => cp.ventHeatToSpace);
            }
            return true;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (req.Thing == null)
            {
                Log.Error(string.Concat("Getting MaxCoolingPerSecond stat for ", req.Def, " without concrete thing. This always returns 1."));
                return 1;
            }
            return HeatStatistics.GetMaxCoolingPerSecond(req, applyPostProcess);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            List<CompProperties_TempControl> TempControl = req.Thing.def.comps
                .Where(cp => cp is CompProperties_TempControl)
                .Select(cp => cp as CompProperties_TempControl)
                .ToList();
            if (TempControl.Count == 0)
            {
                Log.Error("No temperature control found, this is a bug, report to the dev");
                // something is wrong here, there is nothing
                return "No temperature control found, this is a bug, report to the dev";
            }
            if (TempControl.Count > 1)
            {
                Log.Error("Duplicate temperature control found, this is a bug, report to the dev");
                //there is a duplicate, that s wrong too
                return "Duplicate temperature control found, this is a bug, report to the dev";
            }

            if (!(req.Thing is Building_TempControl))
            {
                return req.Thing.Label + "is no Building_TempControl, this is a bug, report to the dev";
            }
            Building_TempControl radiator = (Building_TempControl)req.Thing;

            IntVec3 intVec3_1 = radiator.Position + IntVec3.North.RotatedBy(radiator.Rotation);
            IntVec3 intVec3_2 = radiator.Position + IntVec3.South.RotatedBy(radiator.Rotation);

            StringBuilder stringBuilder = new StringBuilder();

            float cooledRoomTemp = intVec3_1.GetTemperature(radiator.Map);
            stringBuilder.AppendLine("StatsReport_SOS2HS_CooledRoomTemp".Translate());
            stringBuilder.AppendLine("  " + cooledRoomTemp + " C");

            float extRoomTemp = intVec3_2.GetTemperature(radiator.Map);
            stringBuilder.AppendLine("StatsReport_SOS2HS_ExteriorRoomTemp".Translate());
            stringBuilder.AppendLine("  " + extRoomTemp + " C");

            float targetTemp = radiator.compTempControl.targetTemperature;
            stringBuilder.AppendLine("StatsReport_SOS2HS_TargetTemperature".Translate());
            stringBuilder.AppendLine("  " + targetTemp + " C");

            float efficiencyLossPerDegree = 1.0f / 130.0f; // SOS2 internal value, means loss of efficiency for each degree above targettemp, lose 50% at 65C above targetTemp, 100% at 130+
            stringBuilder.AppendLine("StatsReport_SOS2HS_EfficiencyLossPerDegree".Translate());
            stringBuilder.AppendLine("  " + efficiencyLossPerDegree + " C^-1");

            float energyPerSecond = TempControl[0].energyPerSecond; // the power of the radiator
            stringBuilder.AppendLine("StatsReport_SOS2HS_EnergyPerSecond".Translate());
            stringBuilder.AppendLine("  " + energyPerSecond + " J.s^-1");

            RoomGroup roomGroup = intVec3_1.GetRoomGroup(radiator.Map);
            float roomSurface = 0f; // the power of the radiator
            stringBuilder.AppendLine("StatsReport_SOS2HS_CooledRoomSurface".Translate());
            if (roomGroup != null && !roomGroup.UsesOutdoorTemperature)
            {
                roomSurface = roomGroup.CellCount;
                stringBuilder.AppendLine("  " + roomSurface + " m^2");
            }
            else
            {
                stringBuilder.AppendLine("  ? m^2 (outdoor)");
            }

            float coolingConversionRate = 4.16666651f; // Celsius cooled per Joules*Second*Meter^2  conversion rate
            stringBuilder.AppendLine("StatsReport_SOS2HS_CoolingConversionRate".Translate());
            stringBuilder.AppendLine("  " + coolingConversionRate + " C.J^-1.s^-1.m^-2");

            float sidesTempGradient = (cooledRoomTemp - extRoomTemp);
            stringBuilder.AppendLine("StatsReport_SOS2HS_SidesTempGradient".Translate());
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_SidesTempGradientEquation".Translate() + " )");
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_SidesTempGradientEquationNumbers".Translate(cooledRoomTemp, extRoomTemp) + " )");
            stringBuilder.AppendLine("  " + sidesTempGradient + " C");

            float efficiency = (1f - sidesTempGradient * efficiencyLossPerDegree);
            stringBuilder.AppendLine("StatsReport_SOS2HS_Efficiency".Translate());
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_EfficiencyEquation".Translate() + " )");
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_EfficiencyEquationNumbers".Translate(sidesTempGradient, efficiencyLossPerDegree) + " )");
            stringBuilder.AppendLine("  " + (efficiency * 100) + " %");

            float maxCoolingPerSecond = energyPerSecond * efficiency / roomSurface * coolingConversionRate * 60; // max cooling power possible
            stringBuilder.AppendLine("StatsReport_SOS2HS_MaxCoolingPerSecond".Translate());
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_MaxCoolingPerSecondEquation".Translate() + " * 60 )");
            stringBuilder.AppendLine("  (= " + "StatsReport_SOS2HS_MaxCoolingPerSecondEquationNumbers".Translate(energyPerSecond, efficiency, roomSurface, coolingConversionRate) + " * 60 )");
            stringBuilder.AppendLine("  " + maxCoolingPerSecond + " C.s^-1");

            return stringBuilder.ToString();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return string.Format("{0} C", HeatStatistics.GetMaxCoolingPerSecond(optionalReq).ToString("0.###"));
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
                    .Any(cp => cp.ventHeatToSpace);
            }
            return false;
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
        {
            yield return new Dialog_InfoCard.Hyperlink();
        }
    }
}