using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace SOS2HS
{
    public class StatWorker_SOS2_Heatsink_MaxHeatOutputPerSecond : StatWorker
    {
        private bool IsConcernedThing(Thing thing)
        {
            CompShipHeatSink comp = thing.TryGetComp<CompShipHeatSink>();
            if (comp == null)
            {
                return false;
            }
            return !comp.Props.ventHeatToSpace;
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
            if (base.ShouldShowFor(req))
            {
                if (!req.HasThing)
                {
                    return false;
                }
                return IsConcernedThing(req.Thing);
            }
            return false;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (req.Thing == null)
            {
                Log.Error("Getting " + this.GetType().FullName + " for " + req.Def.defName + " without concrete thing. This always returns 1. This is a bug. Contact the dev.");
                return 1;
            }
            return SOS2HS_SOS2_Heatsink.GetMaxHeatOutputPerSecond(req, applyPostProcess);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {

            var heatPushed = SOS2HS_SOS2_Heatsink.GetMaxHeatPushed();
            var heatPushTick = SOS2HS_SOS2_Heatsink.GetHeatVentTick(req);
            float surface = req.Thing.Position.GetRoomGroup(req.Thing.Map).CellCount;
            float heatPushedPerSecond = heatPushed / heatPushTick * 60;
            float heatOutputPerSecond = heatPushedPerSecond / surface;

            SEB seb = new SEB("StatsReport_SOS2HS");
            seb.Simple("MaxHeatPushed", heatPushed);
            seb.Simple("HeatPushTickInterval", heatPushTick);
            seb.Simple("RoomSurface", surface);
            seb.Full("HeatPushedPerSecond", heatPushedPerSecond, heatPushed, heatPushTick);
            seb.Full("HeatOutputPerSecond", heatOutputPerSecond, heatPushedPerSecond, surface);

            return seb.ToString();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return new SEB("StatsReport_SOS2HS", "TemperaturePerSecond").ValueNoFormat(GetValueUnfinalized(optionalReq).ToString("0.###")).ToString();
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
        {
            yield return new Dialog_InfoCard.Hyperlink();
        }
    }
}