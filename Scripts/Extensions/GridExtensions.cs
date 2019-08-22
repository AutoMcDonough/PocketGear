using Sandbox.Game.Entities;
using VRage.Game.ModAPI;

namespace Sisk.PocketGear.Extensions {
    public static class GridExtensions {
        /// <summary>
        ///     Check if the given cube grid is projected or a preview.
        /// </summary>
        /// <param name="cubeGrid">The cube grid to check.</param>
        /// <returns>Returns true if cube grid is projected or a preview.</returns>
        public static bool IsProjected(this IMyCubeGrid cubeGrid) {
            return (cubeGrid as MyCubeGrid).IsProjected();
        }

        /// <summary>
        ///     Check if the given cube grid is projected or a preview.
        /// </summary>
        /// <param name="cubeGrid">The cube grid to check.</param>
        /// <returns>Returns true if cube grid is projected or a preview.</returns>
        public static bool IsProjected(this MyCubeGrid cubeGrid) {
            return cubeGrid.IsPreview || cubeGrid.Projector != null;
        }
    }
}