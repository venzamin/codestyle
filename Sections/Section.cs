using Cysharp.Threading.Tasks;
using Venus.Client.Managers;
using System;
using UnityEngine;

namespace Portfolio.Framework
{
    public abstract class Section : MonoBehaviour, IDisposable
    {
        private bool _isDisposed;
        private bool _isCompletedLoad;
        public virtual string SceneName { get { return GetType().Name; } }
        public Section()
        {
            _isCompletedLoad = false;
        }
        public async UniTask Load(object parameter = null)
        {
            AnalyseSectionParameters(parameter);
            Initialize();
            await OnLoad();
            await OnLoadUI();
            await OnLoadCompleted();
            _isCompletedLoad = true;
        }
        protected virtual void Initialize()
        {
            _isDisposed = false;
        }
        protected virtual UniTask OnLoad()
        {
            return UniTask.CompletedTask;
        }
        protected virtual async UniTask OnLoadUI()
        {
            await UIManager.Instance.PrepareSection();
        }
        protected virtual UniTask OnLoadCompleted()
        {
            return UniTask.CompletedTask;
        }
        public virtual void Dispose()
        {
            UIManager.Instance.Clear();
            if (_isDisposed)
                return;
            _isDisposed = true;
        }
        protected virtual void OnUpdate()
        {
        }

        protected virtual void AnalyseSectionParameters(object parameter = null) {}

        protected virtual void Update()
        {
            if (_isCompletedLoad && !_isDisposed)
            {
                OnUpdate();
            }
        }
    }
}
