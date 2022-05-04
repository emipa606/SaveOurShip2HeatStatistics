using RimWorld;
using UnityEngine;
using Verse;

namespace SOS2HS;
/* First step: see the patch which adds data to the xml comp data
 *
 * Second step of this mod: adding this
 * Displaying this information
*/

[StaticConstructorOnStartup]
public static class SOS2HS_SOS2_Radiator
{
    public static float GetCurrentEfficiency(StatRequest req, bool applyPostProcess = true)
    {
        var tempController = req.Thing;

        var intVec3_1 = tempController.Position + IntVec3.North.RotatedBy(tempController.Rotation);
        var intVec3_2 = tempController.Position + IntVec3.South.RotatedBy(tempController.Rotation);

        var cooledRoomTemp = intVec3_1.GetTemperature(tempController.Map);
        var extRoomTemp = intVec3_2.GetTemperature(tempController.Map);
        var efficiencyLossPerDegree =
            1.0f / 130.0f; // SOS2 internal value, means loss of efficiency for each degree above targettemp, lose 50% at 65C above targetTemp, 100% at 130+
        var sidesTempGradient = cooledRoomTemp - extRoomTemp;
        var efficiency = 1f - (sidesTempGradient * efficiencyLossPerDegree);
        return efficiency;
    }


    public static float GetCurrentACPerSecond(StatRequest req, bool applyPostProcess = true)
    {
        var tempControl = req.Thing.TryGetComp<CompTempControl>();
        var tempController = req.Thing;

        var intVec3_1 = tempController.Position + IntVec3.North.RotatedBy(tempController.Rotation);

        var cooledRoomTemp = intVec3_1.GetTemperature(tempController.Map);
        var targetTemp = tempControl.targetTemperature;
        var targetTempDiff = targetTemp - cooledRoomTemp;
        var maxACPerSecond = GetMaxACPerSecond(req); // max cooling power possible
        var isHeater = tempControl.Props.energyPerSecond > 0;
        if (isHeater)
        {
            return Mathf.Max(Mathf.Min(targetTempDiff, maxACPerSecond), 0);
        }

        return Mathf.Min(Mathf.Max(targetTempDiff, maxACPerSecond), 0);
    }

    public static float GetMaxACPerSecond(StatRequest req, bool applyPostProcess = true)
    {
        var tempControl = req.Thing.TryGetComp<CompTempControl>();
        var tempController = req.Thing;

        var unused = tempController.Position + IntVec3.North.RotatedBy(tempController.Rotation);

        var energyPerSecond = tempControl.Props.energyPerSecond; // the power of the radiator
        var roomSurface = GetRoomSurface(req.Thing); // the power of the radiator
        var coolingConversionRate = 4.16666651f; // Celsius cooled per JoulesSecond*Meter^2  conversion rate
        var efficiency = GetCurrentEfficiency(req);
        var maxACPerSecond =
            energyPerSecond * efficiency / roomSurface * coolingConversionRate; // max cooling power possible
        return maxACPerSecond;
    }

    public static float GetRoomSurface(Thing thing)
    {
        var intVec3_1 = thing.Position + IntVec3.North.RotatedBy(thing.Rotation);

        return intVec3_1.GetRoom(thing.Map).CellCount;
    }

    public static float GetHeatVentTick(ThingDef def)
    {
        var shipHeat = def.GetCompProperties<CompProperties_ShipHeat>();
        if (shipHeat == null)
        {
            return 1;
        }

        return shipHeat.heatVentTick;
    }

    public static float GetHeatVentTick(StatRequest req, bool applyPostProcess = true)
    {
        return GetHeatVentTick(req.Thing.def);
    }
}