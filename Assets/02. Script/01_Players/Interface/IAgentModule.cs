using System;

namespace _02._Script._01_Players.Interface {
    public interface IAgentModule {
        Type Type => GetType();
        bool ShouldRegister => true;
        void Initialize(Agent owner);
    }
}