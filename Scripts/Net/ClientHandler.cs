using AutoMcD.PocketGear.Net.Messages;
using Sisk.Utils.Logging;
using Sisk.Utils.Net;

namespace AutoMcD.PocketGear.Net {
    public class ClientHandler : NetworkHandlerBase {
        public ClientHandler(ILogger log, Network network) : base(log.ForScope<ClientHandler>(), network) {
            Network.Register<SettingsResponseMessage>(OnSettingsResponseMessage);
        }

        /// <inheritdoc />
        public override void Close() {
            Network.Unregister<SettingsResponseMessage>(OnSettingsResponseMessage);
            base.Close();
        }

        /// <summary>
        ///     Settings received message handler.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The message.</param>
        private void OnSettingsResponseMessage(ulong sender, SettingsResponseMessage message) {
            if (message.Settings != null) {
                Mod.Static.OnSettingsReceived(message.Settings);
            }
        }
    }
}