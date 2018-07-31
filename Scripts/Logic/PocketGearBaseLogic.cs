using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMcD.PocketGear.Net;
using AutoMcD.PocketGear.Settings;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Logging;
using Sisk.Utils.Profiler;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

// ReSharper disable MergeCastWithTypeCheck
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable InlineOutVariableDeclaration

namespace AutoMcD.PocketGear.Logic {
    // bug: IMyMotorStator.AttachedEntityChanged throws "Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type.". Once this is solved i should use this to desable damage protection in with this.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorAdvancedStator), false, POCKETGEAR_BASE, POCKETGEAR_BASE_LARGE, POCKETGEAR_BASE_LARGE_SMALL, POCKETGEAR_BASE_SMALL)]
    public class PocketGearBaseLogic : MyGameLogicComponent {
        private const float FORCED_LOWER_LIMIT_DEG = 333.5f;
        private const float FORCED_UPPER_LIMIT_DEG = 360.0f;
        private const string POCKETGEAR_BASE = "MA_PocketGear_Base";
        private const string POCKETGEAR_BASE_LARGE = "MA_PocketGear_L_Base";
        private const string POCKETGEAR_BASE_LARGE_SMALL = "MA_PocketGear_L_Base_sm";
        private const string POCKETGEAR_BASE_SMALL = "MA_PocketGear_Base_sm";
        private static readonly HashSet<string> HiddenActions = new HashSet<string> { "Add Small Top Part", "IncreaseLowerLimit", "DecreaseLowerLimit", "ResetLowerLimit", "IncreaseUpperLimit", "DecreaseUpperLimit", "ResetUpperLimit", "IncreaseDisplacement", "DecreaseDisplacement", "ResetDisplacement", "RotorLock", "Reverse", "IncreaseVelocity", "DecreaseVelocity", "ResetVelocity" };
        private static readonly HashSet<string> HiddenControls = new HashSet<string> { "Add Small Top Part", "LowerLimit", "UpperLimit", "Displacement", "RotorLock", "Reverse", "Velocity" };
        public static readonly HashSet<string> PocketGearIds = new HashSet<string> { POCKETGEAR_BASE, POCKETGEAR_BASE_LARGE, POCKETGEAR_BASE_LARGE_SMALL, POCKETGEAR_BASE_SMALL };

        private static IMyTerminalControlButton _createNewPadButton;

        private static IMyTerminalControlSlider _deployVelocitySlider;
        private static IMyTerminalControlCombobox _lockRetractBehaviorCombobox;
        private static IMyTerminalControlOnOffSwitch _switchDeployStateSwitch;
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

        private static bool AreTerminalControlsInitialized { get; set; }

        public bool CanPocketGearBeBuilt => _pocketGearBase.Top != null && _pocketGearPad == null;

        public bool CanRetract {
            get {
                if (_pocketGearPad == null) {
                    _pocketGearPad = GetPocketGearPad(_pocketGearBase);
                }

                return _pocketGearBase.IsWorking && _pocketGearPad != null && (!_pocketGearPad.IsLocked || LockRetractBehavior != LockRetractBehaviors.PreventRetract);
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

        public LockRetractBehaviors LockRetractBehavior {
            get { return _settings.LockRetractBehavior; }
            set {
                if (value != _settings.LockRetractBehavior) {
                    _settings.LockRetractBehavior = value;
                    _switchDeployStateSwitch.UpdateVisual();
                    Mod.Static.Network.Sync(new PropertySyncMessage { EntityId = Entity.EntityId, Name = nameof(LockRetractBehavior), Value = BitConverter.GetBytes((long) value) });
                }
            }
        }

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

        public static string DisplayName(string name) {
            return Regex.Replace(name, "[a-z][A-Z]", m => $"{m.Value[0]} {m.Value[1]}");
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

        private static void InitializeTerminalControls() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(InitializeTerminalControls)) : null) {
                if (AreTerminalControlsInitialized) {
                    return;
                }

                AreTerminalControlsInitialized = true;

                List<IMyTerminalControl> defaultControls;
                List<IMyTerminalAction> defaultActions;
                MyAPIGateway.TerminalControls.GetControls<IMyMotorAdvancedStator>(out defaultControls);
                MyAPIGateway.TerminalControls.GetActions<IMyMotorAdvancedStator>(out defaultActions);

                foreach (var control in defaultControls) {
                    if (HiddenControls.Contains(control.Id)) {
                        var visible = control.Visible;
                        var enabled = control.Enabled;
                        control.Visible = block => !PocketGearIds.Contains(block.BlockDefinition.SubtypeId) && visible.Invoke(block);
                        control.Enabled = block => !PocketGearIds.Contains(block.BlockDefinition.SubtypeId) && enabled.Invoke(block);

                        if (control.Id == "LowerLimit" && control is IMyTerminalControlSlider) {
                            var slider = control as IMyTerminalControlSlider;
                            var getter = slider.Getter;
                            var setter = slider.Setter;
                            slider.Getter = block => PocketGearIds.Contains(block.BlockDefinition.SubtypeId) ? FORCED_LOWER_LIMIT_DEG : getter.Invoke(block);
                            slider.Setter = (block, value) => { setter.Invoke(block, PocketGearIds.Contains(block.BlockDefinition.SubtypeId) ? FORCED_LOWER_LIMIT_DEG : value); };
                        }

                        if (control.Id == "UpperLimit" && control is IMyTerminalControlSlider) {
                            var slider = control as IMyTerminalControlSlider;
                            var getter = slider.Getter;
                            var setter = slider.Setter;
                            slider.Getter = block => PocketGearIds.Contains(block.BlockDefinition.SubtypeId) ? FORCED_UPPER_LIMIT_DEG : getter.Invoke(block);
                            slider.Setter = (block, value) => { setter.Invoke(block, PocketGearIds.Contains(block.BlockDefinition.SubtypeId) ? FORCED_UPPER_LIMIT_DEG : value); };
                        }
                    }
                }

                foreach (var action in defaultActions) {
                    if (HiddenActions.Contains(action.Id)) {
                        var original = action.Enabled;
                        action.Enabled = block => !PocketGearIds.Contains(block.BlockDefinition.SubtypeId) && original.Invoke(block);
                    }
                }

                var controls = new List<IMyTerminalControl>();
                _deployVelocitySlider = TerminalControlUtils.CreateSlider<IMyMotorAdvancedStator>(
                    DisplayName(nameof(DeployVelocity)),
                    tooltip: "The speed at which the PocketGear is retracted / extended.",
                    writer: (block, builder) => builder.Append($"{block.GameLogic?.GetAs<PocketGearBaseLogic>()?.DeployVelocity:N2} rpm"),
                    getter: block => block.GameLogic?.GetAs<PocketGearBaseLogic>()?.DeployVelocity ?? 0,
                    setter: (block, value) => {
                        var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
                        if (logic != null) {
                            logic.DeployVelocity = value;
                        }
                    },
                    min: block => 0,
                    max: block => (block as IMyMotorAdvancedStator)?.MaxRotorAngularVelocity * 9.549296f ?? 1,
                    enabled: block => PocketGearIds.Contains(block.BlockDefinition.SubtypeId),
                    visible: block => PocketGearIds.Contains(block.BlockDefinition.SubtypeId),
                    supportsMultipleBlocks: true
                );
                controls.Add(_deployVelocitySlider);

                _lockRetractBehaviorCombobox = TerminalControlUtils.CreateCombobox<IMyMotorAdvancedStator>(
                    DisplayName(nameof(LockRetractBehavior)),
                    tooltip: "Whether it should prevent retracting if locked or if it should unlock on retract.",
                    content: list => list.AddRange(Enum.GetValues(typeof(LockRetractBehaviors)).Cast<LockRetractBehaviors>().Select(x => new MyTerminalControlComboBoxItem { Key = (long) x, Value = MyStringId.GetOrCompute(DisplayName(x.ToString())) })),
                    getter: block => (long) (block.GameLogic?.GetAs<PocketGearBaseLogic>()?.LockRetractBehavior ?? LockRetractBehaviors.PreventRetract),
                    setter: (block, value) => {
                        var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
                        if (logic != null) {
                            logic.LockRetractBehavior = (LockRetractBehaviors) value;
                        }
                    },
                    enabled: block => PocketGearIds.Contains(block.BlockDefinition.SubtypeId),
                    visible: block => PocketGearIds.Contains(block.BlockDefinition.SubtypeId),
                    supportsMultipleBlocks: true
                );
                controls.Add(_lockRetractBehaviorCombobox);

                _createNewPadButton = TerminalControlUtils.CreateButton<IMyMotorAdvancedStator>(
                    DisplayName(nameof(PlaceLandingPad)),
                    tooltip: "Place a new PocketGear pad.",
                    action: PlaceLandingPad,
                    enabled: block => {
                        if (!PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                            return false;
                        }

                        var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
                        var enabled = false;
                        if (logic != null) {
                            enabled = logic.CanPocketGearBeBuilt;
                        }

                        return enabled;
                    },
                    visible: block => PocketGearIds.Contains(block.BlockDefinition.SubtypeId),
                    supportsMultipleBlocks: true
                );
                controls.Add(_createNewPadButton);

                _switchDeployStateSwitch = TerminalControlUtils.CreateOnOffSwitch<IMyMotorAdvancedStator>(
                    DisplayName(nameof(SwitchDeployState)),
                    tooltip: "Switch between deploy and retract.",
                    onText: "Deploy",
                    offText: "Retract",
                    getter: block => block.GameLogic.GetAs<PocketGearBaseLogic>().IsDeploying,
                    setter: (block, value) => block?.GameLogic?.GetAs<PocketGearBaseLogic>()?.SwitchDeployState(value),
                    enabled: block => {
                        if (!PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                            return false;
                        }

                        var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
                        var enabled = false;
                        if (logic != null) {
                            enabled = logic.CanRetract;
                        }

                        return enabled;
                    },
                    visible: block => PocketGearIds.Contains(block.BlockDefinition.SubtypeId),
                    supportsMultipleBlocks: true
                );
                controls.Add(_switchDeployStateSwitch);

                TerminalControlUtils.RegisterControls<IMyMotorAdvancedStator>(controls);
            }
        }

        private static void PlaceLandingPad(IMyMotorAdvancedStator stator) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(PlaceLandingPad)) : null) {
                var logic = stator?.GameLogic?.GetAs<PocketGearBaseLogic>();

                var pad = logic?._pocketGearPad;
                if (pad != null) {
                    return;
                }

                var top = logic?._pocketGearBase.Top;
                if (top == null) {
                    return;
                }

                top.GameLogic.GetAs<PocketGearPartLogic>()?.PlaceLandingPad();
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

                if (!AreTerminalControlsInitialized) {
                    InitializeTerminalControls();
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

                _pocketGearBase.LowerLimitDeg = FORCED_LOWER_LIMIT_DEG;
                _pocketGearBase.UpperLimitDeg = FORCED_UPPER_LIMIT_DEG;

                if (_pocketGearBase.TopGrid != null) {
                    _lastKnownTopGridId = _pocketGearBase.TopGrid.EntityId;
                    _pocketGearPad = GetPocketGearPad(_pocketGearBase);

                    if (IsDeploying) {
                        Mod.Static.DamageHandler?.EnableProtection(_pocketGearBase);
                        Mod.Static.DamageHandler?.EnableProtection(_pocketGearBase.Top);
                        Mod.Static.DamageHandler?.EnableProtection(_pocketGearPad);
                    }
                }

                _switchDeployStateSwitch.UpdateVisual();
                _createNewPadButton.UpdateVisual();

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
                    _pocketGearPad = GetPocketGearPad(_pocketGearBase);
                    _switchDeployStateSwitch.UpdateVisual();
                    _createNewPadButton.UpdateVisual();
                    Log.Debug($"AttachedEntityChanged => top: {_pocketGearBase.Top != null} | pad: {_pocketGearPad != null}");
                } else {
                    Mod.Static.DamageHandler?.DisableProtection(_lastKnownTopGridId);
                    _pocketGearPad = null;
                    _switchDeployStateSwitch.UpdateVisual();
                    _createNewPadButton.UpdateVisual();
                    Log.Debug($"AttachedEntityChanged => top: {_pocketGearBase.Top != null} | pad: {_pocketGearPad != null}");
                }
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
                    if (message is PropertySyncMessage) {
                        var syncMessage = (PropertySyncMessage) message;
                        switch (syncMessage.Name) {
                            case nameof(DeployVelocity):
                                _settings.DeployVelocity = BitConverter.ToSingle(syncMessage.Value, 0);
                                _deployVelocitySlider.UpdateVisual();
                                break;
                            case nameof(LockRetractBehavior):
                                _settings.LockRetractBehavior = (LockRetractBehaviors) BitConverter.ToInt64(syncMessage.Value, 0);
                                _lockRetractBehaviorCombobox.UpdateVisual();
                                _switchDeployStateSwitch.UpdateVisual();
                                break;
                            case nameof(ShouldDeploy):
                                _settings.ShouldDeploy = BitConverter.ToBoolean(syncMessage.Value, 0);
                                _switchDeployStateSwitch.UpdateVisual();
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
                if (LockRetractBehavior == LockRetractBehaviors.UnlockOnRetract) {
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

        private void SwitchDeployState(bool deploy) {
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
    }
}