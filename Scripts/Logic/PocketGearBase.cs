using System;
using System.Collections.Generic;
using System.Linq;
using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sisk.PocketGear.Data;
using Sisk.PocketGear.Extensions;
using Sisk.PocketGear.Net.Messages;
using Sisk.PocketGear.Settings.V1;
using Sisk.Utils.Logging;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sisk.PocketGear.Logic {
    /// <summary>
    ///     Provide game logic for pocket gear bases.
    /// </summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorAdvancedStator), false, Defs.Base.NORMAL, Defs.Base.LARGE_NORMAL, Defs.Base.LARGE_SMALL, Defs.Base.SMALL)]
    public class PocketGearBase : MyGameLogicComponent {
        public const float FORCED_LOWER_LIMIT_DEG = 334.0f;
        public const float FORCED_UPPER_LIMIT_DEG = 360.0f;

        private const string ERROR_BUILD_SPOT_OCCUPIED = ERROR_UNABLE_TO_PLACE + " Build spot occupied.";
        private const string ERROR_UNABLE_TO_PLACE = "PocketGear Pad cannot be placed.";

        private const float FORCED_LOWER_LIMIT_RAD = (float) (Math.PI * FORCED_LOWER_LIMIT_DEG / 180.0);
        private const float FORCED_UPPER_LIMIT_RAD = (float) (Math.PI * FORCED_UPPER_LIMIT_DEG / 180.0);
        private const float TOGGLE_PAD_THRESHOLD = TOGGLE_PAD_THRESHOLD_PERCENT * (FORCED_UPPER_LIMIT_RAD - FORCED_LOWER_LIMIT_RAD) / 100 + FORCED_LOWER_LIMIT_RAD;
        private const int TOGGLE_PAD_THRESHOLD_PERCENT = 30;

        private HashSet<IMySlimBlock> _neighbors = new HashSet<IMySlimBlock>();
        private PocketGearBaseSettings _settings;
        private bool _togglePadWhenThresholdReached;
        private long _topGridId;

        /// <summary>
        ///     Initializes a new instance of the game logic component for PocketGear bases.
        /// </summary>
        public PocketGearBase() {
            Log = Mod.Static.Log.ForScope<PocketGearBase>();
        }

        /// <summary>
        ///     Indicates if a new pocket gear pad can be built.
        /// </summary>
        public bool CanBuiltPad => Stator?.Top != null && Pad == null;

        /// <summary>
        ///     Current lock/retract behavior.
        /// </summary>
        public LockRetractBehaviors CurrentBehavior {
            get { return _settings.LockRetractBehavior; }
            set {
                if (value != _settings.LockRetractBehavior) {
                    _settings.LockRetractBehavior = value;
                    Mod.Static.Controls.DeployRetract.UpdateVisual();
                    Mod.Static.Network?.Sync(new PropertySyncMessage(Entity.EntityId, nameof(CurrentBehavior), value));
                }
            }
        }

        /// <summary>
        ///     Indicates the deploy velocity.
        /// </summary>
        public float DeployVelocity {
            get { return _settings.DeployVelocity; }
            set {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (value != _settings.DeployVelocity) {
                    _settings.DeployVelocity = value;
                    Stator.TargetVelocityRPM = ShouldDeploy ? value : value * -1;
                    Mod.Static.Network?.Sync(new PropertySyncMessage(Entity.EntityId, nameof(DeployVelocity), value));
                }
            }
        }

        /// <summary>
        ///     Indicates if pocket gear is deploying.
        /// </summary>
        public bool IsDeploying => Stator.TargetVelocityRPM > 0 || ShouldDeploy;

        /// <summary>
        ///     Indicates the pad attached status.
        /// </summary>
        private bool IsPadAttached {
            get { return _settings.IsPadAttached; }
            set {
                if (value != _settings.IsPadAttached) {
                    _settings.IsPadAttached = value;
                }
            }
        }

        /// <summary>
        ///     Indicates if protection is enabled.
        /// </summary>
        private bool IsProtected { get; set; }

        /// <summary>
        ///     Logger used for logging.
        /// </summary>
        private ILogger Log { get; }

        /// <summary>
        ///     The attached PocketGear Pad.
        /// </summary>
        private IMyLandingGear Pad { get; set; }

        /// <summary>
        ///     Indicates if pocket gear should deploy.
        /// </summary>
        private bool ShouldDeploy {
            get { return _settings.ShouldDeploy; }
            set {
                if (value != _settings.ShouldDeploy) {
                    _settings.ShouldDeploy = value;
                    Mod.Static.Network?.Sync(new PropertySyncMessage(Entity.EntityId, nameof(ShouldDeploy), value));
                }
            }
        }

        /// <summary>
        ///     The entity which holds this game logic component.
        /// </summary>
        private IMyMotorStator Stator { get; set; }

        /// <summary>
        ///     Get neighbor blocks in given radius.
        /// </summary>
        /// <param name="slimBlock">The block used to find his neighbors.</param>
        /// <param name="radius">The radius used to search for neighbors.</param>
        /// <param name="slimBlocks">Found neighbors are stored here.</param>
        private static void GetNeighbors(IMySlimBlock slimBlock, int radius, ref HashSet<IMySlimBlock> slimBlocks) {
            foreach (var neighbor in slimBlock.Neighbours) {
                if (slimBlocks.Contains(neighbor)) {
                    continue;
                }

                slimBlocks.Add(neighbor);
                if (radius > 1) {
                    GetNeighbors(neighbor, radius - 1, ref slimBlocks);
                }
            }
        }

        /// <inheritdoc />
        public override void Close() {
            if (Mod.Static.Network != null) {
                Mod.Static.Network.Register<PropertySyncMessage>(Entity.EntityId, OnPropertySyncMessage);
            }

            if (Stator != null && (Mod.Static.Network == null || Mod.Static.Network.IsServer)) {
                Stator.AttachedEntityChanged -= OnAttachedEntityChanged;

                // todo: check if it is enough to run this on server.
                if (Mod.Static.DamageHandler != null) {
                    Stator.CubeGrid.OnBlockAdded -= OnBlockAdded;
                    Stator.CubeGrid.OnBlockRemoved -= OnBlockRemoved;

                    DisableProtection();
                }
            }
        }

        /// <inheritdoc />
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);

            Stator = Entity as IMyMotorStator;

            if (Mod.Static.Network == null || Mod.Static.Network.IsServer) {
                if (Entity.Storage == null) {
                    Entity.Storage = new MyModStorageComponent();
                }
            }
        }

        /// <summary>
        ///     Tells the component container serializer whether this component should be saved.
        ///     I use it to call the <see cref="IMyEntity.Save" /> extension method.
        /// </summary>
        /// <returns></returns>
        public override bool IsSerialized() {
            using (Log.BeginMethod(nameof(IsSerialized))) {
                if (Stator != null && (Mod.Static.Network == null || Mod.Static.Network.IsServer)) {
                    try {
                        Stator.Save(new Guid(PocketGearBaseSettings.GUID), _settings);
                    } catch (Exception exception) {
                        Log.Error(exception);
                    }
                }

                return base.IsSerialized();
            }
        }

        /// <inheritdoc />
        public override void OnAddedToScene() {
            using (Log.BeginMethod(nameof(OnAddedToScene))) {
                if (Stator.IsProjected()) {
                    return;
                }

                if (Mod.Static.Network == null || Mod.Static.Network.IsServer) {
                    try {
                        _settings = Stator.Load<PocketGearBaseSettings>(new Guid(PocketGearBaseSettings.GUID));
                        if (_settings != null) {
                            if (_settings.Version < PocketGearBaseSettings.VERSION) {
                                // todo: merge old and new settings in future versions.
                            }
                        } else {
                            _settings = new PocketGearBaseSettings();
                        }
                    } catch (Exception exception) {
                        if (exception.GetType().ToString() == "ProtoBuf.ProtoException") {
                            var old = Stator.Load<Settings.V0.PocketGearBaseSettings>(new Guid(Settings.V0.PocketGearBaseSettings.GUID));
                            if (old != null) {
                                Log.Warning("Old settings version found. Converting to current version.");
                                _settings = new PocketGearBaseSettings {
                                    DeployVelocity = old.DeployVelocity,
                                    ShouldDeploy = old.ShouldDeploy,
                                    LockRetractBehavior = old.LockRetractBehavior
                                };
                            } else {
                                Log.Error(exception);
                                _settings = new PocketGearBaseSettings();
                            }
                        } else {
                            Log.Error(exception);
                            _settings = new PocketGearBaseSettings();
                        }
                    }

                    Stator.LowerLimitRad = FORCED_LOWER_LIMIT_RAD;
                    Stator.UpperLimitRad = FORCED_UPPER_LIMIT_RAD;

                    // todo: check if it is enough to run this on server.
                    if (Mod.Static.DamageHandler != null) {
                        Stator.CubeGrid.OnBlockAdded += OnBlockAdded;
                        Stator.CubeGrid.OnBlockRemoved += OnBlockRemoved;

                        GetNeighbors(Stator.SlimBlock, Mod.Static.Settings.ProtectionRadius, ref _neighbors);
                    }

                    Stator.AttachedEntityChanged += OnAttachedEntityChanged;
                }

                if (Mod.Static.Network != null) {
                    Mod.Static.Network.Register<PropertySyncMessage>(Entity.EntityId, OnPropertySyncMessage);
                }

                if (!Mod.Static.Controls.AreTerminalControlsInitialized) {
                    Mod.Static.Controls.InitializeControls();
                }
            }
        }

        /// <inheritdoc />
        public override void UpdateAfterSimulation() {
            if (_togglePadWhenThresholdReached) {
                var angle = Stator.Angle;
                if (ShouldDeploy && angle > TOGGLE_PAD_THRESHOLD) {
                    if (Pad != null) {
                        Pad.Enabled = true;
                        _togglePadWhenThresholdReached = false;
                        EnableProtection();
                    }
                } else if (!ShouldDeploy && angle < TOGGLE_PAD_THRESHOLD) {
                    if (Pad != null) {
                        Pad.Enabled = false;
                        _togglePadWhenThresholdReached = false;
                        DisableProtection();
                    }
                }
            }

            if (!_togglePadWhenThresholdReached) {
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        /// <summary>
        ///     Deploy pocket gear.
        /// </summary>
        public void Deploy() {
            ShouldDeploy = true;
            Stator.TargetVelocityRPM = DeployVelocity;

            _togglePadWhenThresholdReached = true;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        /// <summary>
        ///     Place a pocket gear pad. This is will start in an separate thread.
        /// </summary>
        public void PlacePad() {
            if (!IsPadAttached) {
                MyAPIGateway.Parallel.Start(PlacePad, PlacePadCompleted, new PlacePadData(Stator.Top));
            }
        }

        /// <summary>
        ///     Retract pocket gear.
        /// </summary>
        public void Retract() {
            if (CurrentBehavior == LockRetractBehaviors.PreventRetract && Pad != null && Pad.IsLocked) {
                return;
            }

            if (CurrentBehavior == LockRetractBehaviors.UnlockOnRetract && Pad != null && Pad.IsLocked) {
                Pad.Unlock();
            }

            ShouldDeploy = false;
            Stator.TargetVelocityRPM = DeployVelocity * -1;

            _togglePadWhenThresholdReached = true;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        /// <summary>
        ///     Disable protection for protected blocks.
        /// </summary>
        private void DisableProtection() {
            if (Mod.Static.DamageHandler == null) {
                return;
            }

            IsProtected = false;
            Mod.Static.DamageHandler.DisableProtection(Stator.CubeGrid.EntityId);
            Mod.Static.DamageHandler.DisableProtection(_topGridId);
        }

        /// <summary>
        ///     Enable protection for blocks nearby a pocket gear.
        /// </summary>
        private void EnableProtection() {
            if (Mod.Static.DamageHandler == null || Pad == null || Stator.Top == null || Stator.Angle < TOGGLE_PAD_THRESHOLD) {
                return;
            }

            IsProtected = true;
            Mod.Static.DamageHandler.EnableProtection(Stator.SlimBlock);
            foreach (var slimBlock in _neighbors) {
                Mod.Static.DamageHandler.EnableProtection(slimBlock);
            }

            Mod.Static.DamageHandler.EnableProtection(Stator.Top.SlimBlock);
            Mod.Static.DamageHandler.EnableProtection(Pad.SlimBlock);
        }

        /// <summary>
        ///     Called if <see cref="IMyMotorStator.Top" /> changed.
        /// </summary>
        /// <param name="base">The base on which the top is changed.</param>
        private void OnAttachedEntityChanged(IMyMechanicalConnectionBlock @base) {
            if (@base.Top != null) {
                _topGridId = Stator.TopGrid.EntityId;

                if (!IsPadAttached) {
                    PlacePad();
                } else if (Pad == null) {
                    var blocks = new List<IMySlimBlock>();
                    Stator.TopGrid.GetBlocks(blocks);

                    var pad = blocks.Where(x => x.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_LandingGear)).Select(x => x.FatBlock).FirstOrDefault();
                    if (pad != null) {
                        Pad = (IMyLandingGear) pad;
                        Mod.Static.Controls.PlacePocketGearPad.UpdateVisual();
                    }
                }

                // todo: check if it is enough to run this on server.
                if (Mod.Static.DamageHandler != null) {
                    EnableProtection();
                }
            } else {
                IsPadAttached = false;
            }
        }

        /// <summary>
        ///     Called when a new block is placed on same grid as this pocket gear. Used to enable protection.
        /// </summary>
        /// <param name="slimBlock">The blocks that is added.</param>
        private void OnBlockAdded(IMySlimBlock slimBlock) {
            if (Mod.Static.DamageHandler == null) {
                return;
            }

            var position = slimBlock.Position;
            var distance = Vector3I.DistanceManhattan(position, Stator.Position);
            if (distance <= Mod.Static.Settings.ProtectionRadius) {
                _neighbors.Add(slimBlock);
                if (IsProtected) {
                    Mod.Static.DamageHandler.EnableProtection(slimBlock);
                }
            }
        }

        /// <summary>
        ///     Called when a block is removed from same grid as this pocket gear. Used to disable protection.
        /// </summary>
        /// <param name="slimBlock"></param>
        private void OnBlockRemoved(IMySlimBlock slimBlock) {
            if (Mod.Static.DamageHandler == null) {
                return;
            }

            if (!_neighbors.Contains(slimBlock)) {
                return;
            }

            var position = slimBlock.Position;
            var distance = Vector3I.DistanceManhattan(position, Stator.Position);
            if (distance <= Mod.Static.Settings.ProtectionRadius) {
                _neighbors.Remove(slimBlock);
                Mod.Static.DamageHandler.DisableProtection(slimBlock);
            }
        }

        /// <summary>
        ///     Called when a <see cref="PropertySyncMessage" /> received.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The <see cref="PropertySyncMessage" /> message received.</param>
        private void OnPropertySyncMessage(ulong sender, PropertySyncMessage message) {
            switch (message.Name) {
                case nameof(DeployVelocity):
                    _settings.DeployVelocity = message.GetValueAs<float>();
                    Mod.Static.Controls.DeployVelocity.UpdateVisual();
                    break;
                case nameof(CurrentBehavior):
                    _settings.LockRetractBehavior = message.GetValueAs<LockRetractBehaviors>();

                    Mod.Static.Controls.LockRetractBehavior.UpdateVisual();
                    Mod.Static.Controls.DeployRetract.UpdateVisual();
                    break;
                case nameof(ShouldDeploy):
                    _settings.ShouldDeploy = message.GetValueAs<bool>();
                    Mod.Static.Controls.DeployRetract.UpdateVisual();
                    break;
                default:
                    Log.Error($"Unexpected property name. '{message.Name}'");
                    break;
            }
        }

        /// <summary>
        ///     Place the matching smart rotor on top of this rotor. This is called in an separate thread.
        /// </summary>
        /// <param name="workData">The work data used in this method.</param>
        private void PlacePad(WorkData workData) {
            using (Log.BeginMethod(nameof(PlacePad))) {
                var data = workData as PlacePadData;

                if (data?.Head == null) {
                    return;
                }

                var head = data.Head;

                var cubeGrid = head.CubeGrid;
                var gridSize = cubeGrid.GridSize;
                var matrix = head.WorldMatrix;
                var up = matrix.Up;
                var left = matrix.Left;
                var forward = matrix.Forward;

                var headPosition = head.GetPosition();
                var baseSubtype = Stator.BlockDefinition.SubtypeId;
                var headSubType = head.BlockDefinition.SubtypeId;

                string padSubtype;
                if (!Mod.Static.Defs.BaseToPad.TryGetValue(baseSubtype, out padSubtype)) {
                    Log.Error($"No matching pad found for '{baseSubtype}'");
                    data.FlagAsFailed();
                    return;
                }

                Vector3D origin;
                switch (headSubType) {
                    case Defs.Part.NORMAL:
                        origin = headPosition + left * gridSize * 2 + forward * gridSize;
                        break;
                    case Defs.Part.LARGE_NORMAL:
                        origin = headPosition + left * (gridSize * 5) + forward * gridSize;
                        break;
                    case Defs.Part.SMALL:
                        origin = headPosition + left + forward * gridSize;
                        break;
                    case Defs.Part.LARGE_SMALL:
                        origin = headPosition + left * (gridSize * 5) + forward * gridSize;
                        break;
                    default:
                        Log.Error(new Exception($"Unknown PocketGearPart SubtypeId: {headSubType}"));
                        data.FlagAsFailed();
                        return;
                }

                var padPosition = cubeGrid.WorldToGridInteger(origin);
                if (cubeGrid.CubeExists(padPosition)) {
                    var slimBlock = cubeGrid.GetCubeBlock(padPosition);
                    var pad = slimBlock?.FatBlock as IMyLandingGear;
                    if (pad != null) {
                        if (pad.BlockDefinition.SubtypeId == padSubtype) {
                            Pad = pad;
                            data.FlagAsSucceeded();
                            return;
                        }
                    }

                    Log.Error(ERROR_BUILD_SPOT_OCCUPIED);
                    MyAPIGateway.Utilities.ShowNotification(ERROR_BUILD_SPOT_OCCUPIED);
                    data.FlagAsFailed();
                    return;
                }

                var canPlaceCube = cubeGrid.CanAddCube(padPosition);
                if (!canPlaceCube) {
                    Log.Error(ERROR_UNABLE_TO_PLACE);
                    MyAPIGateway.Utilities.ShowNotification(ERROR_UNABLE_TO_PLACE);
                    data.FlagAsFailed();
                }

                try {
                    var colorMask = Stator.SlimBlock.ColorMaskHSV;
                    var buildPercent = head.SlimBlock.IsFullIntegrity ? 1 : 0.00001525902f;
                    var padBuilder = new MyObjectBuilder_MotorAdvancedStator {
                        SubtypeName = padSubtype,
                        Owner = Stator.OwnerId,
                        BuiltBy = Stator.OwnerId,
                        BuildPercent = buildPercent,
                        IntegrityPercent = buildPercent,
                        LimitsActive = true,
                        MaxAngle = MathHelper.ToRadians(195),
                        MinAngle = MathHelper.ToRadians(-15),

                        Min = padPosition,
                        BlockOrientation = new SerializableBlockOrientation(head.Orientation.Forward, head.Orientation.Up),
                        ColorMaskHSV = colorMask
                    };

                    cubeGrid.AddBlock(padBuilder, false);
                    var slimBlock = cubeGrid.GetCubeBlock(padPosition);

                    Pad = slimBlock?.FatBlock as IMyLandingGear;
                    data.FlagAsSucceeded();
                } catch (Exception exception) {
                    Log.Error(exception);
                    Log.Error(exception.StackTrace);
                    data.FlagAsFailed();
                }
            }
        }

        /// <summary>
        ///     Get called after <see cref="PlacePad" /> task is completed.
        /// </summary>
        /// <param name="workData">The work data used in this method.</param>
        private void PlacePadCompleted(WorkData workData) {
            using (Log.BeginMethod(nameof(PlacePadCompleted))) {
                var data = workData as PlacePadData;

                if (data?.Head == null) {
                    return;
                }

                switch (data.Result) {
                    case PlacePadData.DataResult.Running:
                        break;
                    case PlacePadData.DataResult.Success:
                        IsPadAttached = true;
                        Deploy();
                        Log.Debug("Pad placed");
                        break;
                    case PlacePadData.DataResult.Failed:
                        Log.Error("Something went wrong when trying to place pad.");
                        break;
                }
            }
        }
    }
}