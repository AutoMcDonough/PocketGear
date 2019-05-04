using System;
using Sisk.PocketGear.Net.Messages;
using Sisk.Utils.Logging;
using Sisk.Utils.Net;

namespace Sisk.PocketGear.Net {
    public class ServerHandler : NetworkHandlerBase {
        public ServerHandler(ILogger log, Network network) : base(log.ForScope<ClientHandler>(), network) {
            Network.Register<SettingsRequestMessage>(OnSettingsRequestMessage);
        }

        public override void Close() {
            Network.Unregister<SettingsRequestMessage>(OnSettingsRequestMessage);
            base.Close();
        }

        /// <summary>
        ///     Request Settings message handler.
        /// </summary>
        /// <param name="sender">The sender who requested settings.</param>
        /// <param name="message">The message from the requester.</param>
        private void OnSettingsRequestMessage(ulong sender, SettingsRequestMessage message) {
            if (Mod.Static.Settings == null) {
                return;
            }

            try {
                var response = new SettingsResponseMessage {
                    Settings = Mod.Static.Settings,
                    SteamId = sender
                };

                Network.Send(response, sender);
            } catch (Exception exception) {
                using (Log.BeginMethod(nameof(OnSettingsRequestMessage))) {
                    Log.Error(exception);
                }
            }
        }
    }
}