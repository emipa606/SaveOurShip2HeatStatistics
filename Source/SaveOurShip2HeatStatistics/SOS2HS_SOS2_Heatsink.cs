using System.Reflection;
using RimWorld;
using Verse;

namespace SOS2HS;
/* First step: see the patch which adds data to the xml comp data
 *
 * Second step of this mod: adding this
 * Displaying this information
*/

[StaticConstructorOnStartup]
public static class SOS2HS_SOS2_Heatsink
{
    public static float GetMaxHeatPushed()
    {
        // modified from source : https://stackoverflow.com/questions/2665648/how-do-i-get-class-of-an-internal-static-class-in-another-assembly
        var ass = Assembly.GetAssembly(typeof(ShipCombatLaserMote));
        var type = ass.GetType("RimWorld.ShipCombatManager");
        var prop = type.GetField("HeatPushMult");
        return (float)prop.GetValue(type);
    }

    public static float GetMaxHeatOutput(StatRequest req, bool applyPostProcess = true)
    {
        if (req.Thing is null)
        {
            return 0f;
        }

        // modified from source : https://stackoverflow.com/questions/2665648/how-do-i-get-class-of-an-internal-static-class-in-another-assembly
        var ass = Assembly.GetAssembly(typeof(ShipCombatLaserMote));
        var type = ass.GetType("RimWorld.ShipCombatManager");
        var prop = type.GetField("HeatPushMult");
        var heatPushed = (float)prop.GetValue(type) / GetHeatVentTick(req, applyPostProcess);
        var surface = GetRoomSurface(req.Thing);
        return heatPushed / surface;
    }

    public static float GetMaxHeatOutputPerSecond(StatRequest req, bool applyPostProcess = true)
    {
        return GetMaxHeatOutput(req, applyPostProcess) * 60;
    }


    public static float GetRoomSurface(Thing thing)
    {
        return thing.GetRoom().CellCount;
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