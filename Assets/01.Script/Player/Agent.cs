using System;
using System.Collections.Generic;
using System.Linq;
using _01.Script.Player.Interface;
using UnityEngine;

namespace _01.Script.Player {
    public abstract class Agent : MonoBehaviour, IDisposable {
        private List<IDisposable> _disposables;
        private bool _disposed;
        private bool _initialized;
        private Dictionary<Type, IAgentModule> _modules;

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            foreach (var disposable in _disposables) disposable.Dispose();

            _disposables.Clear();
            _disposables = null;
            OnDispose();
        }

        protected virtual void OnDispose() { }

        public void Initialize() {
            if (_initialized) return;
            _disposables = new List<IDisposable>();
            InitializeDictionary();
            _initialized = true;
            Afterinitialize();
        }

        protected virtual void Afterinitialize() { }

        private void InitializeDictionary() {
            var modules = GetComponentsInChildren<IAgentModule>();
            foreach (var module in modules) module.Initialize(this);
            _modules = modules.ToDictionary(m => m.Type);
        }

        protected T GetModule<T>() where T : class {
            if (_modules.TryGetValue(typeof(T), out var module)) return module as T;
            Debug.LogError($"Module {typeof(T)} not found)");
            return null;
        }
    }
}