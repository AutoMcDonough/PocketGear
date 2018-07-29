using System;
using System.Collections.Generic;
using System.Linq;
using AutoMcD.PocketGear.Logic;
using AutoMcD.PocketGear.Net;
using AutoMcD.PocketGear.Settings;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sisk.Utils.Logging;
using Sisk.Utils.Logging.DefaultHandler;
using Sisk.Utils.Profiler;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace AutoMcD.PocketGear {
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Mod : MySessionComponentBase {
        public const string NAME = "PocketGears";

        // important: set profile to false before publishing this mod.
        public const bool PROFILE = true;

        // important: change to info or none before publishing this mod.
        private const LogEventLevel DEFAULT_LOG_EVENT_LEVEL = LogEventLevel.All;

        private const string LOG_FILE_TEMPLATE = "{0}.log";

        // important: change to an unused mod and network id.
        private const ushort MOD_ID = 51500;
        private const ushort NETWORK_ID = 51500;
        private const string PROFILER_LOG_FILE = "profiler.log";
        private const string PROFILER_SUMMARY_FILE = "profiler_summary.txt";
        private const string SETTINGS_FILE = "settings.xml";
        private static readonly string LogFile = string.Format(LOG_FILE_TEMPLATE, NAME);
        private ILogger _profilerLog;

        public Mod() {
            Static = this;
            InitializeLogging();
        }

        /// <summary>
        ///     Handles impact damage for PocketGears.
        /// </summary>
        public DamageHandler DamageHandler { get; private set; }

        /// <summary>
        ///     Indicates if the mod is used on an client.
        /// </summary>
        public bool IsClient { get; set; }

        /// <summary>
        ///     Logger used for logging.
        /// </summary>
        public ILogger Log { get; private set; }

        /// <summary>
        ///     Network to handle sycing.
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
                    foreach (var result in Profiler.Results.OrderBy(x => x.Avg)) {
                        writer.WriteLine(result);
                    }
                }
            }
        }

        public override void HandleInput() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(HandleInput)) : null) {
                if (!IsClient || MyAPIGateway.Gui.ChatEntryVisible || MyAPIGateway.Gui.IsCursorVisible) {
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
        ///     Initialize the session component.
        /// </summary>
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent) {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(Init)) : null) {
                using (Log.BeginMethod(nameof(Init))) {
                    base.Init(sessionComponent);

                    InitializeNetwork();
                    InitializeDamageHandler();
                }
            }
        }

        /// <summary>
        ///     Load mod settings and localize mod definitions.
        /// </summary>
        public override void LoadData() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(LoadData)) : null) {
                LoadSettings();
            }
        }

        /// <summary>
        ///     Save mod settings and fire OnSave event.
        /// </summary>
        // bug?: SaveData is called after the game save. You can use GetObjectBuilder.
        public override void SaveData() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(SaveData)) : null) {
                using (Log.BeginMethod(nameof(SaveData))) {
                    SaveSettings();
                    Log.Flush();
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

            if (Network != null) {
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

        private void InitializeDamageHandler() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(InitializeDamageHandler)) : null) {
                if (Settings.UseImpactDamageHandler) {
                    DamageHandler = new DamageHandler();
                    DamageHandler.Init();
                }
            }
        }

        /// <summary>
        ///     Initalize the logging system.
        /// </summary>
        private void InitializeLogging() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(InitializeLogging)) : null) {
                Log = Logger.ForScope<Mod>();
                Log.Register(new LocalStorageHandler(LogFile, LogFormatter, DEFAULT_LOG_EVENT_LEVEL, PROFILE ? -1 : 500));

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
        ///     Initalize the network system.
        /// </summary>
        private void InitializeNetwork() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(InitializeNetwork)) : null) {
                using (Log.BeginMethod(nameof(InitializeNetwork))) {
                    Log.Info("Initialize Network");
                    Network = new Network(NETWORK_ID);
                    IsClient = !(Network.IsServer && Network.IsDedicated);
                    Log.Info($"IsClient {IsClient}, IsServer: {Network.IsServer}, IsDedicated: {Network.IsDedicated}");
                    Log.Info("Network initialized");
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
                    }

                    Settings = settings;
                    Log.Info("Settings loaded");
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