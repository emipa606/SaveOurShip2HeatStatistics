using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SOS2HS;

public class StatWorker_SOS2_Radiator_MaxACPerSecond : StatWorker
{
    private bool IsConcernedThing(Thing thing)
    {
        return thing.TryGetComp<CompTempControl>() != null && !(thing is MinifiedThing);
    }

    public override bool IsDisabledFor(Thing thing)
    {
        if (!base.IsDisabledFor(thing))
        {
            return !IsConcernedThing(thing);
        }

        return true;
    }

    public override bool ShouldShowFor(StatRequest req)
    {
        if (!base.ShouldShowFor(req))
        {
            return false;
        }

        if (!req.HasThing)
        {
            return false;
        }

        return IsConcernedThing(req.Thing);
    }

    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        if (req.Thing != null)
        {
            return SOS2HS_SOS2_Radiator.GetMaxACPerSecond(req, applyPostProcess);
        }

        Log.Error(
            $"Getting {GetType().FullName} for {req.Def.defName} without concrete thing. This always returns 1. This is a bug. Contact the dev.");
        return 1;
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {
        var tempControl = req.Thing.TryGetComp<CompTempControl>();
        var tempController = req.Thing;

        var intVec3_1 = tempController.Position + IntVec3.North.RotatedBy(tempController.Rotation);
        var intVec3_2 = tempController.Position + IntVec3.South.RotatedBy(tempController.Rotation);

        var cooledRoomTemp = intVec3_1.GetTemperature(tempController.Map);
        var extRoomTemp = intVec3_2.GetTemperature(tempController.Map);
        var efficiencyLossPerDegree =
            1.0f / 130.0f; // SOS2 internal value, means loss of efficiency for each degree above targettemp, lose 50% at 65C above targetTemp, 100% at 130+
        var energyPerSecond = tempControl.Props.energyPerSecond; // the power of the radiator
        var roomSurface = SOS2HS_SOS2_Radiator.GetRoomSurface(req.Thing);
        var coolingConversionRate = 4.16666651f; // Celsius cooled per JoulesSecond*Meter^2  conversion rate
        var sidesTempGradient = cooledRoomTemp - extRoomTemp;
        var efficiency = 1f - (sidesTempGradient * efficiencyLossPerDegree);
        var maxACPerSecond =
            energyPerSecond * efficiency / roomSurface * coolingConversionRate; // max cooling power possible


        var seb = new SEB("StatsReport_SOS2HS");
        seb.Simple("CooledRoomTemp", cooledRoomTemp);
        seb.Simple("ExteriorRoomTemp", extRoomTemp);
        seb.Simple("EfficiencyLossPerDegree", efficiencyLossPerDegree);
        seb.Simple("EnergyPerSecond", energyPerSecond);
        seb.Simple("CooledRoomSurface", roomSurface);
        seb.Simple("ACConversionRate", coolingConversionRate);
        seb.Full("SidesTempGradient", sidesTempGradient, cooledRoomTemp, extRoomTemp);
        seb.Full("RelativeEfficiency", efficiency * 100, sidesTempGradient, efficiencyLossPerDegree);
        seb.Full("MaxACPerSecond", maxACPerSecond, energyPerSecond, efficiency, roomSurface, coolingConversionRate);

        return seb.ToString();
    }

    public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense,
        StatRequest optionalReq, bool finalized = true)
    {
        return new SEB("StatsReport_SOS2HS", "TemperaturePerSecond").ValueNoFormat(GetValueUnfinalized(optionalReq))
            .ToString();
    }

    public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
    {
        yield return new Dialog_InfoCard.Hyperlink();
    }
}