using System;
using System.Collections.Generic;
using AutoMcD.PocketGear.Net;
using AutoMcD.PocketGear.Settings;
using AutoMcD.PocketGear.TerminalControls;
using Sandbox.Common.ObjectBuilders;
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
    public class PocketGearBaseLogic : MyGameLogicComponent {
        public const float FORCED_LOWER_LIMIT_DEG = 333.5f;
        public const float FORCED_UPPER_LIMIT_DEG = 360.0f;
        private const float FORCED_LOWER_LIMIT_RAD = (float) (Math.PI * FORCED_LOWER_LIMIT_DEG / 180.0);
        private const float FORCED_UPPER_LIMIT_RAD = (float) (Math.PI * FORCED_UPPER_LIMIT_DEG / 180.0);

        private const string POCKETGEAR_BASE = "MA_PocketGear_Base";
        private const string POCKETGEAR_BASE_LARGE = "MA_PocketGear_L_Base";
        private const string POCKETGEAR_BASE_LARGE_SMALL = "MA_PocketGear_L_Base_sm";
        private const string POCKETGEAR_BASE_SMALL = "MA_PocketGear_Base_sm";
        public static readonly HashSet<string> PocketGearIds = new HashSet<string> { POCKETGEAR_BASE, POCKETGEAR_BASE_LARGE, POCKETGEAR_BASE_LARGE_SMALL, POCKETGEAR_BASE_SMALL };

        private bool _changePocketGearPadState;
        private int _changePocketGearPadStateAfterTicks;
        private bool _isJustPlaced;
        private long _lastKnownTopGridId;
        private bool _manualLock;
        private MatrixD _manualLockBaseMatrix;
        private MatrixD _manualLockTopMatrix;
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
                    PocketGearBaseControls.DeployRetractSwitch.UpdateVisual();
                    Mod.Static.Network.Sync(new PropertySyncMessage { EntityId = Entity.EntityId, Name = nameof(CurrentBehavior), Value = BitConverter.GetBytes((long) value) });
                }
            }
        }

        public float DeployVelocity {
            get { return _settings.DeployVelocity; }
            set {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (value != _settings.DeployVelocity) {
                    _settings.DeployVelocity = value;
                    Mod.Static.Network.Sync(new PropertySyncMessage { EntityId = Entity.EntityId, Name = nameof(DeployVelocity), Value = BitConverter.GetBytes(DeployVelocity) });
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
                    Mod.Static.Network.Sync(new PropertySyncMessage { EntityId = Entity.EntityId, Name = nameof(ShouldDeploy), Value = BitConverter.GetBytes(value) });
                }
            }
        }

        private static IMyLandingGear GetPocketGearPad(IMyMechanicalConnectionBlock stator) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(GetPocketGearPad)) : null) {
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
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(Close)) : null) {
                Mod.Static.Network.UnRegisterEntitySyncHandler(Entity.EntityId, OnEntitySyncMessageReceived);

                _pocketGearBase.LimitReached -= OnLimitReached;
                _pocketGearBase.CubeGrid.OnIsStaticChanged -= OnIsStaticChanged;

                if (Mod.Static.DamageHandler != null) {
                    _pocketGearBase.CubeGrid.OnPhysicsChanged -= OnPhysicsChanged;
                    Mod.Static.DamageHandler.DisableProtection(_pocketGearBase.CubeGrid.EntityId);
                    Mod.Static.DamageHandler.DisableProtection(_lastKnownTopGridId);
                }
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(Init)) : null) {
                Log = Mod.Static.Log.ForScope<PocketGearBaseLogic>();

                _pocketGearBase = Entity as IMyMotorStator;
                _isJustPlaced = _pocketGearBase?.CubeGrid?.Physics != null;
                _settings = Load(Entity, new Guid(PocketGearBaseSettings.GUID));

                Mod.Static.Network.RegisterEntitySyncHandler(Entity.EntityId, OnEntitySyncMessageReceived);

                if (Entity.Storage == null) {
                    Entity.Storage = new MyModStorageComponent();
                }

                if (!PocketGearBaseControls.AreTerminalControlsInitialized) {
                    PocketGearBaseControls.InitializeTerminalControls();
                }

                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override bool IsSerialized() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(Save)) : null) {
                Save(_pocketGearBase, new Guid(PocketGearBaseSettings.GUID), _settings);
                return base.IsSerialized();
            }
        }

        public override void UpdateAfterSimulation() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(UpdateAfterSimulation)) : null) {
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
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(UpdateBeforeSimulation)) : null) {
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
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(UpdateOnceBeforeFrame)) : null) {
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
                    _lastKnownTopGridId = _pocketGearBase.TopGrid.EntityId;
                    _pocketGearPad = GetPocketGearPad(_pocketGearBase);

                    if (IsDeploying) {
                        Mod.Static.DamageHandler?.EnableProtection(_pocketGearBase);
                        Mod.Static.DamageHandler?.EnableProtection(_pocketGearBase.Top);
                        Mod.Static.DamageHandler?.EnableProtection(_pocketGearPad);
                    }
                }

                try {
                    PocketGearBaseControls.DeployRetractSwitch.UpdateVisual();
                    PocketGearBaseControls.PlacePocketGearPadButton.UpdateVisual();
                } catch (Exception exception) {
                    Log.Error(exception);
                }

                _pocketGearBase.LimitReached += OnLimitReached;
                _pocketGearBase.CubeGrid.OnIsStaticChanged += OnIsStaticChanged;

                if (Mod.Static.DamageHandler != null) {
                    // hack: use this to check if top is detached until the IMyMotorStator.AttachedEntityChanged bug is fixed.
                    _pocketGearBase.CubeGrid.OnPhysicsChanged += OnPhysicsChanged;
                }
            }
        }

        public void AttachedEntityChanged(IMyEntity entity) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(AttachedEntityChanged)) : null) {
                if (entity != null) {
                    if (IsDeploying) {
                        Mod.Static.DamageHandler?.EnableProtection(_pocketGearBase);
                        Mod.Static.DamageHandler?.EnableProtection(_pocketGearBase.Top);
                        Mod.Static.DamageHandler?.EnableProtection(_pocketGearPad);
                    }

                    _lastKnownTopGridId = _pocketGearBase.TopGrid.EntityId;
                    if (_pocketGearPad == null) {
                        _pocketGearPad = GetPocketGearPad(_pocketGearBase);
                    }
                } else {
                    Mod.Static.DamageHandler?.DisableProtection(_lastKnownTopGridId);
                    _pocketGearPad = null;
                }

                PocketGearBaseControls.DeployRetractSwitch.UpdateVisual();
                PocketGearBaseControls.PlacePocketGearPadButton.UpdateVisual();
            }
        }

        public void ManualRotorLock() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(ManualRotorLock)) : null) {
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

        public void SwitchDeployState(bool deploy) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(SwitchDeployState)) : null) {
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
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(ChangePocketGearPadState)) : null) {
                if (_pocketGearPad != null) {
                    _pocketGearPad.Enabled = deployed;
                }
            }
        }

        private void ChangePocketGearPadStateAfterTicks(int ticks) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(ChangePocketGearPadStateAfterTicks)) : null) {
                _changePocketGearPadState = true;
                _changePocketGearPadStateAfterTicks = ticks;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        private PocketGearBaseSettings Load(IMyEntity entity, Guid guid) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(ChangePocketGearPadStateAfterTicks)) : null) {
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

        private void OnEntitySyncMessageReceived(IEntitySyncMessage message) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(OnEntitySyncMessageReceived)) : null) {
                using (Log.BeginMethod(nameof(OnEntitySyncMessageReceived))) {
                    var syncMessage = message as PropertySyncMessage;
                    if (syncMessage != null) {
                        switch (syncMessage.Name) {
                            case nameof(DeployVelocity):
                                _settings.DeployVelocity = BitConverter.ToSingle(syncMessage.Value, 0);
                                PocketGearBaseControls.DeployVelocitySlider.UpdateVisual();
                                break;
                            case nameof(CurrentBehavior):
                                _settings.LockRetractBehavior = (LockRetractBehaviors) BitConverter.ToInt64(syncMessage.Value, 0);
                                PocketGearBaseControls.LockRetractBehaviorCombobox.UpdateVisual();
                                PocketGearBaseControls.DeployRetractSwitch.UpdateVisual();
                                break;
                            case nameof(ShouldDeploy):
                                _settings.ShouldDeploy = BitConverter.ToBoolean(syncMessage.Value, 0);
                                PocketGearBaseControls.DeployRetractSwitch.UpdateVisual();
                                break;
                        }
                    }
                }
            }
        }

        private void OnIsStaticChanged(IMyCubeGrid cubeGrid, bool isStatic) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(OnIsStaticChanged)) : null) {
                ManualRotorLock();
            }
        }

        private void OnLimitReached(bool upperLimit) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(OnLimitReached)) : null) {
                ChangePocketGearPadState(upperLimit);
                if (upperLimit) {
                    Mod.Static.DamageHandler?.EnableProtection(_pocketGearBase);
                    Mod.Static.DamageHandler?.EnableProtection(_pocketGearBase.Top);
                    Mod.Static.DamageHandler?.EnableProtection(_pocketGearPad);
                } else {
                    Mod.Static.DamageHandler?.DisableProtection(_pocketGearBase);
                    Mod.Static.DamageHandler?.DisableProtection(_pocketGearBase.Top);
                    Mod.Static.DamageHandler?.DisableProtection(_pocketGearPad);
                }
            }
        }

        private void OnPhysicsChanged(IMyEntity entity) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(OnPhysicsChanged)) : null) {
                if (entity.Physics != null) {
                    AttachedEntityChanged(_pocketGearBase.TopGrid == null ? null : _pocketGearBase.Top);
                }
            }
        }

        private void Retract() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(Retract)) : null) {
                if (!CanRetract) {
                    return;
                }

                ShouldDeploy = false;
                if (CurrentBehavior == LockRetractBehaviors.UnlockOnRetract) {
                    PocketGearPadLogic.Unlock(_pocketGearPad);
                }

                _pocketGearBase.TargetVelocityRPM = DeployVelocity * -1;
            }
        }

        private void Save(IMyEntity entity, Guid guid, PocketGearBaseSettings settings) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(Save)) : null) {
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