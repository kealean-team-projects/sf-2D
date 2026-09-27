// Logic regression test against the actual Stats.cs. Engine/time dependencies are
// controlled doubles; this does not claim to exercise the Unity death animation.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using _02._Script._01_Players.Components.Stamina;
using Cysharp.Threading.Tasks;

internal static class Program {
    static void Set(Stats stats,string field,float value) => typeof(Stats).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(stats,value);
    static void Expect(float actual,float expected,string message) {
        if(Math.Abs(actual-expected)>.001f) throw new Exception(message+": "+actual+" != "+expected);
    }
    static void Main() {
        var stats=new Stats(); // MonoBehaviour is a plain test double in this harness only.
        Set(stats,"maxStamina",100); Set(stats,"staminaHealBoost",20); Set(stats,"staminaHealWaitTime",5);
        stats.Initialize(null);
        stats.UseStamina(80,true);
        stats.StaminaUpdate(true,false,false); // Starts the pre-death recovery delay.
        var oldDelay=UniTask.Delays.Dequeue();
        stats.RestoreStamina(75);
        Expect(stats.Stamina,75,"Restored checkpoint value");
        stats.StaminaUpdate(true,false,false);
        Expect(stats.Stamina,95,"Recovery must resume after restore");

        stats.UseStamina(15,true);
        stats.StaminaUpdate(true,false,false);
        var newDelay=UniTask.Delays.Dequeue();
        oldDelay.SetResult();
        stats.StaminaUpdate(true,false,false);
        Expect(stats.Stamina,80,"Old life delay must not unlock the new cooldown");
        newDelay.SetResult();
        stats.StaminaUpdate(true,false,false);
        Expect(stats.Stamina,100,"New cooldown releases recovery normally");
        stats.RestoreStamina(-10); Expect(stats.Stamina,0,"Lower clamp");
        stats.RestoreStamina(150); Expect(stats.Stamina,100,"Upper clamp");
        Console.WriteLine("PASS: saved value restore, recovery reset, stale delay isolation, new cooldown completion, bounds.");
    }
}

namespace UnityEngine {
    public class MonoBehaviour { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    public static class Time { public static float deltaTime=1; }
    public static class Mathf { public static float Clamp(float value,float min,float max) => Math.Clamp(value,min,max); }
}
namespace _02._Script._01_Players { public class Agent { } }
namespace _02._Script._01_Players.Components.DamageCompo { public class DamageModule { public void TakeDamage() { } } }
namespace _02._Script._05_Managers {
    public class EffectManager {
        public static EffectManager Instance=new EffectManager();
        public void setFilter(bool value) { }
    }
}
namespace Cysharp.Threading.Tasks {
    [AsyncMethodBuilder(typeof(TestTaskBuilder))]
    public readonly struct UniTask {
        private readonly Task task;
        public static readonly Queue<TaskCompletionSource> Delays=new Queue<TaskCompletionSource>();
        public UniTask(Task value) { task=value; }
        public TaskAwaiter GetAwaiter() => task.GetAwaiter();
        public static UniTask Delay(TimeSpan duration) {
            var completion=new TaskCompletionSource(); Delays.Enqueue(completion); return new UniTask(completion.Task);
        }
        public void Forget() { if(task.IsFaulted) task.GetAwaiter().GetResult(); }
    }
    public struct TestTaskBuilder {
        private AsyncTaskMethodBuilder builder;
        public static TestTaskBuilder Create() => new TestTaskBuilder { builder=AsyncTaskMethodBuilder.Create() };
        public UniTask Task => new UniTask(builder.Task);
        public void SetResult() => builder.SetResult();
        public void SetException(Exception error) => builder.SetException(error);
        public void SetStateMachine(IAsyncStateMachine machine) => builder.SetStateMachine(machine);
        public void Start<T>(ref T machine) where T:IAsyncStateMachine => builder.Start(ref machine);
        public void AwaitOnCompleted<TA,TS>(ref TA awaiter,ref TS machine) where TA:INotifyCompletion where TS:IAsyncStateMachine => builder.AwaitOnCompleted(ref awaiter,ref machine);
        public void AwaitUnsafeOnCompleted<TA,TS>(ref TA awaiter,ref TS machine) where TA:ICriticalNotifyCompletion where TS:IAsyncStateMachine => builder.AwaitUnsafeOnCompleted(ref awaiter,ref machine);
    }
}
