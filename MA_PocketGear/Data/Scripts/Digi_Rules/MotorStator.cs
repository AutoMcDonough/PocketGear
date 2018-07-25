using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Scripts
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorAdvancedStator), false, RotorLGMod.STATOR_SMALL, RotorLGMod.STATOR_LARGE, RotorLGMod.STATOR2_SMALL, RotorLGMod.STATOR2_LARGE)]
    public class MotorStator : MyGameLogicComponent
    {
        private IMyMotorStator stator;
        private bool isJustPlaced;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            stator = (IMyMotorStator)Entity;
            isJustPlaced = (stator?.CubeGrid?.Physics != null); // HACK NOTE: this doesn't cover blocks that start grids.
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame() // block's first update
        {
            try
            {
                if(stator?.CubeGrid?.Physics == null)
                    return; // ignore ghost grids

                RotorLGMod.Instance.SetupTerminalControls<IMyMotorStator>();

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

                if(isJustPlaced && MyAPIGateway.Multiplayer.IsServer)
                {
                    stator.TargetVelocityRPM = RotorLGMod.DEFAULT_VELOCITY_RPM;
                }
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLine(e);
                MyAPIGateway.Utilities.ShowNotification($"[ Error in {GetType().FullName}: {e.Message} ]", 10000, MyFontEnum.Red);
            }
        }

        public override void UpdateAfterSimulation() // each tick, only on clients because of the condition
        {
            try
            {
                // used to ensure that the limits aren't set beyond the limits allowed by the mod, only needed server side as clients will just spam the network
                if(MyAPIGateway.Multiplayer.IsServer)
                {
                    if(Math.Abs(stator.LowerLimitRad - RotorLGMod.FORCED_LOWER_LIMIT_RAD) > 0.01f)
                    {
                        stator.LowerLimitRad = RotorLGMod.FORCED_LOWER_LIMIT_RAD;
                    }

                    if(Math.Abs(stator.UpperLimitRad - RotorLGMod.FORCED_UPPER_LIMIT_RAD) > 0.01f)
                    {
                        stator.UpperLimitRad = RotorLGMod.FORCED_UPPER_LIMIT_RAD;
                    }

 /*                   if(!stator.CubeGrid.IsStatic)
                    {
                        // if grid is dynamic but physically locked to a static object then enable rotor lock
                        var setLock = stator.CubeGrid.Physics.IsStatic;

                        if(setLock != stator.RotorLock)
                            stator.RotorLock = setLock;
                    }
  */              }
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLine(e);
                MyAPIGateway.Utilities.ShowNotification($"[ Error in {GetType().FullName}: {e.Message} ]", 10000, MyFontEnum.Red);
            }
        }
    }
}
