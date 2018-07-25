using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Digi.Scripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class RotorLGMod : MySessionComponentBase
    {
        public const float DEFAULT_VELOCITY_RPM = -1f;

        public const double FORCED_LOWER_LIMIT_DEG = 333.5;
        public const double FORCED_UPPER_LIMIT_DEG = 360.0;

        public const float FORCED_LOWER_LIMIT_RAD = (float)(Math.PI * FORCED_LOWER_LIMIT_DEG / 180.0);
        public const float FORCED_UPPER_LIMIT_RAD = (float)(Math.PI * FORCED_UPPER_LIMIT_DEG / 180.0);

        public const string STATOR_SMALL = "MA_PocketGear_Base_sm";
        public const string STATOR_LARGE = "MA_PocketGear_Base";
        public const string STATOR2_SMALL = "MA_PocketGear_L_Base_sm";
        public const string STATOR2_LARGE = "MA_PocketGear_L_Base";

        // PocketGear subtypeIds.
        private const string ROTOR_PART_SMALL_LARGE = "MA_PocketGear_Rotor";
        private const string ROTOR_PART_SMALL_SMALL = "MA_PocketGear_Rotor_sm";
        private const string ROTOR_PART_LARGE_LARGE = "MA_PocketGear_L_Rotor";
        private const string ROTOR_PART_2_LARGE_SMALL = "MA_PocketGear_L_Rotor_sm";

        // An array used to easy compare subtypeIds.
        private readonly string[] _pocketGearIds = { ROTOR_PART_2_LARGE_SMALL, ROTOR_PART_LARGE_LARGE, ROTOR_PART_SMALL_LARGE, ROTOR_PART_SMALL_SMALL, STATOR_SMALL, STATOR_LARGE, STATOR2_SMALL, STATOR2_LARGE };

        /// <summary>
        ///     Initialize the session component.
        /// </summary>
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent) {
            // register the damage handler to prevent voxel damage to PocketGears.
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, OnDamage);
        }

        /// <summary>
        /// The damage handler that prevent voxel damage on PocketGears.
        /// </summary>
        /// <param name="target">The target which receives demage.</param>
        /// <param name="info">The damage info applied to target.</param>
        private void OnDamage(object target, ref MyDamageInformation info) {
            // voxel damage seems to be always of type deformation.
            if (info.Type != MyDamageType.Deformation) {
                return;
            }

            // check if the target is a block and of type IMyMotorAdvancedRotor or IMyMotorAdvancedStator.
            var slimBlock = target as IMySlimBlock;
            if (!(slimBlock?.FatBlock is IMyMotorAdvancedRotor || slimBlock?.FatBlock is IMyMotorAdvancedStator)) {
                return;
            }

            // gets the block definition and check if _pocketGearIds contains the SubtypeId.
            var def = slimBlock.FatBlock.BlockDefinition;
            if (_pocketGearIds.All(x => x != def.SubtypeId)) {
                return;
            }

            // try to get the entity from AttackerId but unfortunately it sometimes null.
            var entity = MyAPIGateway.Entities.GetEntityById(info.AttackerId);
            if (entity == null) {
                // try to get an fallback entity in range.
                var sphere = new BoundingSphereD(slimBlock.FatBlock.GetPosition(), slimBlock.CubeGrid.GridSize);
                var entities = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
                if (entities.Any(x => x is IMyVoxelBase)) {
                    entity = entities.First(x => x is IMyVoxelBase);
                } else {
                    return;
                }
            }

            // check if the attacker is type of IMyVoxelBase.
            if (!(entity is IMyVoxelBase)) {
                return;
            }

            // set the damage amount to zero.
            info.Amount = 0;
            info.IsDeformation = false;
        }

        private readonly HashSet<MyStringHash> subtypeIdsLookup = new HashSet<MyStringHash>()
        {
            MyStringHash.GetOrCompute(STATOR_SMALL),
            MyStringHash.GetOrCompute(STATOR_LARGE),
            MyStringHash.GetOrCompute(STATOR2_SMALL),
            MyStringHash.GetOrCompute(STATOR2_LARGE),
        };

        public static RotorLGMod Instance; // HACK currently (v1.187) only way to access session components from gamelogic components.

        private bool controlsCreated = false;
        private readonly Dictionary<string, Func<IMyTerminalBlock, bool>> controlVisibleFunc = new Dictionary<string, Func<IMyTerminalBlock, bool>>();
        private readonly Dictionary<string, Func<IMyTerminalBlock, bool>> actionEnabledFunc = new Dictionary<string, Func<IMyTerminalBlock, bool>>();

        public override void LoadData()
        {
            Instance = this;
        }

        protected override void UnloadData()
        {
            Instance = null; // allow this class to be collected
        }

        public void SetupTerminalControls<T>()
        {
            if(controlsCreated)
                return; // controls should be added only once per session per block type

            controlsCreated = true;

            var controls = new List<IMyTerminalControl>();
            var actions = new List<IMyTerminalAction>();
            MyAPIGateway.TerminalControls.GetControls<IMyMotorAdvancedStator>(out controls);
            MyAPIGateway.TerminalControls.GetActions<IMyMotorAdvancedStator>(out actions);

            var hideControlIds = new HashSet<string>()
            {
                "Add Small Top Part", 
                // "RotorLock",
                "LowerLimit",
                "UpperLimit",
                "Displacement",
            };

            var hideActionIds = new HashSet<string>()
            {
                "Add Small Top Part",
                // "RotorLock",

                "IncreaseLowerLimit",
                "DecreaseLowerLimit",
                "ResetLowerLimit",

                "IncreaseUpperLimit",
                "DecreaseUpperLimit",
                "ResetUpperLimit",

                "IncreaseDisplacement",
                "DecreaseDisplacement",
                "ResetDisplacement",
            };

            foreach(var control in controls)
            {
                string id = control.Id;

                if(hideControlIds.Contains(id))
                {
                    if(control.Visible != null)
                        controlVisibleFunc[id] = control.Visible; // preserve the existing visible condition

                    // basically appends our own visible condition on top of what's already there (from vanilla or other mods)
                    control.Visible = (b) =>
                    {
                        var func = controlVisibleFunc.GetValueOrDefault(id, null);
                        var originalCondition = (func == null ? true : func.Invoke(b));
                        return originalCondition && !HideControlForDefId(b.SlimBlock.BlockDefinition.Id);
                    };
                }
            }

            foreach(var action in actions)
            {
                string id = action.Id;

                if(hideActionIds.Contains(id))
                {
                    if(action.Enabled != null)
                        actionEnabledFunc[id] = action.Enabled;

                    // basically appends our own visible condition on top of what's already there (from vanilla or other mods)
                    action.Enabled = (b) =>
                    {
                        var func = actionEnabledFunc.GetValueOrDefault(id, null);
                        var originalCondition = (func == null ? true : func.Invoke(b));
                        return originalCondition && !HideControlForDefId(b.SlimBlock.BlockDefinition.Id);
                    };
                }
            }
        }

        private static bool HideControlForDefId(MyDefinitionId id)
        {
            return (id.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator) && Instance.subtypeIdsLookup.Contains(id.SubtypeId));
        }
    }
}
