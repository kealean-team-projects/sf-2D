using System;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine.UIElements;

namespace csiimnida.CSILib.SoundManager.Editor {
    public class SoundItemUI {
        public SoundSo SoundItem;
        private readonly Button _deleteBtn;
        private readonly Label _nameLabel;
        private readonly VisualElement _rootElement;

        public SoundItemUI(VisualElement root, SoundSo item) {
            SoundItem = item;
            _rootElement = root.Q<VisualElement>("SoundItem");
            _nameLabel = _rootElement.Q<Label>("SoundName");
            _deleteBtn = _rootElement.Q<Button>("DeleteBtn");

            _deleteBtn.RegisterCallback<ClickEvent>(evt => {
                OnDeleteEvent?.Invoke(this);
                evt.StopPropagation();
            });

            _rootElement.RegisterCallback<ClickEvent>(evt => {
                OnSelectEvent?.Invoke(this);
                evt.StopPropagation();
            });
        }

        public string Name {
            get => _nameLabel.text;
            set => _nameLabel.text = value;
        }

        public bool IsActive {
            get => _rootElement.ClassListContains("active");
            set => _rootElement.EnableInClassList("active", value);
        }

        public event Action<SoundItemUI> OnDeleteEvent;
        public event Action<SoundItemUI> OnSelectEvent;
    }
}