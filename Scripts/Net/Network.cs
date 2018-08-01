using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable TryCastAlwaysSucceeds
// ReSharper disable MergeCastWithTypeCheck

namespace AutoMcD.PocketGear.Net {
    public class Network {
        public delegate void SyncRequestMessageHandler(ISyncRequestMessage syncRequestMessage);

        public delegate void SyncResponseMessageHandler(ISyncResponseMessage syncRequestMessage);

        private readonly Dictionary<long, Action<IEntitySyncMessage>> _entitySyncHandler = new Dictionary<long, Action<IEntitySyncMessage>>();
        private readonly ushort _id;

        public Network(ushort id) {
            _id = id;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(_id, OnMessageReceived);
        }

        public bool IsDedicated => MyAPIGateway.Utilities.IsDedicated;

        public bool IsServer => MyAPIGateway.Multiplayer.IsServer;

        public ulong MyId => MyAPIGateway.Multiplayer.MyId;

        protected virtual void FireOnSyncRequestReceived(ISyncRequestMessage syncRequestMessage) {
            SyncRequestReceived?.Invoke(syncRequestMessage);
        }

        protected virtual void FireOnSyncResponseReceived(ISyncResponseMessage syncResponseMessage) {
            SyncResponseReceived?.Invoke(syncResponseMessage);
        }

        public void Close() {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(_id, OnMessageReceived);
        }

        public void RegisterEntitySyncHandler(long entityId, Action<IEntitySyncMessage> action) {
            var key = entityId;
            if (!_entitySyncHandler.ContainsKey(key)) {
                _entitySyncHandler.Add(key, action);
            }
        }

        public void Send(IMessage message, ulong recipient) {
            var wrapper = new MessageWrapper {
                Sender = MyId,
                Message = message
            };
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(wrapper);
            MyAPIGateway.Multiplayer.SendMessageTo(_id, bytes, recipient);
        }

        public void SendToServer(ISyncRequestMessage request) {
            var wrapper = new MessageWrapper {
                Sender = MyId,
                Message = request
            };
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(wrapper);
            MyAPIGateway.Multiplayer.SendMessageToServer(_id, bytes);
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
            } else if (wrapper.Message is ISyncResponseMessage) {
                var syncMessage = wrapper.Message as ISyncResponseMessage;
                FireOnSyncResponseReceived(syncMessage);
            } else if (wrapper.Message is ISyncRequestMessage) {
                var syncMessage = wrapper.Message as ISyncRequestMessage;
                FireOnSyncRequestReceived(syncMessage);
            }
        }

        public event SyncRequestMessageHandler SyncRequestReceived;
        public event SyncResponseMessageHandler SyncResponseReceived;
    }
}