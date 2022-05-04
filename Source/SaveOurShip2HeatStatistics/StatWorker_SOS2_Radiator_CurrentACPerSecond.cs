using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SOS2HS;

public class StatWorker_SOS2_Radiator_CurrentACPerSecond : StatWorker
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
            return SOS2HS_SOS2_Radiator.GetCurrentACPerSecond(req, applyPostProcess);
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

        var cooledRoomTemp = intVec3_1.GetTemperature(tempController.Map);
        var targetTemp = tempControl.targetTemperature;
        var targetTempDiff = targetTemp - cooledRoomTemp;
        var maxACPerSecond = SOS2HS_SOS2_Radiator.GetMaxACPerSecond(req); // max cooling power possible
        var isHeater = tempControl.Props.energyPerSecond > 0;
        var actualAC = isHeater
            ? Mathf.Max(Mathf.Min(targetTempDiff, maxACPerSecond), 0)
            : Mathf.Min(Mathf.Max(targetTempDiff, maxACPerSecond), 0);

        var seb = new SEB("StatsReport_SOS2HS");
        seb.Simple("CooledRoomTemp", cooledRoomTemp);
        seb.Simple("TargetTemperature", targetTemp);
        seb.Full("TargetTempDiff", targetTempDiff, targetTemp, cooledRoomTemp);
        seb.Simple("MaxACPerSecond", maxACPerSecond);
        seb.Full(isHeater ? "ActualHeaterACPerSecond" : "ActualCoolerACPerSecond", actualAC, targetTempDiff,
            maxACPerSecond);

        return seb.ToString();
    }

    public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense,
        StatRequest optionalReq, bool finalized = true)
    {
        return new SEB("StatsReport_SOS2HS", "TemperaturePerSecond")
            .ValueNoFormat(GetValueUnfinalized(optionalReq).ToString("0.###")).ToString();
    }


    public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
    {
        yield return new Dialog_InfoCard.Hyperlink();
    }
}