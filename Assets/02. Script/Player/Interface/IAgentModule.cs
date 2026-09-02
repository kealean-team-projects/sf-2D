using System;

namespace _02._Script.Player.Interface {
    public interface IAgentModule {
        Type Type => GetType();
        bool ShouldRegister => true;
        void Initialize(Agent owner);
    }
}