using System;
using System.Collections.Generic;
using AutoMcD.PocketGear.Net.Messages;
using AutoMcD.PocketGear.Settings;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sisk.Utils.Logging;
using Sisk.Utils.Profiler;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

// ReSharper disable UsePatternMatching
// ReSharper disable ArrangeAccessorOwnerBody

namespace AutoMcD.PocketGear.Logic {
    // bug: IMyMotorStator.AttachedEntityChanged throws "Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type.". Once this is solved i should use this to desable damage protection in with this.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorAdvancedStator), false, POCKETGEAR_BASE, POCKETGEAR_BASE_LARGE, POCKETGEAR_BASE_LARGE_SMALL, POCKETGEAR_BASE_SMALL)]
    public class PocketGearBase : MyGameLogicComponent {
        public const float FORCED_LOWER_LIMIT_DEG = 333.5f;
        public const float FORCED_UPPER_LIMIT_DEG = 360.0f;
        private const float FORCED_LOWER_LIMIT_RAD = (float) (Math.PI * FORCED_LOWER_LIMIT_DEG / 180.0);
        private const float FORCED_UPPER_LIMIT_RAD = (float) (Math.PI * FORCED_UPPER_LIMIT_DEG / 180.0);

        private const string POCKETGEAR_BASE = "MA_PocketGear_Base";
        private const string POCKETGEAR_BASE_LARGE = "MA_PocketGear_L_Base";
        private const string POCKETGEAR_BASE_LARGE_SMALL = "MA_PocketGear_L_Base_sm";
        private const string POCKETGEAR_BASE_SMALL = "MA_PocketGear_Base_sm";
        private const int PROTECTION_RADIUS = 2;

        public static readonly HashSet<string> PocketGearIds = new HashSet<string> { POCKETGEAR_BASE, POCKETGEAR_BASE_LARGE, POCKETGEAR_BASE_LARGE_SMALL, POCKETGEAR_BASE_SMALL };

        private bool _changePocketGearPadState;
        private int _changePocketGearPadStateAfterTicks;
        private bool _isJustPlaced;
        private bool _lastAttachedState;
        private long _lastKnownTopGridId;
        private bool _manualLock;
        private MatrixD _manualLockBaseMatrix;
        private MatrixD _manualLockTopMatrix;
        private HashSet<IMySlimBlock> _neighbors = new HashSet<IMySlimBlock>();
        private IMyMotorStator _pocketGearBase;
        private IMyLandingGear _pocketGearPad;
        private int _resetManualLockAfterTicks;
        private PocketGearBaseSettings _settings;

        public bool CanPocketGearBeBuilt => _pocketGearBase?.Top != null && _pocketGearPad == null;

        public bool CanRetract {
            get {
                if (_pocketGearPad == null) {
                    _pocketGearPad = GetPocketGearPad(_pocketGearBase);
                }

                return _pocketGearPad != null && _pocketGearBase.IsWorking && _pocketGearPad != null && (!_pocketGearPad.IsLocked || CurrentBehavior != LockRetractBehaviors.PreventRetract);
            }
        }

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

        public bool IsDeploying => _pocketGearBase.TargetVelocityRPM > 0 || ShouldDeploy;

        protected ILogger Log { get; set; }

        public bool ShouldDeploy {
            get { return _settings.ShouldDeploy; }
            set {
                if (value != _settings.ShouldDeploy) {
                    _settings.ShouldDeploy = value;
                    Mod.Static.Network?.Sync(new PropertySyncMessage(Entity.EntityId, nameof(ShouldDeploy), value));
                }
            }
        }

        private static void GetNeighbours(IMySlimBlock slimBlock, int radius, ref HashSet<IMySlimBlock> slimBlocks) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(GetNeighbours)) : null) {
                foreach (var neighbour in slimBlock.Neighbours) {
                    if (slimBlocks.Contains(neighbour)) {
                        continue;
                    }

                    slimBlocks.Add(neighbour);
                    if (radius > 1) {
                        GetNeighbours(neighbour, radius - 1, ref slimBlocks);
                    }
                }
            }
        }

        private static IMyLandingGear GetPocketGearPad(IMyMechanicalConnectionBlock stator) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(GetPocketGearPad)) : null) {
                var rotor = stator?.Top;
                if (rotor == null) {
                    return null;
                }

                var cubeGrid = rotor.CubeGrid;
                var gridSize = cubeGrid.GridSize;
                var position = rotor.GetPosition();
                var forward = rotor.WorldMatrix.Forward;
                var left = rotor.WorldMatrix.Left;
                Vector3D origin;
                switch (rotor.BlockDefinition.SubtypeId) {
                    case PocketGearPartLogic.POCKETGEAR_PART:
                        origin = position + left * gridSize * 2 + forward * gridSize;
                        break;
                    case PocketGearPartLogic.POCKETGEAR_PART_LARGE:
                        origin = position + left * (gridSize * 5) + forward * gridSize;
                        break;
                    case PocketGearPartLogic.POCKETGEAR_PART_SMALL:
                        origin = position + left + forward * gridSize;
                        break;
                    case PocketGearPartLogic.POCKETGEAR_PART_LARGE_SMALL:
                        origin = position + left * (gridSize * 5) + forward * gridSize;
                        break;
                    default:
                        throw new Exception($"Unknown PocketGearPart SubtypeId: {rotor.BlockDefinition.SubtypeId}");
                }

                var rotorPosition = cubeGrid.WorldToGridInteger(origin);
                var slimBlock = cubeGrid.GetCubeBlock(rotorPosition);
                var landingGear = slimBlock?.FatBlock as IMyLandingGear;
                return landingGear;
            }
        }

        public override void Close() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(Close)) : null) {
                if (Mod.Static.Network != null) {
                    Mod.Static.Network.Unregister<PropertySyncMessage>(Entity.EntityId, OnPropertySyncMessage);
                }

                _pocketGearBase.LimitReached -= OnLimitReached;
                _pocketGearBase.CubeGrid.OnIsStaticChanged -= OnIsStaticChanged;

                if (Mod.Static.DamageHandler != null) {
                    var myCubeGrid = _pocketGearBase.CubeGrid as MyCubeGrid;
                    if (myCubeGrid != null) {
                        myCubeGrid.OnHierarchyUpdated -= OnHierarchyUpdated;
                    }

                    _pocketGearBase.CubeGrid.OnBlockAdded -= OnBlockAdded;
                    _pocketGearBase.CubeGrid.OnBlockRemoved -= OnBlockRemoved;

                    DisableProtection();
                }
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(Init)) : null) {
                Log = Mod.Static.Log.ForScope<PocketGearBase>();

                _pocketGearBase = Entity as IMyMotorStator;
                _isJustPlaced = _pocketGearBase?.CubeGrid?.Physics != null;
                _settings = Load(Entity, new Guid(PocketGearBaseSettings.GUID));

                if (Mod.Static.Network != null) {
                    Mod.Static.Network.Register<PropertySyncMessage>(Entity.EntityId, OnPropertySyncMessage);
                }

                if (Entity.Storage == null) {
                    Entity.Storage = new MyModStorageComponent();
                }

                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override bool IsSerialized() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(Save)) : null) {
                Save(_pocketGearBase, new Guid(PocketGearBaseSettings.GUID), _settings);
                return base.IsSerialized();
            }
        }

        public override void UpdateAfterSimulation() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(UpdateAfterSimulation)) : null) {
                if (_manualLock) {
                    if (_resetManualLockAfterTicks <= 0) {
                        _manualLock = false;
                        _pocketGearBase.RotorLock = false;
                        _pocketGearBase.TargetVelocityRPM = DeployVelocity * (ShouldDeploy ? 1 : -1);
                    }
                }

                if (_changePocketGearPadState) {
                    _changePocketGearPadStateAfterTicks--;
                    if (_changePocketGearPadStateAfterTicks <= 0) {
                        ChangePocketGearPadState(true);
                        _changePocketGearPadState = false;
                    }
                }

                if (_changePocketGearPadStateAfterTicks <= 0 && !_changePocketGearPadState && _resetManualLockAfterTicks <= 0 && !_manualLock) {
                    NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
                }
            }
        }

        public override void UpdateBeforeSimulation() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(UpdateBeforeSimulation)) : null) {
                if (_manualLock) {
                    _resetManualLockAfterTicks--;
                    _pocketGearBase.CubeGrid.WorldMatrix = _manualLockBaseMatrix;
                    var topGrid = _pocketGearBase.TopGrid;
                    if (topGrid != null) {
                        topGrid.WorldMatrix = _manualLockTopMatrix;
                        topGrid.Physics?.ClearSpeed();
                        _pocketGearBase.Physics?.ClearSpeed();
                    }
                }

                if (_changePocketGearPadStateAfterTicks <= 0 && !_changePocketGearPadState && _resetManualLockAfterTicks <= 0 && !_manualLock) {
                    NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
                }
            }
        }

        public override void UpdateOnceBeforeFrame() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(UpdateOnceBeforeFrame)) : null) {
                if (_pocketGearBase.CubeGrid?.Physics == null) {
                    return;
                }

                if (_isJustPlaced) {
                    SwitchDeployState(true);
                }

                // bug: ImyRotorStator.UpperLimitDeg requirs an radian.
                _pocketGearBase.LowerLimitRad = FORCED_LOWER_LIMIT_RAD;
                _pocketGearBase.UpperLimitRad = FORCED_UPPER_LIMIT_RAD;

                if (_pocketGearBase.TopGrid != null) {
                    _lastAttachedState = true;
                    _lastKnownTopGridId = _pocketGearBase.TopGrid.EntityId;
                    if (!_isJustPlaced) {
                        _pocketGearPad = GetPocketGearPad(_pocketGearBase);
                    }
                }

                Mod.Static.Controls.Base.DeployRetract.UpdateVisual();
                Mod.Static.Controls.Base.PlacePocketGearPad.UpdateVisual();

                _pocketGearBase.LimitReached += OnLimitReached;
                _pocketGearBase.CubeGrid.OnIsStaticChanged += OnIsStaticChanged;

                if (Mod.Static.DamageHandler != null) {
                    // hack: use this to check if top is detached until the IMyMotorStator.AttachedEntityChanged bug is fixed.
                    var myCubeGrid = _pocketGearBase.CubeGrid as MyCubeGrid;
                    if (myCubeGrid != null) {
                        myCubeGrid.OnHierarchyUpdated += OnHierarchyUpdated;
                    }

                    _pocketGearBase.CubeGrid.OnBlockAdded += OnBlockAdded;
                    _pocketGearBase.CubeGrid.OnBlockRemoved += OnBlockRemoved;

                    GetNeighbours(_pocketGearBase.SlimBlock, PROTECTION_RADIUS, ref _neighbors);

                    if (IsDeploying) {
                        EnableProtection();
                    }
                }
            }
        }

        public void ManualRotorLock() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(ManualRotorLock)) : null) {
                _manualLock = true;
                _manualLockBaseMatrix = _pocketGearBase.CubeGrid.WorldMatrix;
                _manualLockTopMatrix = _pocketGearBase.TopGrid?.WorldMatrix ?? MatrixD.Zero;
                _resetManualLockAfterTicks = 25;
                _pocketGearBase.RotorLock = true;
                _pocketGearBase.TargetVelocityRPM = 0;
                _pocketGearBase.TopGrid?.Physics?.ClearSpeed();
                _pocketGearBase.CubeGrid?.Physics?.ClearSpeed();
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        public void OnPocketGearPadAdded(IMyLandingGear pad) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnPocketGearPadAdded)) : null) {
                using (Log.BeginMethod(nameof(OnPocketGearPadAdded))) {
                    Log.Debug("PocketGear Pad added.");
                    _pocketGearPad = pad;
                    if (IsDeploying && Mod.Static.DamageHandler != null) {
                        Mod.Static.DamageHandler.EnableProtection(pad.SlimBlock);
                    }

                    Mod.Static.Controls.Base.PlacePocketGearPad.UpdateVisual();
                }
            }
        }

        public void OnPocketGearPadRemoved(IMySlimBlock slimBlock) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnPocketGearPadRemoved)) : null) {
                using (Log.BeginMethod(nameof(OnPocketGearPadRemoved))) {
                    Log.Debug("PocketGear Pad removed.");
                    Mod.Static.DamageHandler?.DisableProtection(slimBlock);
                    _pocketGearPad = null;

                    Mod.Static.Controls.Base.PlacePocketGearPad.UpdateVisual();
                }
            }
        }

        public void OnPocketGearPartAttached() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnPocketGearPartAttached)) : null) {
                using (Log.BeginMethod(nameof(OnPocketGearPartAttached))) {
                    Log.Debug("PocketGear Part attached.");

                    _lastAttachedState = true;
                    _pocketGearPad = GetPocketGearPad(_pocketGearBase);
                    _lastKnownTopGridId = _pocketGearBase.TopGrid.EntityId;

                    if (IsDeploying && Mod.Static.DamageHandler != null) {
                        EnableProtection();
                    }

                    Mod.Static.Controls.Base.DeployRetract.UpdateVisual();
                    Mod.Static.Controls.Base.PlacePocketGearPad.UpdateVisual();
                }
            }
        }

        public void OnPocketGearPartDetached() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnPocketGearPartDetached)) : null) {
                using (Log.BeginMethod(nameof(OnPocketGearPartDetached))) {
                    Log.Debug("PocketGear Part detached.");

                    if (Mod.Static.DamageHandler != null) {
                        DisableProtection();
                    }

                    _lastAttachedState = false;
                    _pocketGearPad = null;

                    Mod.Static.Controls.Base.DeployRetract.UpdateVisual();
                    Mod.Static.Controls.Base.PlacePocketGearPad.UpdateVisual();
                }
            }
        }

        public void SwitchDeployState(bool deploy) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(SwitchDeployState)) : null) {
                if (deploy) {
                    ShouldDeploy = true;
                    _pocketGearBase.TargetVelocityRPM = DeployVelocity;
                    ChangePocketGearPadStateAfterTicks(150);
                } else {
                    Retract();
                    _changePocketGearPadState = false;
                }
            }
        }

        private void ChangePocketGearPadState(bool deployed) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(ChangePocketGearPadState)) : null) {
                if (_pocketGearPad != null) {
                    _pocketGearPad.Enabled = deployed;
                }
            }
        }

        private void ChangePocketGearPadStateAfterTicks(int ticks) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(ChangePocketGearPadStateAfterTicks)) : null) {
                _changePocketGearPadState = true;
                _changePocketGearPadStateAfterTicks = ticks;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        private void DisableProtection() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(DisableProtection)) : null) {
                Mod.Static.DamageHandler.DisableProtection(_pocketGearBase.CubeGrid.EntityId);
                Mod.Static.DamageHandler.DisableProtection(_lastKnownTopGridId);
            }
        }

        private void EnableProtection() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(EnableProtection)) : null) {
                if (Mod.Static.DamageHandler != null) {
                    Mod.Static.DamageHandler.EnableProtection(_pocketGearBase.SlimBlock);
                    foreach (var slimBlock in _neighbors) {
                        Mod.Static.DamageHandler.EnableProtection(slimBlock);
                    }

                    Mod.Static.DamageHandler.EnableProtection(_pocketGearBase.Top?.SlimBlock);
                    Mod.Static.DamageHandler.EnableProtection(_pocketGearPad?.SlimBlock);
                }
            }
        }

        private PocketGearBaseSettings Load(IMyEntity entity, Guid guid) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(ChangePocketGearPadStateAfterTicks)) : null) {
                using (Log.BeginMethod(nameof(Load))) {
                    var storage = entity.Storage;
                    PocketGearBaseSettings settings;
                    if (storage != null && storage.ContainsKey(guid)) {
                        try {
                            var str = storage[guid];
                            var data = Convert.FromBase64String(str);

                            settings = MyAPIGateway.Utilities.SerializeFromBinary<PocketGearBaseSettings>(data);
                            if (settings != null) {
                                return settings;
                            }
                        } catch (Exception exception) {
                            Log.Error(exception);
                            Log.Error(exception.StackTrace);
                        }
                    }

                    Log.Debug($"No saved setting for '{entity}' found. Using default settings");
                    settings = new PocketGearBaseSettings();
                    if (!(Math.Abs(Math.Abs(_pocketGearBase.TargetVelocityRPM)) < 0.01)) {
                        settings.DeployVelocity = Math.Abs(_pocketGearBase.TargetVelocityRPM);
                    }

                    return settings;
                }
            }
        }

        private void OnBlockAdded(IMySlimBlock slimBlock) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnBlockAdded)) : null) {
                using (Log.BeginMethod(nameof(OnBlockAdded))) {
                    if (Mod.Static.DamageHandler == null) {
                        return;
                    }

                    var position = slimBlock.Position;
                    var distance = Vector3I.DistanceManhattan(position, _pocketGearBase.Position);
                    if (distance <= PROTECTION_RADIUS) {
                        _neighbors.Add(slimBlock);
                        if (IsDeploying) {
                            Mod.Static.DamageHandler.EnableProtection(slimBlock);
                        }
                    }
                }
            }
        }

        private void OnBlockRemoved(IMySlimBlock slimBlock) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnBlockRemoved)) : null) {
                using (Log.BeginMethod(nameof(OnBlockRemoved))) {
                    if (Mod.Static.DamageHandler == null) {
                        return;
                    }

                    if (!_neighbors.Contains(slimBlock)) {
                        return;
                    }

                    var position = slimBlock.Position;
                    var distance = Vector3I.DistanceManhattan(position, _pocketGearBase.Position);
                    if (distance <= PROTECTION_RADIUS) {
                        _neighbors.Remove(slimBlock);
                        Mod.Static.DamageHandler.DisableProtection(slimBlock);
                    }
                }
            }
        }

        private void OnHierarchyUpdated(MyCubeGrid cubeGrid) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnHierarchyUpdated)) : null) {
                if (_lastAttachedState && _pocketGearBase.TopGrid == null) {
                    OnPocketGearPartDetached();
                } else if (!_lastAttachedState && _pocketGearBase.TopGrid != null) {
                    OnPocketGearPartAttached();
                }
            }
        }

        private void OnIsStaticChanged(IMyCubeGrid cubeGrid, bool isStatic) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnIsStaticChanged)) : null) {
                ManualRotorLock();
            }
        }

        private void OnLimitReached(bool upperLimit) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnLimitReached)) : null) {
                ChangePocketGearPadState(upperLimit);
                if (upperLimit) {
                    EnableProtection();
                } else {
                    DisableProtection();
                }
            }
        }

        private void OnPropertySyncMessage(ulong sender, PropertySyncMessage message) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(OnPropertySyncMessage)) : null) {
                if (message == null) {
                    return;
                }

                switch (message.Name) {
                    case nameof(DeployVelocity):
                        _settings.DeployVelocity = message.GetValueAs<float>();
                        Mod.Static.Controls.Base.DeployVelocity.UpdateVisual();
                        break;
                    case nameof(CurrentBehavior):
                        _settings.LockRetractBehavior = message.GetValueAs<LockRetractBehaviors>();
                        Mod.Static.Controls.Base.LockRetractBehavior.UpdateVisual();
                        Mod.Static.Controls.Base.DeployRetract.UpdateVisual();
                        break;
                    case nameof(ShouldDeploy):
                        _settings.ShouldDeploy = message.GetValueAs<bool>();
                        Mod.Static.Controls.Base.DeployRetract.UpdateVisual();
                        break;
                }
            }
        }

        private void Retract() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(Retract)) : null) {
                if (!CanRetract) {
                    return;
                }

                ShouldDeploy = false;
                if (CurrentBehavior == LockRetractBehaviors.UnlockOnRetract) {
                    PocketGearPad.Unlock(_pocketGearPad);
                }

                _pocketGearBase.TargetVelocityRPM = DeployVelocity * -1;
            }
        }

        private void Save(IMyEntity entity, Guid guid, PocketGearBaseSettings settings) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBase), nameof(Save)) : null) {
                using (Log.BeginMethod(nameof(Save))) {
                    try {
                        if (entity.Storage == null) {
                            entity.Storage = new MyModStorageComponent();
                        }

                        var storage = entity.Storage;
                        var data = MyAPIGateway.Utilities.SerializeToBinary(settings);
                        var str = Convert.ToBase64String(data);
                        storage[guid] = str;
                    } catch (Exception exception) {
                        Log.Error(exception);
                        Log.Error(exception.StackTrace);
                    }
                }
            }
        }
    }
}