// Test actual PlayerAudio logic with controlled physics/audio dependencies.
using System;
using System.Collections.Generic;
using System.Reflection;
using _02._Script._01_Players;
using _02._Script._01_Players.Components;
using _02._Script._01_Players.Components.Audio;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

class Program {
    static void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
    static void Call(object target,string method) => target.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);
    static void Expect(bool value,string message) { if(!value) throw new Exception(message); }
    static void Main() {
        var audio=new PlayerAudio(); var mover=new Mover(); var body=new Rigidbody2D();
        var player=new Player { Mover=mover }; var manager=new SoundManager();
        Set(audio,"player",player); Set(audio,"mover",mover); Set(audio,"body",body); Set(audio,"_manager",manager);
        var ground=new Collider2D(); mover.Surface=ground;
        void Step(bool grounded,float y) { mover.IsGround=grounded; mover.GroundCollider=grounded?ground:null; body.position=new Vector2(0,y); Call(audio,"OnGroundUpdated"); }
        Step(true,2);
        for(int i=1;i<=6;i++) Step(false,2-i*.1f);
        Expect(manager.Played.Count==0,"Must not predict landing while a distant floor is visible");
        Step(true,1.3f);
        Expect(manager.Played.Count==1 && manager.Played[0]=="Landing","Short fall must play at ground transition");
        for(int i=0;i<10;i++) Step(true,1.3f);
        Expect(manager.Played.Count==1,"Standing must not replay landing");
        ground.Material=new FootstepSurface { Material=FootstepMaterial.Stone };
        for(int i=1;i<=6;i++) Step(false,1.3f-i*.1f);
        Step(true,.6f);
        Expect(manager.Played.Count==2 && manager.Played[1]=="Landing_2","Stone landing must choose stone clip");
        Step(false,.59f); Step(true,.58f);
        Expect(manager.Played.Count==2,"Single-step contact flicker must stay silent");
        for(int i=1;i<=6;i++) Step(false,.58f-i*.1f);
        mover.RestoreVersion++; Step(true,2);
        Expect(manager.Played.Count==2,"Respawn must not trigger landing");
        Console.WriteLine("PASS: no airborne prediction; short-fall playback; one sound per landing; stone routing; contact jitter and respawn suppressed.");
    }
}
namespace UnityEngine {
    public class MonoBehaviour {
        protected T GetComponent<T>() where T:class=>null;
        protected T GetComponentInChildren<T>() where T:class=>null;
        protected static T FindAnyObjectByType<T>() where T:class=>null;
    }
    public class DisallowMultipleComponent:Attribute { }
    public class RequireComponent:Attribute { public RequireComponent(Type t) { } }
    public class SerializeField:Attribute { }
    public class HeaderAttribute:Attribute { public HeaderAttribute(string s) { } }
    public class TooltipAttribute:Attribute { public TooltipAttribute(string s) { } }
    public class MinAttribute:Attribute { public MinAttribute(float v) { } }
    public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
    public class Rigidbody2D { public Vector2 position,linearVelocity; }
    public class Collider2D { public object Material; public T GetComponentInParent<T>() where T:class=>Material as T; }
    public class AudioSource { public bool isPlaying=true; public void Pause(){isPlaying=false;} public void UnPause(){isPlaying=true;} }
    public static class AudioListener { public static bool pause; }
    public static class Time { public static float timeScale=1,fixedDeltaTime=.02f,unscaledTime; }
    public static class Mathf { public static float Abs(float v)=>Math.Abs(v); }
}
namespace _02._Script._01_Players {
    public class Player { public bool IsDead,IsClimb,IsMoving; public bool CanMove=true; public Mover Mover; public Sprint SprintControl=new Sprint(); }
    public class Sprint { public bool IsSprinting; }
}
namespace _02._Script._01_Players.Components {
    public class Mover { public bool IsGround; public int RestoreVersion; public Vector2 SoundSurfacePoint; public Collider2D GroundCollider,Surface; public event Action GroundUpdated; public Collider2D FindSoundSurface(float extra)=>Surface; }
}
namespace _02._Script._01_Players.Components.Audio {
    public enum FootstepMaterial { Grass,Stone }
    public class FootstepSurface { public FootstepMaterial Material; public FootstepMaterial MaterialAt(Vector2 point) => Material; }
}
namespace csiimnida.CSILib.SoundManager.RunTime {
    public class SoundManager {
        public List<string> Played=new List<string>();
        public AudioSource PlayTrackedSound(string name) { Played.Add(name); return new AudioSource(); }
        public void StopSound(AudioSource source) {source.isPlaying=false;}
    }
}
