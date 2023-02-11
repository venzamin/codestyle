///
// 스타일 참고를 위한 코드의 일부 입니다.
///

using Portfolio.Networks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Pool;

namespace Portfolio.Services
{
    public class RoomService : NetworkService
    {
        private RoomEntity _ownerRoom;
        private Dictionary<ushort, RoomEntity> _rooms;
        public ReadOnlyDictionary<ushort,RoomEntity> Rooms { get; private set; }
        public RoomEntity OwnerRoom
        {
            get
            {
                if (_ownerRoom == null)
                    throw new ClientException($"Invalide access current room from lobby service. _roomService is null");

                return _ownerRoom;
            }
        }
        public RoomService(INetworkProvider components) : base(components)
        {
            _rooms = new Dictionary<ushort, RoomEntity>();
            Rooms = new ReadOnlyDictionary<ushort, RoomEntity>(_rooms);
        }
        public bool IsCreator
        {
            get
            {
                if (_ownerRoom == null)
                    return false;
                return UserHelper.IsOwnerCurrentCharacater(OwnerRoom.CreatorID);
            }
        }
        public override void Dispose()
        {
            if (IsCreator && _ownerRoom != null)
            {
                SendCS_BT_DELETE_ROOM_REQ(); 
            }
            Clear();
        }
        private void Clear()
        {
            foreach (var pair in _rooms)
            {
                pair.Value.Dispose();
                GenericPool<RoomEntity>.Release(pair.Value);
            }
            _rooms.Clear();
        }
        public override bool ProcessPacket(CSProtocolID protocolID, Packet packet)
        {
            switch (protocolID)
            {
            case CSProtocolID.CS_IF_ROOM_LIST_ACK:
                RecieveCS_IF_ROOM_LIST_ACK(packet);
                return true;
            case CSProtocolID.CS_IF_ROOM_INFO_ACK:
                RecieveCS_IF_ROOM_INFO_ACK(packet);
                return true;
            case CSProtocolID.CS_BT_MAKE_ROOM_ACK:
                RecieveCS_BT_MAKE_ROOM_ACK(packet);
                return true;
            case CSProtocolID.CS_BT_DELETE_ROOM_ACK:
                RecieveCS_BT_DELETE_ROOM_ACK(packet);
                return true;
            case CSProtocolID.CS_BT_JOIN_ROOM_ACK:
                RecieveCS_BT_JOIN_ROOM_ACK(packet);
                return true;
            // case CSProtocolID.CS_BT_LEAVE_ROOM_ACK:
            //     RecieveCS_BT_LEAVE_ROOM_ACK(packet);
            //     return true;
            // case CSProtocolID.CS_BT_READY_GAME_ACK:
            //     RecieveCS_BT_READY_GAME_ACK(packet);
            //     return true;
            // case CSProtocolID.CS_BT_START_GAME_ACK:
            //     RecieveCS_BT_START_GAME_ACK(packet);
            //     return true;
            // case CSProtocolID.CS_IF_ROOM_STATUS_ACK:
            //     RecieveCS_IF_ROOM_STATUS_ACK(packet);
            //     return true;
            // case CSProtocolID.CS_BT_CHANGE_TEAM_ACK:
            //     RecieveCS_BT_CHANGE_TEAM_ACK(packet);
            //     return true;

            }
            return false;
        }
        #region CS_IF_ROOM_JOIN
        private void RecieveCS_BT_JOIN_ROOM_ACK(Packet packet)
        {
            byte result = packet.ReadByte();
            if (result == 0)
            {
                var roomID = packet.ReadUShort();
                if (_rooms.TryGetValue(roomID, out var room))
                {
                    var roomUser = room.Join(packet);
                    if (roomUser == null)
                        throw new ClientException($"OnCS_BT_JOIN_ROOM_ACK");
                    _ownerRoom = room;
                    SendMessageJoinRoom(roomID, roomUser);
                    SendCS_BT_READY_GAME_REQ();
                }
            }
            else
            {
                Log.Warning(CSProtocolID.CS_BT_JOIN_ROOM_ACK, result);
            }
        }
        public void SendCS_BT_JOIN_ROOM_REQ(ushort roomID, long userID, byte teamType = 0)
        {
            Sender.Request(CSProtocolID.CS_BT_JOIN_ROOM_REQ, (packet) =>
            {
                packet.Write(roomID);
                if (teamType == 0)
                {
                    if (_rooms.TryGetValue(roomID, out var room))
                    {
                        teamType = room.GetEmptySlotIndex();
                    }
                }
                packet.Write(teamType);
                packet.Write(userID);
            }); 
        }
        private void SendMessageJoinRoom(ushort roomID, BattleMember roomUser)
        {
            var args = EventArgsPool.Create<ushort, BattleMember>();
            args.Set(roomID, roomUser);
            MessageManager.Instance.Send<MessageJoinRoom>(args);
        }
        #endregion
        #region CS_IF_ROOM_DELETE
        private void RecieveCS_BT_DELETE_ROOM_ACK(Packet packet)
        {
            byte result = packet.ReadByte();
            if (result == 0)
            {
                var roomID = packet.ReadUShort();
                DeleteRoom(roomID);
            }
            else
            {
                Log.Warning(CSProtocolID.CS_BT_DELETE_ROOM_ACK, result);
            }
        }
        public void SendCS_BT_DELETE_ROOM_REQ()
        {
            if (_ownerRoom == null)
                return;

            Sender.Request(CSProtocolID.CS_BT_DELETE_ROOM_REQ, (packet) =>
            {
                packet.Write(_ownerRoom.ID);
            });
        }
        private void SendMessageDeleteRoom(ushort roomID)
        {
            var args = EventArgsPool.Create<ushort>();
            args.Set(roomID);
            MessageManager.Instance.Send<MessageDeleteRoom>(args);
        }
        private void DeleteRoom(ushort roomID)
        {
            SendMessageDeleteRoom(roomID);
            if (_rooms.ContainsKey(roomID))
            {
                _rooms[roomID].Dispose();
                _rooms.Remove(roomID);
            }

            if (_ownerRoom != null && _ownerRoom.ID.Equals(roomID))
            {
                _ownerRoom = null;
            }
        }
        #endregion
        #region CS_IF_ROOM_INFO
        private void RecieveCS_IF_ROOM_INFO_ACK(Packet packet)
        {
            var result = packet.ReadByte();
            if (result == 0)
            {
                var roomID = packet.ReadUShort();
                if (!_rooms.TryGetValue(roomID, out var room))
                {
                    room = GenericPool<RoomEntity>.Get();
                    _rooms.Add(roomID, room);
                }
                room.Initialize(roomID);
                room.AnalyzePacket(packet);
                SendMessageRoomInfo(roomID);
            }
            else
            {
                Helpers.Log.Warning(CSProtocolID.CS_IF_ROOM_INFO_ACK, result);
            }
        }
        public void SendCS_IF_ROOM_INFO_REQ(ushort roomID)
        {
            Sender.Request(CSProtocolID.CS_IF_ROOM_INFO_REQ, (packet) =>
            {
                packet.Write(roomID);
            });
        }
        private void SendMessageRoomInfo(ushort roomID)
        {
            var args = EventArgsPool.Create<ushort>();
            args.Set(roomID);
            MessageManager.Instance.Send<MessageRoomInfo>(args);
        }
        #endregion

        #region  CS_IF_ROOM_LIST
        private void RecieveCS_IF_ROOM_LIST_ACK(Packet packet)
        {
            var result = packet.ReadByte();
            if (result == 0)
            {
                Clear();
                ushort page = packet.ReadUShort();
                byte pageSize = packet.ReadByte();
                ushort totalCount = packet.ReadUShort();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    var id = packet.ReadUShort();
                    if (id == 0)
                        continue;
                    if (!_rooms.TryGetValue(id, out var room))
                    {
                        room = GenericPool<RoomEntity>.Get();
                        _rooms.Add(id, room);
                    }
                    room.Initialize(id);
                    room.AnalyzePacket(packet);
                }
                SendMessageRoomList(page, pageSize, totalCount); 
            }
            else
                Helpers.Log.Warning(CSProtocolID.CS_IF_ROOM_LIST_ACK, result);
        }
        public void SendCS_IF_ROOM_LIST_REQ(ushort page, byte pageSize)
        {
            Sender.Request(CSProtocolID.CS_IF_ROOM_LIST_REQ, (packet) =>
            {
                packet.Write(page);
                packet.Write(pageSize);
            });
        }
        private void SendMessageRoomList(ushort page, byte pageSize, ushort totalCount)
        {
            var args = EventArgsPool.Create<ushort, byte, ushort>();
            args.Set(page, pageSize, totalCount);
            MessageManager.Instance.Send<MessageRoomList>(args);
        }
        #endregion
        #region MAKE_ROOM
        private void RecieveCS_BT_MAKE_ROOM_ACK(Packet packet)
        {
            byte result = packet.ReadByte();
            if (result == 0)
            {
                var roomID = packet.ReadUShort();
                if (!_rooms.TryGetValue(roomID, out var room))
                {
                    room = GenericPool<RoomEntity>.Get();
                    room.Initialize(roomID);
                    _rooms.Add(roomID, room);
                }
                room.AnalyzePacket(packet);
                _ownerRoom = room;
                SendMessageMakeRoom(room);
                SendCS_BT_READY_GAME_REQ();
           }
            else
            {
                Log.Warning(CSProtocolID.CS_BT_MAKE_ROOM_ACK, result);
            }
        }
        public void SendCS_BT_MAKE_ROOM_REQ(ushort zoneID, byte roomType = 1)
        {
            Sender.Request(CSProtocolID.CS_BT_MAKE_ROOM_REQ, (packet) =>
            {
                packet.Write(zoneID); // mapID == zoneID
                packet.Write(roomType); // 3v3 = 1
            });
        }
        private void SendMessageMakeRoom(RoomEntity room)
        {
            var args = EventArgsPool.Create<RoomEntity>();
            args.Set(room);
            MessageManager.Instance.Send<MessageRoomMake>(args);
        }
        #endregion
    }
}
