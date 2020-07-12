using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace SOS2HS
{
    /* First step: see the patch which adds data to the xml comp data
     *
     * Second step of this mod: adding this
     * Displaying this information
    */

    [StaticConstructorOnStartup]
    public static class HeatStatistics
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
            if (!(req.Thing is Thing))
            {
                return 0f;
            }

            // modified from source : https://stackoverflow.com/questions/2665648/how-do-i-get-class-of-an-internal-static-class-in-another-assembly
            var ass = Assembly.GetAssembly(typeof(ShipCombatLaserMote));
            var type = ass.GetType("RimWorld.ShipCombatManager");
            var prop = type.GetField("HeatPushMult");
            var heatPushed = (float)prop.GetValue(type) / GetHeatVentTick(req, applyPostProcess);
            RoomGroup roomGroup = req.Thing.Position.GetRoomGroup(req.Thing.Map);
            if (roomGroup != null && !roomGroup.UsesOutdoorTemperature)
            {
                return heatPushed / roomGroup.CellCount;
            }
            else
            {
                return 0;
            }
        }

        public static float GetMaxHeatOutputPerSecond(StatRequest req, bool applyPostProcess = true)
        {
            return GetMaxHeatOutput(req, applyPostProcess) * 60;
        }

        public static float GetCurrentCoolingPerSecond(StatRequest req, bool applyPostProcess = true)
        {
            if (!(req.Thing is Building_TempControl))
            {
                return 0f;
            }

            Building_TempControl radiator = (Building_TempControl)req.Thing;

            IntVec3 intVec3_1 = radiator.Position + IntVec3.North.RotatedBy(radiator.Rotation);
            IntVec3 intVec3_2 = radiator.Position + IntVec3.South.RotatedBy(radiator.Rotation);

            float cooledRoomTemp = intVec3_1.GetTemperature(radiator.Map);

            float extRoomTemp = intVec3_2.GetTemperature(radiator.Map);

            float roomTempGradient = (cooledRoomTemp - extRoomTemp);

            float efficiencyLossPerDegree = 1.0f / 130.0f; // SOS2 internal value, means loss of efficiency for each degree above targettemp, lose 50% at 65C above targetTemp, 100% at 130+

            float efficiency = (1f - roomTempGradient * efficiencyLossPerDegree);

            List<CompProperties_TempControl> comps = radiator.def.comps.Where(cp => cp is CompProperties_TempControl).Select(cp => cp as CompProperties_TempControl).ToList();
            if (comps.Count != 1)
            {
                Log.Error(radiator.Label + " have 0 or more than 1 CompProperties_TempControl comps, this is a bug, report to the dev");
                // BUG
                return 0f;
            }
            float energyPerSecond = comps[0].energyPerSecond; // the power of the radiator

            RoomGroup roomGroup = intVec3_1.GetRoomGroup(req.Thing.Map);
            float roomSurface = 0f; // the power of the radiator
            if (roomGroup != null && !roomGroup.UsesOutdoorTemperature)
            {
                roomSurface = roomGroup.CellCount;
            }

            float unknownConstant = 4.16666651f; // Celsius cooled per Joules*Second*Meter^2  conversion rate

            float maxCooling = energyPerSecond * efficiency / roomSurface * unknownConstant; // max cooling power possible

            float targetTemp = radiator.compTempControl.targetTemperature;

            float coolingNeeded = targetTemp - cooledRoomTemp;

            float actualCoolingPerSecond = Mathf.Min(Mathf.Max(coolingNeeded, maxCooling), 0) * 60;

            return actualCoolingPerSecond;
        }

        public static float GetMaxCoolingPerSecond(StatRequest req, bool applyPostProcess = true)
        {
            if (!(req.Thing is Building_TempControl))
            {
                return 0f;
            }

            Building_TempControl radiator = (Building_TempControl)req.Thing;

            IntVec3 intVec3_1 = radiator.Position + IntVec3.North.RotatedBy(radiator.Rotation);
            IntVec3 intVec3_2 = radiator.Position + IntVec3.South.RotatedBy(radiator.Rotation);

            float cooledRoomTemp = intVec3_1.GetTemperature(radiator.Map);

            float extRoomTemp = intVec3_2.GetTemperature(radiator.Map);

            float roomTempGradient = (cooledRoomTemp - extRoomTemp);

            float efficiencyLossPerDegree = 1.0f / 130.0f; // SOS2 internal value, means loss of efficiency for each degree above targettemp, lose 50% at 65C above targetTemp, 100% at 130+

            float efficiency = (1f - roomTempGradient * efficiencyLossPerDegree);

            List<CompProperties_TempControl> comps = radiator.def.comps.Where(cp => cp is CompProperties_TempControl).Select(cp => cp as CompProperties_TempControl).ToList();
            if (comps.Count != 1)
            {
                Log.Error(radiator.Label + " have 0 or more than 1 CompProperties_TempControl comps, this is a bug, report to the dev");
                // BUG
                return 0f;
            }
            float energyPerSecond = comps[0].energyPerSecond; // the power of the radiator

            RoomGroup roomGroup = intVec3_1.GetRoomGroup(req.Thing.Map);
            float roomSurface = 0f; // the power of the radiator
            if (roomGroup != null && !roomGroup.UsesOutdoorTemperature)
            {
                roomSurface = roomGroup.CellCount;
            }

            float unknownConstant = 4.16666651f; // Celsius cooled per Joules*Second*Meter^2  conversion rate

            float maxCoolingPerSecond = energyPerSecond * efficiency / roomSurface * unknownConstant * 60; // max cooling power possible

            return maxCoolingPerSecond;
        }

        public static float GetHeatVentTick(StatRequest req, bool applyPostProcess = true)
        {
            List<CompProperties_ShipHeat> shipHeat = req.Thing.def.comps.Where(cp => cp is CompProperties_ShipHeat).Select(cp => cp as CompProperties_ShipHeat).ToList();
            if (shipHeat.Count == 0)
            {
                // something is wrong here, there is nothing
                return 1;
            }
            if (shipHeat.Count > 1)
            {
                //there is a duplicate, that s wrong too
                return 1;
            }
            return shipHeat[0].heatVentTick;
        }

        public static int GetHeatVentTick(ThingDef def)
        {
            /*
             * Learned tip:
             * those three methods allows to fetch the same things from iterables
             * using 3 different ways
             */

            /* the loop way
            foreach (CompProperties cp in def.comps)
            {
                if (cp is CompProperties_ShipHeat shipheat)
                {
                    Log.Message($"{shipheat.heatVentTick} heat vent tick (for loop)");
                }
            }
            */

            /* The linq way (sql like query)
            IEnumerable<CompProperties_ShipHeat> shipHeatslinq =
                from cp in def.comps
                where cp is CompProperties_ShipHeat
                select cp as CompProperties_ShipHeat;
            foreach (CompProperties_ShipHeat cp in shipHeatslinq)
            {
                Log.Message($"{cp.heatVentTick} heat vent tick (linq)");
            }
            */

            /* The functional way
            IEnumerable<CompProperties_ShipHeat> shipHeatsfunc = def.comps.Where(cp => cp is CompProperties_ShipHeat).Select(cp=> cp as CompProperties_ShipHeat);
            foreach (CompProperties_ShipHeat cp in shipHeatsfunc)
            {
                Log.Message($"{cp.heatVentTick} heat vent tick (functional)");
            }
            */

            //but I love functional so..
            List<CompProperties_ShipHeat> shipHeat = def.comps.Where(cp => cp is CompProperties_ShipHeat).Select(cp => cp as CompProperties_ShipHeat).ToList();
            if (shipHeat.Count == 0)
            {
                // something is wrong here, there is nothing
                return 1;
            }
            if (shipHeat.Count > 1)
            {
                //there is a duplicate, that s wrong too
                return 1;
            }
            return shipHeat[0].heatVentTick;
        }
    }
}