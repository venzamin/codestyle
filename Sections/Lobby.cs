using Cysharp.Threading.Tasks;
using Portfolio.Framework;
using Portfolio.Services;
using System.Linq;

namespace R.Client
{
    public class Lobby : Section
    {
        private const string c_EmpteySlotMessage = "빈자리가 있어 모험을 시작할 수 없어요.";
        private const byte c_PageSize = 6;
        enum eLobbyState
        {
            Lobby,
            Room,
        }
        private ushort _page;
        eLobbyState _state;
        UIDummy3vs3PVPSelector _instancePvPLobby;
        UIDummy3vs3PVPRoom _instancePvpRoom;

        UserService UserService => NetworkManager.Instance.User;
        RoomService RoomService => NetworkManager.Instance.Room;

        protected override async UniTask OnLoad()
        {
            UserService.RequestCharacterInformation(101);
            var uiInstancePvpSelector = await AssetManager.Instance.Instantiate("UI/UIDummy3vs3PVPSelector.prefab");
            if (uiInstancePvpSelector.IsAssigned())
            {
                _instancePvPLobby = uiInstancePvpSelector.GetComponentInChildren<UIDummy3vs3PVPSelector>();
                if (_instancePvPLobby.IsAssigned())
                {
                    _instancePvPLobby.renewedLobby += RequestLobbyInfo;
                    _instancePvPLobby.exitedLobby += Disconnect;
                    _instancePvPLobby.createdRoom += CreatePVPBattleRoom;
                    _instancePvPLobby.joinedRoom += RequestJoinRoom;
                }
            }
            var uiInstancePvpRoom = await AssetManager.Instance.Instantiate("UI/UIDummy3vs3PVPRoom.prefab");
            if (uiInstancePvpRoom.IsAssigned())
            {
                _instancePvpRoom = uiInstancePvpRoom.GetComponentInChildren<UIDummy3vs3PVPRoom>();
                if (_instancePvpRoom.IsAssigned())
                {
                    _instancePvpRoom.clickedStart += RequestStartGame;
                    _instancePvpRoom.clickedExit += RequestExitRoom;
                    _instancePvpRoom.clickedForcedStart += ReqeustForceStartGame;
                }
                _instancePvpRoom.EnableCanvas(false);
            }
            await base.OnLoad();
            RoomService.SendCS_IF_ROOM_LIST_REQ(_page, c_PageSize);
            var sfxKey = SFXHelper.GetSfxKeyByEnum(SFXHelper.eSfx.BGM3vs3Lobby);
            SFXManager.Instance.PlayBGM(sfxKey);
        }
        private void CreatePVPBattleRoom()
        {
            RoomService.SendCS_BT_MAKE_ROOM_REQ(1000);
        }
        private void RequestLobbyInfo()
        {
            RoomService.SendCS_IF_ROOM_LIST_REQ(_page, c_PageSize);
        }
        private void RequestJoinRoom(int roomID)
        {
            RoomService.SendCS_BT_JOIN_ROOM_REQ((ushort)roomID, UserService.CurrentCharacter.ID);
        }

        private void RequestStartGame()
        {
            if (RoomService.OwnerRoom.IsAllReady)
            {
                if (RoomService.IsCreator)
                {
                    if (RoomService.OwnerRoom.TotalMemberCount < 6)
                    {
                        UIManager.Instance.ShowToastMessage(c_EmpteySlotMessage);
                    }
                    else
                        RoomService.SendCS_BT_START_GAME_REQ();

                }
                else
                {
                    UIManager.Instance.ShowToastMessage("방장만 게임을 시작할 수 있습니다.");
                }

            }
            else
            {
                UIManager.Instance.ShowToastMessage(c_EmpteySlotMessage);
            }
        }
        private void ReqeustForceStartGame()
        {
            RoomService.SendCS_BT_START_GAME_REQ();
        }
        private void RequestExitRoom()
        {
            if (RoomService.OwnerRoom == null)
                throw new ClientException("Invalide access current room. current room is null");
           
            if (RoomService.OwnerRoom.CreatorID == UserService.CurrentCharacter.ID)
            {
                RoomService.SendCS_BT_DELETE_ROOM_REQ();
            }
            else
            {
                RoomService.SendCS_BT_LEAVE_ROOM_REQ();
            }
        }
        private void Disconnect()
        {
            NetworkManager.Instance.Clear();
            TemplateManager.Instance.Clear();
            SectionManager.Instance.Change<Sections.Title>().GetAwaiter().OnCompleted(() =>
            {
                UIManager.Instance.ShowToastMessage("서버와의 접속이 종료되었습니다.");
                System.GC.Collect();
            });
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            AutoRequestRoomList();
        }
        protected override void OnProcessMessages()
        {
            base.OnProcessMessages();
            MessageManager.Instance.TryReceiveLoop<MessageRoomList>((roomList) =>
            {
                _instancePvPLobby.SetPageInfo(roomList.Page, roomList.PageSize, roomList.TotalCount);
                _instancePvPLobby.DisplayRoomPage(RoomService.Rooms.Values.ToArray());
                ShowLobbyUI();
            });
            MessageManager.Instance.TryReceiveLoop<MessageRoomMake>((makeRoom) =>
            {
                _instancePvpRoom.SetRoom(makeRoom.Entity);
                ShowRoomUI();
            });
            MessageManager.Instance.TryReceiveLoop<MessageJoinRoom>((joinRoom) =>
            {
                if (RoomService.Rooms.TryGetValue(joinRoom.RoomID, out var room))
                {
                    _instancePvpRoom.SetRoom(room);
                }
                ShowRoomUI();
            });
            MessageManager.Instance.TryReceiveLoop<MessageLeaveRoom>((leave) =>
            {
                if (UserService.IsMineCharacter(leave.Leaver))
                {
                    RoomService.SendCS_IF_ROOM_LIST_REQ(_page, c_PageSize);
                }
                else
                {
                    _instancePvpRoom.SetRoom(RoomService.OwnerRoom);
                }
            });
            MessageManager.Instance.TryReceiveLoop<MessageDeleteRoom>((deleteRoom) =>
            {
                RoomService.SendCS_IF_ROOM_LIST_REQ(_page, c_PageSize);
                UIManager.Instance.ShowToastMessage("방장이 방을 해체했습니다.");
            });
            MessageManager.Instance.TryReceiveLoop<MessageRoomInfo>((roomInfo) =>
            {
                switch (_state)
                {
                case eLobbyState.Lobby:
                    _instancePvPLobby.DisplayRoomPage(RoomService.Rooms.Values.ToArray());
                    break;
                case eLobbyState.Room:
                    if (RoomService.OwnerRoom.ID.Equals(roomInfo.RoomID))
                    {
                        _instancePvpRoom.SetRoom(RoomService.OwnerRoom);
                    }
                    break;
                }
            });
            MessageManager.Instance.TryReceiveLoop<MessageReady>((ready) =>
            {
                if (_state == eLobbyState.Room)
                    _instancePvpRoom.SetRoom(RoomService.OwnerRoom);
            });
            MessageManager.Instance.TryReceiveLoop<MessageStartGame>((startGameMsg) =>
            {
                UserService.SendCS_IF_MAP_INFO_REQ(startGameMsg.RoomID, startGameMsg.MapID, 0, 0, null);
            });
            MessageManager.Instance.TryReceiveLoop<MessageMapInformation>((mapInformation) =>
            {
                var parameter = EventArgsPool.Create<ushort, ushort, ushort>();
                parameter.Set(0, mapInformation.ZoneTemplateID, mapInformation.RoomID);
                SectionManager.Instance.Change<PVPBattle>(parameter).Forget();
            });
            ReceiveUIMessageMovePage();
            ReceiveUIMessageChangeTeam();
        }
        private void ReceiveUIMessageMovePage()
        {
            MessageManager.Instance.TryReceiveLoop<MessageUIReqeustPageUpdate>((message) =>
            {
                RoomService.SendCS_IF_ROOM_LIST_REQ(message.Page, message.PageSize);
            });
        }
        private void ReceiveUIMessageChangeTeam()
        {
            MessageManager.Instance.TryReceiveLoop<MessageUIChangeTeam>((message) =>
            {
                RoomService.SendCS_BT_CHANGE_TEAM_REQ(message.Src, message.Des);
            });
        }
        private void ShowLobbyUI()
        {
            _instancePvpRoom.EnableCanvas(false);
            _instancePvPLobby.EnableCanvas(true);
            _state = eLobbyState.Lobby;
        }
        private void ShowRoomUI()
        {
            _instancePvPLobby.EnableCanvas(false);
            _instancePvpRoom.EnableCanvas(true);
            _state = eLobbyState.Room;
        }
        float _timer = 10.0f;
        private void AutoRequestRoomList()
        {
            _timer -= TimeHelper.ClientDeltaTime;
            if (_timer <= 0)
            {
                if (_state.Equals(eLobbyState.Lobby))
                    RoomService.SendCS_IF_ROOM_LIST_REQ(_page, c_PageSize);
                _timer = 10.0f;
            }
        }
    }
}
