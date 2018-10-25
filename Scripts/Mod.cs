using System;
using System.Collections.Generic;
using System.Linq;
using AutoMcD.PocketGear.DamageSystem;
using AutoMcD.PocketGear.Localization;
using AutoMcD.PocketGear.Logic;
using AutoMcD.PocketGear.Net;
using AutoMcD.PocketGear.Net.Messages;
using AutoMcD.PocketGear.Settings;
using AutoMcD.PocketGear.TerminalControls;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sisk.Utils.Localization;
using Sisk.Utils.Logging;
using Sisk.Utils.Logging.DefaultHandler;
using Sisk.Utils.Net;
using Sisk.Utils.Profiler;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace AutoMcD.PocketGear {
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Mod : MySessionComponentBase {
        public const string NAME = "PocketGears";

        // important: set profile to false before publishing this mod.
        public const bool PROFILE = true;

        private const LogEventLevel DEFAULT_LOG_EVENT_LEVEL = LogEventLevel.Info | LogEventLevel.Warning | LogEventLevel.Error;
        private const string LOG_FILE_TEMPLATE = "{0}.log";
        private const ushort NETWORK_ID = 51510;
        private const string PROFILER_LOG_FILE = "profiler.log";
        private const string PROFILER_SUMMARY_FILE = "profiler_summary.txt";
        private const string SETTINGS_FILE = "settings.xml";
        private static readonly string LogFile = string.Format(LOG_FILE_TEMPLATE, NAME);
        private NetworkHandlerBase _networkHandler;
        private ILogger _profilerLog;

        public Mod() {
            Static = this;
        }

        /// <summary>
        ///     Terminal Controls, Actions and Properties
        /// </summary>
        public Controls Controls { get; private set; }

        /// <summary>
        ///     Handles impact damage for PocketGears.
        /// </summary>
        public DamageHandler DamageHandler { get; private set; }

        /// <summary>
        ///     Indicates if mod is a dev version.
        /// </summary>
        private bool IsDevVersion => ModContext.ModName.EndsWith("_DEV");

        /// <summary>
        ///     Logger used for logging.
        /// </summary>
        public ILogger Log { get; private set; }

        /// <summary>
        ///     Network to handle syncing.
        /// </summary>
        public Network Network { get; private set; }

        /// <summary>
        ///     The Mod Settings.
        /// </summary>
        public ModSettings Settings { get; private set; }

        /// <summary>
        ///     The static instance.
        /// </summary>
        public static Mod Static { get; private set; }

        private static string LogFormatter(LogEventLevel level, string message, DateTime timestamp, Type scope, string method) {
            return $"[{timestamp:HH:mm:ss:fff}] [{new string(level.ToString().Take(1).ToArray())}] [{scope}->{method}()]: {message}";
        }

        private static void WriteProfileResults() {
            if (Profiler.Results.Any()) {
                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(PROFILER_SUMMARY_FILE, typeof(Mod))) {
                    foreach (var result in Profiler.Results.OrderByDescending(x => x.Total)) {
                        writer.WriteLine(result);
                    }
                }
            }
        }

        /// <summary>
        ///     Initialize terminal controls and damage handler.
        /// </summary>
        public override void BeforeStart() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(BeforeStart)) : null) {
                InitializeTerminalControls();
                if (Network == null || Network.IsServer) {
                    InitializeDamageHandler();
                }
            }
        }

        /// <summary>
        ///     Handle input to switch lock of pocket gear pads on 'p' press.
        /// </summary>
        public override void HandleInput() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(HandleInput)) : null) {
                if (Network != null && Network.IsDedicated || MyAPIGateway.Gui.ChatEntryVisible || MyAPIGateway.Gui.IsCursorVisible) {
                    return;
                }

                if (!(MyAPIGateway.Session.ControlledObject is IMyShipController)) {
                    return;
                }

                var controller = (IMyShipController) MyAPIGateway.Session.ControlledObject;
                if (MyAPIGateway.Input.IsNewGameControlPressed(MyControlsSpace.LANDING_GEAR)) {
                    var cubegrid = controller.CubeGrid;
                    var grids = MyAPIGateway.GridGroups.GetGroup(cubegrid, GridLinkTypeEnum.Mechanical);
                    var pocketGearPads = new List<IMyLandingGear>();
                    foreach (var grid in grids) {
                        var blocks = new List<IMySlimBlock>();
                        grid.GetBlocks(blocks, x => PocketGearPadLogic.PocketGearIds.Contains(x.BlockDefinition.Id.SubtypeId.String));
                        pocketGearPads.AddRange(blocks.Select(x => x.FatBlock).Cast<IMyLandingGear>().Where(x => x.IsWorking));
                    }

                    var isAnyLocked = pocketGearPads.Any(x => x.IsLocked);
                    foreach (var landingGear in pocketGearPads) {
                        if (landingGear.IsLocked == !isAnyLocked) {
                            continue;
                        }

                        PocketGearPadLogic.SwitchLock(landingGear);
                    }
                }
            }
        }

        /// <summary>
        ///     Load mod settings and localize mod definitions.
        /// </summary>
        public override void LoadData() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(LoadData)) : null) {
                InitializeLogging();
                LoadTranslation();
                LocalizeModDefinitions();

                if (MyAPIGateway.Multiplayer.MultiplayerActive) {
                    InitializeNetwork();

                    if (Network != null) {
                        if (Network.IsServer) {
                            LoadSettings();
                            _networkHandler = new ServerHandler(Log, Network);
                        } else {
                            _networkHandler = new ClientHandler(Log, Network);
                            Network.SendToServer(new SettingsRequestMessage());
                        }
                    }
                } else {
                    LoadSettings();
                }
            }
        }

        /// <summary>
        ///     Save mod settings and fire OnSave event.
        /// </summary>
        public override void SaveData() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(SaveData)) : null) {
                Log.Flush();
                if (PROFILE) {
                    _profilerLog.Flush();
                    WriteProfileResults();
                }
            }
        }

        /// <summary>
        ///     Unloads all data.
        /// </summary>
        protected override void UnloadData() {
            Log?.EnterMethod(nameof(UnloadData));

            if (DamageHandler != null) {
                Log?.Info("Stopping damage handler");
                DamageHandler = null;
            }

            if (Controls != null) {
                Log?.Info("Cleaned up terminal controls");
                Controls.Close();
                Controls = null;
            }

            if (Network != null) {
                _networkHandler.Close();
                _networkHandler = null;

                Log?.Info("Cap network connections");
                Network.Close();
                Network = null;
            }

            if (PROFILE) {
                Log?.Info("Writing profiler data");
                WriteProfileResults();
                if (_profilerLog != null) {
                    Log?.Info("Profiler logging stopped");
                    _profilerLog.Flush();
                    _profilerLog.Close();
                    _profilerLog = null;
                }
            }

            if (Log != null) {
                Log.Info("Logging stopped");
                Log.Flush();
                Log.Close();
                Log = null;
            }

            Static = null;
        }

        /// <summary>
        ///     Executed when received a <see cref="SettingsResponseMessage" />.
        /// </summary>
        /// <param name="settings">The setting received from server.</param>
        public void OnSettingsReceived(ModSettings settings) {
            if (settings != null) {
                Settings = settings;

                InitializeDamageHandler();
            }
        }

        private void InitializeDamageHandler() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(InitializeDamageHandler)) : null) {
                if (Settings.UseImpactDamageHandler) {
                    DamageHandler = new DamageHandler();
                    DamageHandler.Init();
                }
            }
        }

        /// <summary>
        ///     Initialize the logging system.
        /// </summary>
        private void InitializeLogging() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(InitializeLogging)) : null) {
                Log = Logger.ForScope<Mod>();
                Log.Register(new LocalStorageHandler(LogFile, LogFormatter, IsDevVersion ? LogEventLevel.All : DEFAULT_LOG_EVENT_LEVEL, PROFILE ? 0 : 500));

                if (PROFILE) {
                    _profilerLog = Logger.ForScope<Mod>();
                    _profilerLog.Register(new LocalStorageHandler(PROFILER_LOG_FILE, (level, message, timestamp, scope, method) => message, LogEventLevel.All, 0));
                    Profiler.SetLogger(_profilerLog.Info);
                }

                using (Log.BeginMethod(nameof(InitializeLogging))) {
                    Log.Info("Logging initialized");
                }
            }
        }

        /// <summary>
        ///     Initialize the network system.
        /// </summary>
        private void InitializeNetwork() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(InitializeNetwork)) : null) {
                using (Log.BeginMethod(nameof(InitializeNetwork))) {
                    Log.Info("Initialize Network");
                    Network = new Network(NETWORK_ID);
                    Log.Info($"IsClient {Network.IsClient}, IsServer: {Network.IsServer}, IsDedicated: {Network.IsDedicated}");
                    Log.Info("Network initialized");
                }
            }
        }

        /// <summary>
        ///     Initialize terminal controls, actions and properties.
        /// </summary>
        private void InitializeTerminalControls() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(InitializeTerminalControls)) : null) {
                using (Log.BeginMethod(nameof(InitializeTerminalControls))) {
                    Log.Info("Initialize Terminal Controls");
                    Controls = new Controls();
                    Log.Info("Terminal Controls initialized");
                }
            }
        }

        /// <summary>
        ///     Save mod settings.
        /// </summary>
        private void LoadSettings() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(LoadSettings)) : null) {
                using (Log.BeginMethod(nameof(LoadSettings))) {
                    ModSettings settings = null;
                    try {
                        if (MyAPIGateway.Utilities.FileExistsInWorldStorage(SETTINGS_FILE, typeof(Mod))) {
                            using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(SETTINGS_FILE, typeof(Mod))) {
                                settings = MyAPIGateway.Utilities.SerializeFromXML<ModSettings>(reader.ReadToEnd());
                                Log.Debug("Loaded setting from world storage");
                            }
                        } else if (MyAPIGateway.Utilities.FileExistsInLocalStorage(SETTINGS_FILE, typeof(Mod))) {
                            using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(SETTINGS_FILE, typeof(Mod))) {
                                settings = MyAPIGateway.Utilities.SerializeFromXML<ModSettings>(reader.ReadToEnd());
                                Log.Debug("Loaded setting from local storage");
                            }
                        }
                    } catch (Exception exception) {
                        Log.Error(exception);
                    }

                    if (settings != null) {
                        if (settings.Version < ModSettings.VERSION) {
                            // todo: merge old and new settings in future versions.
                        }
                    } else {
                        settings = new ModSettings();
                        SaveSettings();
                    }

                    Settings = settings;
                    Log.Info("Settings loaded");
                }
            }
        }

        /// <summary>
        ///     Load translations for this mod.
        /// </summary>
        private void LoadTranslation() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(LoadTranslation)) : null) {
                using (Log.BeginMethod(nameof(LoadTranslation))) {
                    var currentLanguage = MyAPIGateway.Session.Config.Language;
                    var supportedLanguages = new HashSet<MyLanguagesEnum>();

                    switch (currentLanguage) {
                        case MyLanguagesEnum.English:
                            Lang.Add(MyLanguagesEnum.English, new Dictionary<string, string> {
                                { nameof(ModText.DisplayName_PocketGear_Base), "PocketGear Base" },
                                { nameof(ModText.DisplayName_PocketGear_Large_Base), "PocketGear Large Base" },
                                { nameof(ModText.DisplayName_PocketGear_Part), "PocketGear Part" },
                                { nameof(ModText.DisplayName_PocketGear_Large_Part), "PocketGear Large Part" },
                                { nameof(ModText.DisplayName_PocketGear_Pad), "PocketGear Pad" },
                                { nameof(ModText.DisplayName_PocketGear_Large_Pad), "PocketGear Large Pad" },
                                { nameof(ModText.DisplayName_PocketGear_MagLock), "PocketGear MagLock" },
                                { nameof(ModText.DeployVelocity), "Deploy Velocity" },
                                { nameof(ModText.Tooltip_DeployVelocity), "The speed at which the PocketGear is retracted / extended." },
                                { nameof(ModText.LockRetractBehavior), "Lock Retract Behavior" },
                                { nameof(ModText.Tooltip_LockRetractBehavior), "Whether it should prevent retracting if locked or if it should unlock on retract." },
                                { nameof(ModText.PreventRetract), "Prevent Retracting" },
                                { nameof(ModText.UnlockOnRetract), "Unlock on retract" },
                                { nameof(ModText.PlaceLandingPad), "Place Pad" },
                                { nameof(ModText.Tooltip_PlaceLandingPad), "Place a new PocketGear Pad." },
                                { nameof(ModText.SwitchDeployState), "Switch Deploy State" },
                                { nameof(ModText.Tooltip_SwitchDeployState), "Switch between deploy and retract." },
                                { nameof(ModText.Deploy), "Deploy" },
                                { nameof(ModText.Retract), "Retract" }
                            });
                            break;
                        case MyLanguagesEnum.German:
                            Lang.Add(MyLanguagesEnum.German, new Dictionary<string, string> {
                                { nameof(ModText.DisplayName_PocketGear_Base), "PocketGear Basis" },
                                { nameof(ModText.DisplayName_PocketGear_Large_Base), "PocketGear Basis, Groß" },
                                { nameof(ModText.DisplayName_PocketGear_Part), "PocketGear Teil" },
                                { nameof(ModText.DisplayName_PocketGear_Large_Part), "PocketGear Teil, Groß" },
                                { nameof(ModText.DisplayName_PocketGear_Pad), "PocketGear Pad" },
                                { nameof(ModText.DisplayName_PocketGear_Large_Pad), "PocketGear Pad, Groß" },
                                { nameof(ModText.DisplayName_PocketGear_MagLock), "PocketGear MagLock" },
                                { nameof(ModText.DeployVelocity), "Ausfahrgeschwindigkeit" },
                                { nameof(ModText.Tooltip_DeployVelocity), "Die Geschwindigkeit, mit der das PocketGear ein- / ausgefahren wird." },
                                { nameof(ModText.LockRetractBehavior), "Sperr/Einfahr Verhalten" },
                                { nameof(ModText.Tooltip_LockRetractBehavior), "Ob es das Zurückziehen verhindern soll, wenn es gesperrt ist oder ob es beim Zurückziehen entsperrt werden soll." },
                                { nameof(ModText.PreventRetract), "Einfahren Verhindern" },
                                { nameof(ModText.UnlockOnRetract), "Entsperren beim Einfahren" },
                                { nameof(ModText.PlaceLandingPad), "Plaziere Pad" },
                                { nameof(ModText.Tooltip_PlaceLandingPad), "Plaziere ein neues PocketGear Pad" },
                                { nameof(ModText.SwitchDeployState), "Wechsel des Ausfahrstatus" },
                                { nameof(ModText.Tooltip_SwitchDeployState), "Wechsel zwischen aus-/ einfahren." },
                                { nameof(ModText.Deploy), "Ausfahren" },
                                { nameof(ModText.Retract), "Einfahren" }
                            });
                            break;
                    }

                    Texts.LoadSupportedLanguages(supportedLanguages);
                    if (supportedLanguages.Contains(currentLanguage)) {
                        Texts.LoadTexts(currentLanguage);
                        Log.Info($"Loaded {currentLanguage} translations.");
                    } else if (supportedLanguages.Contains(MyLanguagesEnum.English)) {
                        Texts.LoadTexts();
                        Log.Warning($"No {currentLanguage} translations found. Fall back to {MyLanguagesEnum.English} translations.");
                    }
                }
            }
        }

        /// <summary>
        ///     Localize all definitions add by this mod.
        /// </summary>
        private void LocalizeModDefinitions() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(LocalizeModDefinitions)) : null) {
                using (Log.BeginMethod(nameof(LocalizeModDefinitions))) {
                    Log.Info("Adding localizations");
                    var definitions = MyDefinitionManager.Static.GetAllDefinitions().Where(x => x.Context.ModId == ModContext.ModId && x.Context.ModName == ModContext.ModName);

                    foreach (var definition in definitions) {
                        if (definition is MyCubeBlockDefinition) {
                            if (definition.DisplayNameText.StartsWith("DisplayName_")) {
                                Log.Debug($"|-> {definition.Id}");
                                definition.DisplayNameString = Texts.GetString(definition.DisplayNameText);
                                definition.DisplayNameEnum = null;
                            }
                        }
                    }

                    Log.Info("Localizations added");
                }
            }
        }

        /// <summary>
        ///     Load mod settings.
        /// </summary>
        private void SaveSettings() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(SaveSettings)) : null) {
                using (Log.BeginMethod(nameof(SaveSettings))) {
                    if (MyAPIGateway.Utilities.FileExistsInWorldStorage(SETTINGS_FILE, typeof(Mod))) {
                        using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(SETTINGS_FILE, typeof(Mod))) {
                            writer.Write(MyAPIGateway.Utilities.SerializeToXML(Settings));
                            Log.Debug("Saved setting to world storage");
                        }
                    } else {
                        using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(SETTINGS_FILE, typeof(Mod))) {
                            writer.Write(MyAPIGateway.Utilities.SerializeToXML(Settings));
                            Log.Debug("Saved setting to local storage");
                        }
                    }

                    Log.Info("Settings saved");
                }
            }
        }
    }
}