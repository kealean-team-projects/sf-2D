using System;

namespace _01.Script.Player.Interface {
    public interface IAgentModule {
        Type Type => GetType();
        bool ShouldRegister => true;
        void Initialize(Agent owner);
    }
}