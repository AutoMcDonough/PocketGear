using Sisk.Utils.Logging;
using Sisk.Utils.Net;

namespace Sisk.PocketGear.Net {
    public abstract class NetworkHandlerBase {
        protected NetworkHandlerBase(ILogger log, Network network) {
            Log = log;
            Network = network;
        }

        /// <summary>
        ///     Logger used for logging.
        /// </summary>
        protected ILogger Log { get; private set; }

        /// <summary>
        ///     Network to handle syncing.
        /// </summary>
        protected Network Network { get; private set; }

        /// <summary>
        ///     Close the network message handler.
        /// </summary>
        public virtual void Close() {
            if (Network != null) {
                Network = null;
            }

            if (Log != null) {
                Log = null;
            }
        }
    }
}