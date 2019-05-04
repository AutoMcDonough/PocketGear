using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMcD.PocketGear.DamageSystem;
using AutoMcD.PocketGear.Net;
using AutoMcD.PocketGear.Net.Messages;
using AutoMcD.PocketGear.Settings;
using AutoMcD.PocketGear.TerminalControls;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sisk.Utils.Logging;
using Sisk.Utils.Logging.DefaultHandler;
using Sisk.Utils.Net;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;

namespace AutoMcD.PocketGear {
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Mod : MySessionComponentBase {
        public const string NAME = "PocketGears";

        private const LogEventLevel DEFAULT_LOG_EVENT_LEVEL = LogEventLevel.Info | LogEventLevel.Warning | LogEventLevel.Error;
        private const string LOG_FILE_TEMPLATE = "{0}.log";
        private const ushort NETWORK_ID = 51510;
        private const string SETTINGS_FILE = "settings.xml";
        private static readonly string LogFile = string.Format(LOG_FILE_TEMPLATE, NAME);
        private NetworkHandlerBase _networkHandler;

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
        ///     Language used to localize this mod.
        /// </summary>
        public MyLanguagesEnum? Language { get; private set; }

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

        /// <summary>
        ///     Handle input to switch lock of pocket gear pads on 'p' press.
        /// </summary>
        public override void HandleInput() {
            if (Network != null && Network.IsDedicated || MyAPIGateway.Gui.ChatEntryVisible || MyAPIGateway.Gui.IsCursorVisible) {
                return;
            }

            if (!(MyAPIGateway.Session.ControlledObject is IMyShipController)) {
                return;
            }

            var controller = (IMyShipController)MyAPIGateway.Session.ControlledObject;
            if (MyAPIGateway.Input.IsNewGameControlPressed(MyControlsSpace.LANDING_GEAR)) {
                var cubeGrid = controller.CubeGrid;
                var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
                var pads = new List<IMyLandingGear>();
                gts.GetBlocksOfType(pads, x => Defs.Pad.Ids.Contains(x.BlockDefinition.SubtypeId));

                foreach (var pad in pads) {
                    if (!controller.HandBrake) {
                        pad.Unlock();
                    } else {
                        pad.Lock();
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent) {
            base.Init(sessionComponent);

            if (Network == null || Network.IsServer) {
                InitializeDamageHandler();
            }
        }

        /// <summary>
        ///     Load mod settings and localize mod definitions.
        /// </summary>
        public override void LoadData() {
            InitializeLogging();
            LoadLocalization();

            MyAPIGateway.Gui.GuiControlRemoved += OnGuiControlRemoved;

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

            Controls = new Controls();
        }

        /// <summary>
        ///     Save mod settings and fire OnSave event.
        /// </summary>
        public override void SaveData() {
            Log.Flush();
        }

        /// <summary>
        ///     Unloads all data.
        /// </summary>
        protected override void UnloadData() {
            Log?.EnterMethod(nameof(UnloadData));
            MyAPIGateway.Gui.GuiControlRemoved -= OnGuiControlRemoved;

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
            if (Settings.UseImpactDamageHandler) {
                DamageHandler = new DamageHandler();
            }
        }

        /// <summary>
        ///     Initialize the logging system.
        /// </summary>
        private void InitializeLogging() {
            Log = Logger.ForScope<Mod>();
            Log.Register(new LocalStorageHandler(LogFile, LogFormatter, IsDevVersion ? LogEventLevel.All : DEFAULT_LOG_EVENT_LEVEL, 0));

            using (Log.BeginMethod(nameof(InitializeLogging))) {
                Log.Info("Logging initialized");
            }
        }

        /// <summary>
        ///     Initialize the network system.
        /// </summary>
        private void InitializeNetwork() {
            using (Log.BeginMethod(nameof(InitializeNetwork))) {
                Log.Info("Initialize Network");
                Network = new Network(NETWORK_ID);
                Log.Info($"IsClient {Network.IsClient}, IsServer: {Network.IsServer}, IsDedicated: {Network.IsDedicated}");
                Log.Info("Network initialized");
            }
        }

        /// <summary>
        ///     Load localizations for this mod.
        /// </summary>
        private void LoadLocalization() {
            using (Log.BeginMethod(nameof(LoadLocalization))) {
                var path = Path.Combine(ModContext.ModPathData, "Localization");
                var supportedLanguages = new HashSet<MyLanguagesEnum>();
                MyTexts.LoadSupportedLanguages(path, supportedLanguages);

                Log.Debug($"Localization path: {path}");
                Log.Debug($"Supported Languages: {string.Join(", ", supportedLanguages)}");
                var currentLanguage = supportedLanguages.Contains(MyAPIGateway.Session.Config.Language) ? MyAPIGateway.Session.Config.Language : MyLanguagesEnum.English;
                if (Language != null && Language == currentLanguage) {
                    return;
                }

                Language = currentLanguage;
                var languageDescription = MyTexts.Languages.Where(x => x.Key == currentLanguage).Select(x => x.Value).FirstOrDefault();
                if (languageDescription != null) {
                    var cultureName = string.IsNullOrWhiteSpace(languageDescription.CultureName) ? null : languageDescription.CultureName;
                    var subcultureName = string.IsNullOrWhiteSpace(languageDescription.SubcultureName) ? null : languageDescription.SubcultureName;

                    MyTexts.LoadTexts(path, cultureName, subcultureName);
                }
            }
        }

        /// <summary>
        ///     Save mod settings.
        /// </summary>
        private void LoadSettings() {
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

        /// <summary>
        ///     Event triggered on gui control removed.
        ///     Used to detect if Option screen is closed and then to reload localization.
        /// </summary>
        /// <param name="obj"></param>
        private void OnGuiControlRemoved(object obj) {
            if (obj.ToString().EndsWith("ScreenOptionsSpace")) {
                LoadLocalization();
            }
        }

        /// <summary>
        ///     Load mod settings.
        /// </summary>
        private void SaveSettings() {
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