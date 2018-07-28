using System;
using System.Linq;
using AutoMcD.PocketGear.Net;
using Sandbox.ModAPI;
using Sisk.Utils.Logging;
using Sisk.Utils.Logging.DefaultHandler;
using Sisk.Utils.Profiler;
using VRage.Game;
using VRage.Game.Components;

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
        private static readonly string LogFile = string.Format(LOG_FILE_TEMPLATE, NAME);
        private ILogger _profilerLog;

        public Mod() {
            Static = this;
            InitializeLogging();
        }

        /// <summary>
        ///     Indicates if the mod is used on an client.
        /// </summary>
        public bool IsPlayer { get; set; }

        /// <summary>
        ///     Logger used for logging.
        /// </summary>
        public ILogger Log { get; private set; }

        /// <summary>
        ///     Network to handle sycing.
        /// </summary>
        public Network Network { get; private set; }

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

        /// <summary>
        ///     Initialize the session component.
        /// </summary>
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent) {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(Init)) : null) {
                using (Log.BeginMethod(nameof(Init))) {
                    base.Init(sessionComponent);

                    InitializeNetwork();
                    IsPlayer = !(Network.IsServer && Network.IsDedicated);
                    // todo: we should fix the real issue with the pocketgear if possible.
                    //MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, OnDamage);
                }
            }
        }

        /// <summary>
        ///     Load mod settings and localize mod definitions.
        /// </summary>
        public override void LoadData() { }

        /// <summary>
        ///     Save mod settings and fire OnSave event.
        /// </summary>
        // bug?: SaveData is called after the game save. You can use GetObjectBuilder.
        public override void SaveData() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(SaveData)) : null) {
                using (Log.BeginMethod(nameof(SaveData))) {
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

        private void InitializeNetwork() {
            using (PROFILE ? Profiler.Measure(nameof(Mod), nameof(InitializeNetwork)) : null) {
                using (Log.BeginMethod(nameof(InitializeNetwork))) {
                    Log.Info("Initialize Network");
                    Network = new Network(NETWORK_ID);
                    Log.Info($"IsServer: {Network.IsServer}, IsDedicated: {Network.IsDedicated}");
                    Log.Info("Network initialized");
                }
            }
        }
    }
}