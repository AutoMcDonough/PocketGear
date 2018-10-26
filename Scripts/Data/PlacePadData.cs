using ParallelTasks;
using Sandbox.ModAPI;

namespace AutoMcD.PocketGear.Data {
    /// <summary>
    ///     A <see cref="WorkData" /> type for <see cref="ParallelTasks" />.
    /// </summary>
    public class PlacePadData : WorkData {
        /// <summary>
        ///     Initializes a new instance of <see cref="PlacePadData" /> work data.
        /// </summary>
        /// <param name="head"></param>
        public PlacePadData(IMyAttachableTopBlock head) {
            Head = head;
            Result = PlacePadResult.NotStarted;
        }

        /// <summary>
        ///     The Entity which should place a new hinge.
        /// </summary>
        public IMyAttachableTopBlock Head { get; }

        public PlacePadResult Result { get; set; }
    }
}