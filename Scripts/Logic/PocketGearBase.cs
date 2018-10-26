using System;
using System.Collections.Generic;
using AutoMcD.PocketGear.Data;
using AutoMcD.PocketGear.Extensions;
using AutoMcD.PocketGear.Net.Messages;
using AutoMcD.PocketGear.Settings;
using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sisk.Utils.Logging;
using Sisk.Utils.Profiler;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable UsePatternMatching
// ReSharper disable UseNegatedPatternMatching

namespace AutoMcD.PocketGear.Logic {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorAdvancedStator), false, Defs.Base.NORMAL, Defs.Base.LARGE_NORMAL, Defs.Base.LARGE_SMALL, Defs.Base.SMALL)]
    public class PocketGearBase : MyGameLogicComponent {
        public const float FORCED_LOWER_LIMIT_DEG = 334.0f;
        public const float FORCED_UPPER_LIMIT_DEG = 360.0f;
        private const float FORCED_LOWER_LIMIT_RAD = (float) (Math.PI * FORCED_LOWER_LIMIT_DEG / 180.0);
        private const float FORCED_UPPER_LIMIT_RAD = (float) (Math.PI * FORCED_UPPER_LIMIT_DEG / 180.0);

        private bool _lastAttachedState;
        private PocketGearBaseSettings _settings;
        private IMyCubeGrid _topGrid;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PocketGearBase" /> game logic component.
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
                    Mod.Static.Controls.Base.DeployRetract.UpdateVisual();
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
                    Mod.Static.Network?.Sync(new PropertySyncMessage(Entity.EntityId, nameof(DeployVelocity), value));
                }
            }
        }

        /// <summary>
        ///     Indicates if pocket gear is deploying.
        /// </summary>
        public bool IsDeploying => Stator.TargetVelocityRPM > 0 || ShouldDeploy;

        /// <summary>
        ///     Indicates if the block which holds this game logic is just placed.
        /// </summary>
        public bool IsJustPlaced { get; private set; }

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
        public bool ShouldDeploy {
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

        /// <inheritdoc />
        public override void Close() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(Close)) : null) {
                Stator.LimitReached -= OnLimitReached;

                var cubeGrid = _topGrid as MyCubeGrid;
                if (cubeGrid != null) {
                    cubeGrid.OnHierarchyUpdated -= OnHierarchyUpdated;
                }

                if (_topGrid != null) {
                    _topGrid.OnBlockAdded -= OnTopGridBlockAdded;
                    _topGrid.OnBlockRemoved -= OnTopGridBlockRemoved;
                }
            }
        }

        /// <inheritdoc />
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(Init)) : null) {
                using (Log.BeginMethod(nameof(Init))) {
                    base.Init(objectBuilder);

                    Stator = Entity as IMyMotorStator;
                    if (Stator == null) {
                        Log.Error($"Entity is not of type {typeof(IMyMotorStator)}");
                        return;
                    }

                    if (Stator.IsProjected()) {
                        return;
                    }

                    // bug: ImyRotorStator.UpperLimitDeg requires an radian.
                    Stator.LowerLimitRad = FORCED_LOWER_LIMIT_RAD;
                    Stator.UpperLimitRad = FORCED_UPPER_LIMIT_RAD;
                    Stator.LimitReached += OnLimitReached;

                    IsJustPlaced = Stator.CubeGrid?.Physics != null;

                    try {
                        _settings = Stator.Load<PocketGearBaseSettings>(new Guid(PocketGearBaseSettings.GUID));
                    } catch (Exception exception) {
                        Log.Error(exception);
                        _settings = new PocketGearBaseSettings();
                    }

                    if (Entity.Storage == null) {
                        Entity.Storage = new MyModStorageComponent();
                    }

                    if (Mod.Static.Network != null) {
                        Mod.Static.Network.Register<PropertySyncMessage>(Entity.EntityId, OnPropertySyncMessage);
                    }

                    // bug: IMyMotorStator.AttachedEntityChanged throws "Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type.".
                    //Stator.AttachedEntityChanged += OnAttachedEntityChanged;

                    // hack: until IMyMotorStator.AttachedEntityChanged event is fixed.
                    var cubeGrid = Stator.CubeGrid as MyCubeGrid;
                    if (cubeGrid != null) {
                        cubeGrid.OnHierarchyUpdated += OnHierarchyUpdated;
                    }
                }
            }
        }

        /// <summary>
        ///     Tells the component container serializer whether this component should be saved.
        ///     I use it to call the <see cref="IMyEntity.Save" /> extension method.
        /// </summary>
        /// <returns></returns>
        public override bool IsSerialized() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(IsSerialized)) : null) {
                using (Log.BeginMethod(nameof(IsSerialized))) {
                    try {
                        Stator.Save(new Guid(PocketGearBaseSettings.GUID), _settings);
                    } catch (Exception exception) {
                        Log.Error(exception);
                    }

                    return base.IsSerialized();
                }
            }
        }

        /// <summary>
        ///     Deploy pocket gear.
        /// </summary>
        public void Deploy() {
            // todo: implement logic.
        }

        /// <summary>
        ///     Place a pocket gear pad. This is will start in an separate thread.
        /// </summary>
        public void PlacePad() {
            MyAPIGateway.Parallel.Start(PlacePad, PlacePadCompleted, new PlacePadData(Stator.Top));
        }

        /// <summary>
        ///     Retract pocket gear.
        /// </summary>
        public void Retract() {
            // todo: implement logic.
        }

        /// <summary>
        ///     Called if <see cref="IMyMotorStator.Top" /> changed.
        /// </summary>
        /// <param name="base">The base on which the top is changed.</param>
        private void OnAttachedEntityChanged(IMyMotorBase @base) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnAttachedEntityChanged)) : null) {
                if (@base?.Top != null) {
                    if (Stator.TopGrid != null) {
                        _topGrid = Stator.TopGrid;
                        // used to get attached pocket gear pad.
                        _topGrid.OnBlockAdded += OnTopGridBlockAdded;
                        _topGrid.OnBlockRemoved += OnTopGridBlockRemoved;
                    }

                    MyAPIGateway.Parallel.Start(PlacePad, PlacePadCompleted, new PlacePadData(@base.Top));
                } else {
                    if (_topGrid != null) {
                        _topGrid.OnBlockAdded -= OnTopGridBlockAdded;
                        _topGrid.OnBlockRemoved -= OnTopGridBlockRemoved;
                        _topGrid = null;
                    }
                }
            }
        }

        // hack: until IMyMotorStator.AttachedEntityChanged event is fixed.
        /// <summary>
        ///     Used to check if <see cref="IMyMotorStator.Top" /> is changed, because of a the
        ///     <see cref="IMyMotorStator.AttachedEntityChanged" /> event bug.
        /// </summary>
        /// <param name="cubeGrid">The cube grid on which the hierarchy updated.</param>
        private void OnHierarchyUpdated(MyCubeGrid cubeGrid) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnHierarchyUpdated)) : null) {
                using (Log.BeginMethod(nameof(OnHierarchyUpdated))) {
                    if (cubeGrid.MarkedForClose) {
                        return;
                    }

                    if (!_lastAttachedState && Stator.Top != null) {
                        _lastAttachedState = true;
                        OnAttachedEntityChanged(Stator);
                    } else if (_lastAttachedState && Stator.Top == null) {
                        _lastAttachedState = false;
                        OnAttachedEntityChanged(Stator);
                    }
                }
            }
        }

        /// <summary>
        ///     Executed when upper or lower limit is reached.
        /// </summary>
        /// <param name="upperLimit">
        ///     Indicates which limit is reached. <see langword="true" /> means upper limit,
        ///     <see langword="false" /> lower limit.
        /// </param>
        private void OnLimitReached(bool upperLimit) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnLimitReached)) : null) {
                if (!upperLimit) {
                    if (Pad != null) {
                        Pad.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Called when a <see cref="PropertySyncMessage" /> received.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The <see cref="PropertySyncMessage" /> message received.</param>
        private void OnPropertySyncMessage(ulong sender, PropertySyncMessage message) {
            // todo: implement logic.
        }

        /// <summary>
        ///     Called when a new block is added to the top grid. Used to get the attached pocket gear pad.
        /// </summary>
        /// <param name="block">The block that is added.</param>
        private void OnTopGridBlockAdded(IMySlimBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnTopGridBlockAdded)) : null) {
                if (block.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_LandingGear) && Defs.Pad.Ids.Contains(block.BlockDefinition.Id.SubtypeId.String)) {
                    Pad = block.FatBlock as IMyLandingGear;
                }
            }
        }

        /// <summary>
        ///     Called when a block is removed from the top grid. Used to detect when attached pocket gear pad is removed.
        /// </summary>
        /// <param name="block">The block that is removed.</param>
        private void OnTopGridBlockRemoved(IMySlimBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnTopGridBlockRemoved)) : null) {
                if (block.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_LandingGear) && Defs.Pad.Ids.Contains(block.BlockDefinition.Id.SubtypeId.String)) {
                    if (block == Pad.SlimBlock) {
                        Pad = null;
                    }
                }
            }
        }

        /// <summary>
        ///     Place a pocket gear pad. This is called in an separate thread.
        /// </summary>
        /// <param name="workData">The work data used in this method.</param>
        private void PlacePad(WorkData workData) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(PlacePad)) : null) {
                using (Log.BeginMethod(nameof(PlacePad))) {
                    var data = workData as PlacePadData;

                    var rotor = data?.Head as IMyMotorRotor;
                    if (rotor == null) {
                        return;
                    }

                    var cubeGrid = rotor.CubeGrid;
                    var gridSize = cubeGrid.GridSize;
                    var matrix = rotor.WorldMatrix;
                    var left = matrix.Left;
                    var forward = matrix.Forward;
                    var up = matrix.Up;

                    var position = rotor.GetPosition();

                    Vector3D origin;
                    string padId;
                    switch (rotor.BlockDefinition.SubtypeId) {
                        case Defs.Part.NORMAL:
                            padId = Defs.Pad.NORMAL;
                            origin = position + left * gridSize * 2 + forward * gridSize;
                            break;
                        case Defs.Part.LARGE_NORMAL:
                            padId = Defs.Pad.LARGE_NORMAL;
                            origin = position + left * (gridSize * 5) + forward * gridSize;
                            break;
                        case Defs.Part.SMALL:
                            padId = Defs.Pad.SMALL;
                            origin = position + left + forward * gridSize;
                            break;
                        case Defs.Part.LARGE_SMALL:
                            padId = Defs.Pad.LARGE_SMALL;
                            origin = position + left * (gridSize * 5) + forward * gridSize;
                            break;
                        default:
                            data.Result = PlacePadResult.Failure;
                            Log.Error(new Exception($"Unknown PocketGearPart SubtypeId: {rotor.BlockDefinition.SubtypeId}"));
                            return;
                    }

                    var padPosition = cubeGrid.WorldToGridInteger(origin);
                    if (cubeGrid.CubeExists(padPosition)) {
                        Log.Debug($"There is already a block on this position: {padPosition}.");
                        data.Result = PlacePadResult.Failure;
                        return;
                    }

                    var canPlaceCube = cubeGrid.CanAddCube(padPosition);
                    if (!canPlaceCube) {
                        Log.Debug($"Unable to place block on this position: {padPosition}.");
                        data.Result = PlacePadResult.Failure;
                        return;
                    }

                    try {
                        var instantBuild = MyAPIGateway.Session.CreativeMode || MyAPIGateway.Session.HasCreativeRights && MyAPIGateway.Session.EnableCopyPaste;
                        var buildPercent = instantBuild ? 1 : 0.00001525902f;

                        var padBuilder = new MyObjectBuilder_LandingGear {
                            SubtypeName = padId,
                            Owner = Stator.OwnerId,
                            BuiltBy = Stator.OwnerId,
                            AutoLock = false,
                            BuildPercent = buildPercent,
                            IntegrityPercent = buildPercent
                        };

                        var cubeGridBuilder = new MyObjectBuilder_CubeGrid {
                            CreatePhysics = true,
                            GridSizeEnum = rotor.CubeGrid.GridSizeEnum,
                            PositionAndOrientation = new MyPositionAndOrientation(origin, forward, up)
                        };

                        cubeGridBuilder.CubeBlocks.Add(padBuilder);

                        var gridsToMerge = new List<MyObjectBuilder_CubeGrid> { cubeGridBuilder };

                        MyAPIGateway.Utilities.InvokeOnGameThread(() => (cubeGrid as MyCubeGrid)?.PasteBlocksToGrid(gridsToMerge, 0, false, false));
                        data.Result = PlacePadResult.Success;
                    } catch (Exception exception) {
                        data.Result = PlacePadResult.Failure;
                        Log.Error(exception);
                    }
                }
            }
        }

        /// <summary>
        ///     Get called after <see cref="PlacePad" /> task is completed.
        /// </summary>
        /// <param name="workData">The work data used in this method.</param>
        private void PlacePadCompleted(WorkData workData) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(PlacePadCompleted)) : null) {
                using (Log.BeginMethod(nameof(PlacePadCompleted))) {
                    var data = workData as PlacePadData;
                    if (data == null) {
                        return;
                    }

                    if (data.Result == PlacePadResult.Success) {
                        Log.Debug("PocketGar Pad placed.");
                    }
                }
            }
        }
    }
}