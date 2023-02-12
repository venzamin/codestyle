using Cysharp.Threading.Tasks;
using Portfolio.Framework;
using Portfolio.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;

namespace Portfolio.Views
{
    public enum eAnimatorStateStatus
    {
        None,
        Enter,
        Update,
        EndTime,
        Exit,
    }
    public abstract class ActorView : View
    {
        private long _id;
        private string _assetKey;
        private int _cachedInstanceID;
        private Animator _animator;
        public Vector3 Destination { get; protected set; }
        public Vector3 Direction { get; protected set; }

        protected override string AssetKey => _assetKey;
        protected Animator Animator => _animator; 
        public int InstanceID => _cachedInstanceID;
        public long ID => _id;

        private List<(eAniClip, Action)> _animateMessageProcessors;
        public ActorView() : base()
        {
            _animateMessageProcessors = new List<(eAniClip, Action)>();
        }
        public async UniTask Load(long id, string assetKey)
        {
            _id = id;
            _assetKey = assetKey;
            await Load();
        }
        protected override void CompleteLoad()
        {
            #region Collect Status Attribute Method
            List<(ActorAnimatorStateStatusAttribute, MethodInfo)> statusMethods = ListPool<(ActorAnimatorStateStatusAttribute, MethodInfo)>.Get();
            List<(MessageAnimateAttribute, MethodInfo)> animateMethods = ListPool<(MessageAnimateAttribute, MethodInfo)>.Get(); 
            var methods = GetType().GetMethods();
            foreach (var mathodInfo in methods)
            {
                var csAttributes = Attribute.GetCustomAttributes(mathodInfo);
                foreach (var atts in csAttributes)
                {
                    if (atts is ActorAnimatorStateStatusAttribute)
                    {
                        var attribute = atts as ActorAnimatorStateStatusAttribute;
                        statusMethods.Add((attribute, mathodInfo));
                    }
                    else if (atts is MessageAnimateAttribute)
                    {
                        var attribute = atts as MessageAnimateAttribute;
                        animateMethods.Add((attribute, mathodInfo));
                        _animateMessageProcessors.Add((attribute.Animation, () => mathodInfo.Invoke(this, null)));
                    }    
                }
            }
            #endregion
            _cachedInstanceID = Model.GetInstanceID();

            if (Model.TryGetComponent(out _animator))
            {
                if (statusMethods != null && statusMethods.Count > 0)
                {
                    var behaviours = _animator.GetBehaviours<ActorAnimatorStateBehaviour>();
                    foreach (var behaviour in behaviours)
                    {
                        var targets = statusMethods.FindAll((key) => (key.Item1.ClipType.Equals(behaviour.TargetClip)));
                        foreach (var keyValue in targets)
                        {
                            switch(keyValue.Item1.Status)
                            {
                                case eAnimatorStateStatus.Enter:
                                    behaviour.EnteredState += (sender, args) => InvokeAnimatorStatusEvent(keyValue.Item2, sender, args);
                                    break;
                                case eAnimatorStateStatus.Update:
                                    behaviour.UpdatedState += (sender, args) => InvokeAnimatorStatusEvent(keyValue.Item2, sender, args);
                                    break;
                                case eAnimatorStateStatus.Exit:
                                    behaviour.ExitedState += (sender, args) => InvokeAnimatorStatusEvent(keyValue.Item2, sender, args);
                                    break;
                                //case eAnimatorStateStatus.EndTime:
                                //    behaviour.endtimeState += (sender, args) => InvokeAnimatorStatusEvent(keyValue.Item2, sender, args);
                                    //break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            statusMethods.Clear();
            ListPool<(ActorAnimatorStateStatusAttribute, MethodInfo)>.Release(statusMethods);
        }
        public override void Dispose()
        {
            base.Dispose();
            var animatorStateBehaviours = _animator.GetBehaviours<ActorAnimatorStateBehaviour>();
            foreach (var behaviour in animatorStateBehaviours)
                behaviour.Dispose();
        }

        protected override void Translated()
        {
            Direction = Model.eulerAngles;
            Destination = Model.position;
        }
        private void InvokeAnimatorStatusEvent(MethodInfo info, object sender, EventArgs args)
        {
            if (info == null)
                return;

            object[] parameters = new object[2] 
            {
                sender,
                args,
            };
            info.Invoke(this, parameters);
        }
        public virtual void ProcessMessageAnimate(MessageViewAnimate message)
        {
            for (int i = 0; i < _animateMessageProcessors.Count; ++i)
            {
                if (_animateMessageProcessors[i].Item1 == message.Clip)
                {
                    _animateMessageProcessors[i].Item2.SafeInvoke();
                }
            }
        }
    }
}
