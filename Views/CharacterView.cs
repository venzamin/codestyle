using R.Client.Behavour.Character;
using R.Client.Entities;
using R.Client.Helpers;
using R.Client.Managers;
using R.Client.Messages;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Portfolio.Views
{
    public sealed class CharacterView : ActorView
    {
        internal Transform GetModel()
        {
            return Model;
        }

        //public event Action completedMove;
        public event Action startedAttackMotion;
        public event Action endedAttackCollision;
        public event Action endedAttackMotion;
        public event Action endedChargeAttackMotion;
        public event Action hitedChargeAttackMotion;
        private AnimationState _state;
        private Dictionary<eAniClip, AnimationState> _states;
        private CharacterAssignBehaviour _assigner;
        private CharacterViewBehaviour _behaviour;
        private List<CharacterWeaponBehaviour> _weaponBehaviours;
        private List<Renderer> _renderers;
        private List<ParticleSystem> _particles;
        private List<int> _affectedBushInstanceIDs;
        private bool _isOnBush;
        public bool IsOnBush => _isOnBush;

        public NavigationHandle NavigationHandle { get; internal set; }


        protected override string AddressableRoot => "Characters";
        public CharacterViewBehaviour Behaviour => _behaviour;
        public CharacterAssignBehaviour Assigner => _assigner;
        public float DestinationDistance { get; private set; }
        public bool IsMoveComplete => Mathf.Approximately(DestinationDistance, 0.0f);

        public CharacterView() : base()
        {
            _renderers = ListPool<Renderer>.Get();
            _particles = ListPool<ParticleSystem>.Get();
            _weaponBehaviours = new List<CharacterWeaponBehaviour>();
            _affectedBushInstanceIDs = ListPool<int>.Get();
        }
        public override void Dispose()
        {
            base.Dispose();
            ListPool<int>.Release(_affectedBushInstanceIDs);
            _affectedBushInstanceIDs.Clear();
            ListPool<ParticleSystem>.Release(_particles);
            _particles.Clear();
            ListPool<Renderer>.Release(_renderers);
            _renderers.Clear();
            DictionaryPool<eAniClip, AnimationState>.Release(_states);
            _states.Clear();
        }
        private void AddAnimationState<AnimationStateType>(eAniClip aniClip) where AnimationStateType : AnimationState, new()
        {
            if (_states == null)
                _states = DictionaryPool<eAniClip, AnimationState>.Get();

            var state = Activator.CreateInstance<AnimationStateType>();
            state.Initialize(Animator, aniClip);
            _states.Add(aniClip, state);
        }

        internal void TriggerBush(bool isEnter, int bushInstanceID)
        {
            if (isEnter)
            {
                if (!_affectedBushInstanceIDs.Contains(bushInstanceID))
                    _affectedBushInstanceIDs.Add(bushInstanceID);
            }
            else
            {
                if (_affectedBushInstanceIDs.Contains(bushInstanceID))
                    _affectedBushInstanceIDs.Remove(bushInstanceID);
            }
            _isOnBush = _affectedBushInstanceIDs.Count > 0;
            var enable = !_isOnBush;
            if (_renderers == null)
            {
                Helpers.Log.Warning("Invalide access of trigger bush. renderers is null");
            }
            else
            {
                for (int i = 0; i < _renderers.Count; ++i)
                {
                    if (_renderers[i].enabled == enable)
                        continue;
                    
                    _renderers[i].enabled = enable;
                }
            }

            if (_particles == null)
            {
                Helpers.Log.Warning("Invalide accses of trigger bush. _particles is null");
            }
            else
            {
                for (int i = 0; i < _particles.Count; ++i)
                {
                    if (_particles[i].gameObject.activeSelf == enable)
                        continue;

                    _particles[i].gameObject.SetActive(enable);
                }
            }
        }
        public void SetEnableRenderer(bool enable)
        {
            if (_renderers == null)
            {
                Helpers.Log.Warning("Invalide access of trigger bush. renderers is null");
            }
            else
            {
                for (int i = 0; i < _renderers.Count; ++i)
                {
                    if (_renderers[i].enabled == enable)
                        continue;

                    _renderers[i].enabled = enable;
                }
            }

            if (_particles == null)
            {
                Helpers.Log.Warning("Invalide accses of trigger bush. _particles is null");
            }
            else
            {
                for (int i = 0; i < _particles.Count; ++i)
                {
                    if (_particles[i].IsNull())
                        continue;

                    if (_particles[i].gameObject.IsAssigned())
                    {
                        if (_particles[i].gameObject.activeSelf == enable)
                            continue;

                        _particles[i].gameObject.SetActive(enable);
                    }
                }
            }
        }

        [MessageAnimate(eAniClip.Run)]
        public void Run()
        {
            ChangeState(eAniClip.Run);
        }
        [MessageAnimate(eAniClip.Walk)]
        public void Walk()
        {
            ChangeState(eAniClip.Walk);
        }
        [MessageAnimate(eAniClip.Stand)]
        public void Stand()
        {
            ChangeState(eAniClip.Stand);
        }
        [MessageAnimate(eAniClip.Hitted)]
        public void Hit()
        {
            int hitRandom = UnityEngine.Random.Range((int)eAniClip.Hitted, (int)eAniClip.Hitted3 + 1);
            ChangeState((eAniClip)hitRandom, true);

            var sfxKey = SFXHelper.GetSfxKeyByCharAssetAnimation(AssetKey, (eAniClip)hitRandom, true);
            SFXManager.Instance.PlayWorld(sfxKey, message.Position, false);
            var sfxWeaponKey = SFXHelper.GetSfxKeyByCharAssetAnimation(AssetKey, (eAniClip)hitRandom, false);
            SFXManager.Instance.PlayWorld(sfxWeaponKey, message.Position, false);

            var vfxKey = VFXHelper.GetVfxKeyByCharacterAnimation(AssetKey, (eAniClip)hitRandom); // dummy. attacker의 속성을 이용해서 받아와야함.
            VFXManager.Instance.Play(vfxKey, message.Position, message.Direction);

        }
        public void Stop()
        {
            Stand();
            var sfxKey = SFXHelper.GetSfxKeyByEnum(SFXHelper.eSfx.CharRun);
            SFXManager.Instance.SetAudioOff(sfxKey, Assigner.Root, true);
            //Translated();
        }
        public void BeginCharge()
        {
            ChangeState(eAniClip.ChargeBegin);
            var sfxKey = SFXHelper.GetSfxKeyByEnum(SFXHelper.eSfx.WeaponChargeAttack);
            SFXManager.Instance.PlayWorld(sfxKey, Assigner.Root.position, false);
        }
        public void ChargeAttack()
        {
            _state.Reset();
            ChangeState(eAniClip.ChargeAttack);
        }
        public void ChargeMove()
        {
            _state.Reset();
            ChangeState(eAniClip.ChargeWalk);
        }
        public void Charging()
        {
            _state.Reset();
            ChangeState(eAniClip.Charge, true);
        }
        public void ChargeIdle()
        {
            _state.Reset();
            ChangeState(eAniClip.ChargeIdle);
        }
        public void Die(bool useReset = true)
        {
            if (useReset)
                _state.Reset();

            SetWeaponTrail(false);
            ChangeState(eAniClip.Death);
            if (Model.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }
        }
        public void Revive()
        {
            _state.Reset();
            ChangeState(eAniClip.Stand);
            if (Model.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = true;
            }
        }
        public void Attack(Vector3 position, Vector3 direction, ushort attackMotionID)
        {
            Model.position = position;
            Destination = position;
            Direction = direction;
            UpdatePosition();
            
            if (!ChangeState((eAniClip)attackMotionID, true))
            {
                Helpers.Log.Error($"Invalid access animationKey. ({attackMotionID})");
                endedAttackMotion.SafeInvoke();
            }
        }
        private bool ChangeState(eAniClip aniClip, bool forcedChange = false)
        {
            if (_states.TryGetValue(aniClip, out var state))
            {
                ChangeState(state, forcedChange);
                return true;
            }

            Helpers.Log.Warning("Invalide access aniclip. do not found from states");
            return false;
        }
        private void ChangeState(AnimationState state, bool forcedChange = false)
        {
            if (!forcedChange && _state != null && _state.Equals(state))
                return;
            _state?.SetSpeed(1);
            _state?.End();
            _state = state;
            _state?.Begin();
        }
        public void Warp()
        {
            var angle = MathHelper.DirectionToAngle(Direction);
            var lookAtRotation = Quaternion.Euler(0, angle, 0);
            Model.SetPositionAndRotation(Destination, lookAtRotation);
        }
        public void SetTranslateInfo(Vector3 position, Vector3 destination, Vector3 direction)
        {
            Destination = destination;
            Direction = direction;
        }
        private void SetWeaponTrail(bool type)
        {
            foreach (var weapon in _weaponBehaviours)
            {
                VFXManager.Instance.SetTrailEmit(weapon.transform.GetInstanceID(), type);
            }
        }
        protected override void Updated()
        {
            base.Updated();
            UpdatePosition();
        }
        public void UpdatePosition()
        {
            DestinationDistance = Vector3.Distance(Model.position, Destination);
            var rate = DestinationDistance * TimeHelper.ClientDeltaTime;
            var angle = MathHelper.DirectionToAngle(NavigationHandle == null ? Direction : NavigationHandle.Direction);
            var lookAtRotation = Quaternion.Euler(0, angle, 0);
            var updatePosition = NavigationHandle == null ? Destination : NavigationHandle.Position;
            Model.SetPositionAndRotation(updatePosition, lookAtRotation);
        }
        public void SetAnimationSpeed(float speed)
        {
            _state?.SetSpeed(speed);
        }
        // public void RefreshPosition()
        // {
        // }
        [ActorAnimatorStateStatus(eAnimatorStateStatus.Exit, eAniClip.Attack)]
        [ActorAnimatorStateStatus(eAnimatorStateStatus.Exit, eAniClip.Attack2)]
        [ActorAnimatorStateStatus(eAnimatorStateStatus.Exit, eAniClip.Attack3)]
        public void ExitedAttackAnimationState(object sender, EventArgs args)
        {
            endedAttackMotion.SafeInvoke();
            _state.Reset();
        }
        [ActorAnimatorStateStatus(eAnimatorStateStatus.Exit, eAniClip.ChargeBegin)]
        public void ExitedBegineChargeAnimationState(object sender, EventArgs args)
        {
        }
        [ActorAnimatorStateStatus(eAnimatorStateStatus.Exit, eAniClip.ChargeWalk)]
        public void ExitedChargeWalkAnimationState(object sender, EventArgs args)
        {
            if (_state.Clip.Equals(eAniClip.ChargeWalk))
            {
                ChangeState(eAniClip.ChargeIdle);
            }
        }
        [ActorAnimatorStateStatus(eAnimatorStateStatus.Exit, eAniClip.ChargeAttack)]
        public void ExitedChargeAttackAnimationState(object sender, EventArgs args)
        {
            endedChargeAttackMotion.SafeInvoke();
            //_state.Reset();
        }
        [ActorAnimatorStateStatus(eAnimatorStateStatus.EndTime, eAniClip.Stand)]
        public void EndtimeStandAnimation(object sender, EventArgs args)
        {
            Debug.Log("EndtimeStandAnimation");
        }

        internal void Warp(Vector3 position, Vector3 direction)
        {
            Destination = position;
            Direction = direction;
            var angle = MathHelper.DirectionToAngle(Direction);
            var lookAtRotation = Quaternion.Euler(0, angle, 0);
            Model.SetPositionAndRotation(Destination, lookAtRotation);
        }

        internal void Disapear()
        {
            throw new NotImplementedException();
        }
        #region STATUS
        public void Spawn(AIMessage message)
        {
            InitColliders();
            SetTranslateInfo(message.Position, message.Destination, message.Direction);
            Warp();
            Stand();
        }

        #endregion
    }
}
