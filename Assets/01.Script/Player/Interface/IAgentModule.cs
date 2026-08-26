using System;

namespace _01.Script.Player.Components {
    public interface IAgentModule {
        Type Type => GetType();
        bool ShouldRegister => true;
        void Initialize(Agent owner);
    }
}