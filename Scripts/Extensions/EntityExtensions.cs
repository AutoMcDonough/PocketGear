using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace AutoMcD.PocketGear.Extensions {
    public static class EntityExtensions {
        /// <summary>
        ///     Load setting for given entity.
        /// </summary>
        /// <typeparam name="TSettings">The settings type.</typeparam>
        /// <param name="entity">The entity which setting should be loaded.</param>
        /// <param name="guid">The <see cref="Guid" /> key for the settings.</param>
        /// <returns>Return the loaded settings if available. Else it return default settings.</returns>
        public static TSettings Load<TSettings>(this IMyEntity entity, Guid guid) where TSettings : class, new() {
            var storage = entity.Storage;
            TSettings settings;
            if (storage != null && storage.ContainsKey(guid)) {
                var str = storage[guid];
                var data = Convert.FromBase64String(str);

                settings = MyAPIGateway.Utilities.SerializeFromBinary<TSettings>(data);
                if (settings != null) {
                    return settings;
                }
            }

            settings = new TSettings();

            return settings;
        }

        /// <summary>
        ///     Save the given settings to the <see cref="MyModStorageComponent" /> of given entity.
        /// </summary>
        /// <typeparam name="TSettings">The settings type.</typeparam>
        /// <param name="entity">The entity which holds the settings.</param>
        /// <param name="guid">The <see cref="Guid" /> which indicates the right key for the <see cref="MyModStorageComponent" />.</param>
        /// <param name="settings">The settings which should be saved.</param>
        public static void Save<TSettings>(this IMyEntity entity, Guid guid, TSettings settings) where TSettings : class, new() {
            if (entity.Storage == null) {
                entity.Storage = new MyModStorageComponent();
            }

            var storage = entity.Storage;
            var data = MyAPIGateway.Utilities.SerializeToBinary(settings);
            var str = Convert.ToBase64String(data);
            storage[guid] = str;
        }
    }
}