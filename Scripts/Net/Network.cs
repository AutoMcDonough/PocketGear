using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable TryCastAlwaysSucceeds
// ReSharper disable MergeCastWithTypeCheck

namespace AutoMcD.PocketGear.Net {
    public class Network {
        private readonly Dictionary<long, Action<IEntitySyncMessage>> _entitySyncHandler = new Dictionary<long, Action<IEntitySyncMessage>>();
        private readonly ushort _id;

        public Network(ushort id) {
            _id = id;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(_id, OnMessageReceived);
        }

        public bool IsDedicated => MyAPIGateway.Utilities.IsDedicated;

        public bool IsServer => MyAPIGateway.Multiplayer.IsServer;

        public void Close() {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(_id, OnMessageReceived);
        }

        public void RegisterEntitySyncHandler(long entityId, Action<IEntitySyncMessage> action) {
            var key = entityId;
            if (!_entitySyncHandler.ContainsKey(key)) {
                _entitySyncHandler.Add(key, action);
            }
        }

        public void Sync(IEntitySyncMessage syncMessage) {
            if (MyAPIGateway.Multiplayer.MultiplayerActive) {
                var wrapper = new MessageWrapper {
                    Sender = MyAPIGateway.Multiplayer.MyId,
                    Message = syncMessage
                };
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(wrapper);
                MyAPIGateway.Multiplayer.SendMessageToOthers(_id, bytes);
            }
        }

        public void UnRegisterEntitySyncHandler(long entityId, Action<IEntitySyncMessage> action) {
            var key = entityId;
            if (_entitySyncHandler.ContainsKey(key)) {
                _entitySyncHandler.Remove(key);
            }
        }

        private void OnMessageReceived(byte[] bytes) {
            var wrapper = MyAPIGateway.Utilities.SerializeFromBinary<MessageWrapper>(bytes);

            if (wrapper.Message is IEntitySyncMessage) {
                var syncMessage = wrapper.Message as IEntitySyncMessage;
                var key = syncMessage.EntityId;
                if (_entitySyncHandler.ContainsKey(key)) {
                    _entitySyncHandler[key](syncMessage);
                }
            }
        }
    }
}