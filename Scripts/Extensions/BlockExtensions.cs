using VRage.Game.ModAPI;

namespace Sisk.PocketGear.Extensions {
    public static class BlockExtensions {
        /// <summary>
        ///     Checks if given slim block is projected or a preview.
        /// </summary>
        /// <param name="slimBlock">The given slim block to check.</param>
        /// <returns>Return true if if slim block is projected or a preview.</returns>
        public static bool IsProjected(this IMySlimBlock slimBlock) {
            return slimBlock.CubeGrid.IsProjected();
        }

        /// <summary>
        ///     Checks if given cube block is projected or a preview.
        /// </summary>
        /// <param name="cubeBlock">The given cube block to check.</param>
        /// <returns>Return true if if cube block is projected or a preview.</returns>
        public static bool IsProjected(this IMyCubeBlock cubeBlock) {
            return cubeBlock.CubeGrid.IsProjected();
        }
    }
}