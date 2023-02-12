using Portfolio.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Portfolio.Processors
{
    public class CharacterProcessor : EntityProcessor<CharacterEntity>
    {
        private bool _inversTransformFlag;
        private byte _teamType;
        private byte _slotIndex;
        private NavigationHandle _navHandle;
        private ListHolder<BattleState> _states;

        public long ID => Entity.ID;
        public byte TeamType => _teamType;
        public byte SlotIndex => _slotIndex;
        public bool InversTransformFlag => _inversTransformFlag;
        public bool Attackable => Entity.Attackable;
        public ushort ActionKey
        {
            get => Entity.ActionKey;
            set => Entity.ActionKey = value;
        }
        public ushort AttackAnimationKey => Entity.AttackAnimationKey;
        public string AssetKey => Entity.AssetKey;
        public string NickName => Entity.NickName;
        public uint MaxHP => Entity.MaxHP;
        public uint MaxMP => Entity.MaxMP;
        public eBaseKind Kind => Entity.Kind;
        public Vector3 Position => Entity.Position;
        public Vector3 Direction { get => Entity.Direction; set => Entity.Direction = value; }
        public Vector3 Destination => Entity.Destination;
        public eAbnormalStatus AbnormalStatus => Entity.AbnormalStatus;
        public byte ChargeCount
        {
            get => Entity.ChargeCount;
            set => Entity.ChargeCount = value;
        }
        public MovementEntity NavigationPtr { get => Entity.Movement; }
        public NavigationHandle Handle => _navHandle;
        public static bool Create(Packet packet, long id, out CharacterProcessor processor)
        {
            var templateID = packet.ReadUShort();
            var nickName = packet.ReadString();
            var mapID = packet.ReadUShort();

            var entity = new CharacterEntity();
            entity.Initialize(id, templateID);
            entity.SetNickName(nickName);
            entity.SetAbnormalStatus(0);
            processor = new CharacterProcessor(entity);
            processor.EnterMap(packet);

            return true;
        }
        public static bool Create(CharacterEntity entity, ushort templateID, out CharacterProcessor processor)
        {
            processor = new CharacterProcessor(entity);
            entity.AssignTemplate(templateID);
            return true;
        }
        public CharacterProcessor(CharacterEntity entity) : base(entity)
        {
            _states = GenericPool<ListHolder<BattleState>>.Get();
        }
        public void Prepare(bool inversTransform)
        {
            SetInversTransformFlag(inversTransform);
            _navHandle = NavigationManager.Instance.PrepareCharacterHandle(ID, inversTransform, TransferInformation.GetTransfer(Entity));
            _navHandle.InversMovement = inversTransform;
        }
        private void RegisterState(BattleState state)
        {
            if (_states == null)
            {
                Log.Error("_states list is null. please check code for preparation _states.");
                return;
            }
            if (state == null)
            {
                Log.Error("can't register null state. please check code for register target state.");
                return;
            }
            if (_states.Contains(state))
            {
                Log.Warning($"aready had state({state.GetType().Name})");
                return;
            }
            state.Enter(UnregisterState);
            _states.Add(state);
        }
        private void UnregisterState(BattleState state)
        {
            if (_states == null)
            {
                Log.Error("_states list is null. please check code for preparation _states.");
                return;
            }
            if (state == null)
            {
                Log.Error("can't register null state. please check code for register target state.");
                return;
            }
            if (_states.Contains(state))
            {
                _states.Remove(state);
            }
        }
        public void Update()
        {
            UpdateState();
            float deltaTime = TimeHelper.ClientDeltaTime;
            _navHandle?.Update(deltaTime);
            Entity.Position = _navHandle.Position;
        }
        private void UpdateState()
        {
            if (_states == null)
                return;

            for (int i = 0; i < _states.Count; i++)
            {
                if (_states.TryGetAt(i, out var state))
                {
                    state.Process();
                }
            }
        }
        public bool IsEnemy(byte teamType)
        {
            return Entity.IsEnemy(teamType);
        }
        public override void Dispose()
        {
            GenericPool<ListHolder<BattleState>>.Release(_states);
            _states.Dispose();
            Entity.Dispose();
        }
        public void PrepareMemberInformation(EventArgs args)
        {
            if (!args.TryGetValue<byte, byte>(out _teamType, out _slotIndex))
            {
                throw new ClientException(eClientExceptionTrait.ERROR_INVALIDE_RECEIVE_MEMBERINFO);
            }
        }
        #region Process
        public void Warp()
        {
            Entity.Destination = Entity.Position;
            _navHandle.Position = Entity.Position;
            _navHandle.Direction= Entity.Direction;
            _navHandle.Destination = Entity.Destination;
        }
        public void Warp(Vector3 position, Vector3 direction, Vector3 destination)
        {
            _navHandle.Position = position; 
            Entity.Position = position;
            _navHandle.Direction = direction;
            Entity.Direction = direction;
            _navHandle.Destination = destination;
            Entity.Destination = destination;
        }
        public void InputDirection(Vector3 input)
        {
            Direction = input * Entity.TemplateMoveSpeed; 
            _navHandle.Direction = Direction;
        }
        public void InputDestination(Vector3 destination)
        {
            _navHandle.Destination = destination;
            Entity.Destination = destination;
        }
        public void InputMoveSpeed(float rate)
        {
            Entity.MoveSpeed = rate * Entity.TemplateMoveSpeed;
        }
        public void InputMoveType(eMoveType moveType)
        {
            Entity.MoveType = moveType;
        }

        public void SetHP(uint remain, uint max) => Entity.SetHP(remain, max);
        public void SetMP(uint remain, uint max) => Entity.SetMP(remain, max);
        public void SetTeamType(byte teamType) => Entity.SetTeamType(teamType);
        public void SetSlotIndex(byte slotIndex) => Entity.SetSlotIndex(slotIndex);
        #endregion
        #region State 
        public void EnterMap(Packet packet)
        {
            Entity.EnterMap(packet);
        }
        public void Spawn()
        {
            Entity.EnterMap();
            var state = BattleStateFactory.Instance.Create<Spawn>();
            RegisterState(state);
        }
        public void Attack(Packet packet)
        {
            AnalyseAttackPacket(packet);
            if (ChargeCount > 0)
                SendChargeEndMessage();
            else
                SendAttackMessage();
        }
        public void AttackStop()
        {
            Entity.StopAttack();
        }
        public void MoveStart()
        {
            eAniClip eAniClip = Entity.MoveSpeed > 0.8f ? eAniClip.Run : eAniClip.Walk;
            SendAnimateMesssge(ID, eAniClip, Entity.MoveSpeed);
            SendMoveMessage(false, Entity.TargetID);
        }
        public void MoveStart(Packet packet)
        {
            eAniClip eAniClip = Entity.MoveSpeed > 0.8f ? eAniClip.Run : eAniClip.Walk;
            SendAnimateMesssge(ID, eAniClip, Entity.MoveSpeed);

            MoveStart();
            Entity.ReceiveStartMovePacket(packet);
            _navHandle.Position = Entity.Position;
            _navHandle.Destination = Entity.Destination;
            _navHandle.Direction = Entity.Direction;

            ProtocolEventManager.Instance.TryInvoke(CSProtocolID.CS_IF_NAV_START_ACK, Entity);
        }
        public void MoveEnd()
        {
            _navHandle.Position = Entity.Position;
            _navHandle.Destination = Entity.Destination;
            _navHandle.Direction = Entity.Direction;

            _navHandle.Stop();
            SendMoveMessage(true, Entity.TargetID);
            SendAnimateMesssge(ID, eAniClip.Stand, 0);
        }
        public void MoveEnd(Packet packet)
        {
            Entity.ReceiveEndMovePacket(packet);
            MoveEnd();
        }
        public void Move(Packet packet)
        {
            Entity.ReceiveMovePacket(packet);
            _navHandle.Position = Entity.Position;
            _navHandle.Destination = Entity.Destination;
            _navHandle.Direction = Entity.Direction;
            SendMoveMessage(false, Entity.TargetID);
        }
        public void Hit(Packet packet)
        {
            Entity.ReceiveHit(packet, out uint damage);
            SendHitMessage();
            DamageFloatingManager.Instance.Play(damage, Handle.Position);
        }
        public void LeaveMap()
        {

            Entity.LeaveMap();
        }
        public void Die()
        {
            Entity.Die();
        }
        #endregion
        #region Messages
        public void SendAnimateMesssge(long id, eAniClip clip, float speed)
        {
            MessageManager.Instance.Send<MessageViewAnimate, long, eAniClip, float>(id, clip, speed);
        }
        public void SendAttackMessage()
        {
            var message = AIMessage.Create(this, eAIMessageType.Attack);
            var args = EventArgsPool.Create<ushort, string>();
            args.Set(ActionKey, AssetKey);
            message.Args = args;
            AIMessageManager.Instance.AddMessage(message);

        }
        public void SendMoveMessage(bool moveEnd, long targetID)
        {
            var message = AIMessage.Create(this, eAIMessageType.Move);
            var args = EventArgsPool.Create<bool, float, MovementEntity>();
            args.Set(moveEnd, Entity.MoveSpeed / Entity.TemplateMoveSpeed, Entity.Movement);
            message.Args = args;
            AIMessageManager.Instance.AddMessage(message);
        }
        private void SendChargeEndMessage()
        {
            var message = AIMessage.Create(this, eAIMessageType.EndCharge);
            var args = EventArgsPool.Create<string, byte>();
            args.Set(AssetKey, ChargeCount);
            message.Args = args;
            AIMessageManager.Instance.AddMessage(message);
        }
        private void SendHitMessage()
        {
            var message = AIMessage.Create(this, eAIMessageType.Hitted);
            var args = EventArgsPool.Create<ushort>();
            args.Set(ActionKey);
            message.Args = args;
            AIMessageManager.Instance.AddMessage(message);
        }
        #endregion
        #region Analyse Packets 
        public void AnalyseNavPacket(Packet packet)
        {
            Entity.AnalyseNavPacket(packet);
        }
        public void AnalyseStatusPacket(Packet packet)
        {
            Entity.AnalyseStatusPacket(packet);
        }
        public void AnalyseAttackPacket(Packet packet)
        {
            Entity.AnalyseAttackPacket(packet);
        }
        public void AnalysePositionDirectionPacket(Packet packet)
        {
            Entity.AnalysePositionDirectionPacket(packet);
        }
        public void BuildMovePacket(ref Packet packet)
        {
            packet.Write((byte)eBaseKind.Character);
            packet.Write(ID);
            packet.Write((byte)Entity.MoveType);
            packet.Write(Entity.Position);
            packet.Write(Entity.Destination);
            packet.Write(Entity.MoveSpeed);

            if (Entity.MoveType == eMoveType.Object)
            {
                packet.Write(Entity.TargetID);
                packet.Write((byte)eMapObject.Character);
            }
        }
        #endregion
        #region Abnormal Status 
        public void SetAbnormalStatus(byte status)
        {
            Entity.SetAbnormalStatus(status);
        }
        private void SetInversTransformFlag(bool flag)
        {
            _inversTransformFlag = flag;
        }

        #endregion
    }
}
