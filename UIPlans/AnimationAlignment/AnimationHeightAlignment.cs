using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AnimationHeightAlignment {
    const string Folder="Assets/AnimationAlignment/";
    static readonly EditorCurveBinding Height=EditorCurveBinding.FloatCurve("Body_1",typeof(Transform),"m_LocalPosition.y");
    static AnimationClip Load(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+name+".anim");
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    static bool Near(float a,float b) => Mathf.Abs(a-b)<.00001f;
    static bool SameCurve(AnimationCurve a,AnimationCurve b) {
        if(a==null || b==null) return a==b;
        if(a.length!=b.length || a.preWrapMode!=b.preWrapMode || a.postWrapMode!=b.postWrapMode) return false;
        for(int i=0;i<a.length;i++) {
            var x=a.keys[i]; var y=b.keys[i];
            if(!Near(x.time,y.time) || !Near(x.value,y.value) ||
               !x.inTangent.Equals(y.inTangent) || !x.outTangent.Equals(y.outTangent) ||
               x.weightedMode!=y.weightedMode || x.inWeight!=y.inWeight || x.outWeight!=y.outWeight)
                return false;
        }
        return true;
    }
    public static void Run() {
        var report=new StringBuilder();
        var walk=Load("PlayerWalk");
        float reference=AnimationUtility.GetEditorCurve(walk,Height).Evaluate(0);
        var horizontal=EditorCurveBinding.FloatCurve("Body_1",typeof(Transform),"m_LocalPosition.x");
        float referenceX=AnimationUtility.GetEditorCurve(walk,horizontal).Evaluate(0);
        float importedRest=reference*.5f;
        Check(Near(reference,4.5987535f),"Walking baseline changed; re-evaluate conversion.");
        Check(AnimationUtility.GetEditorCurve(Load("PlayerJump"),Height).Evaluate(0)<importedRest+.3f,
            "Jump no longer uses the smaller import-space baseline.");
        Check(AnimationUtility.GetEditorCurve(Load("PlayerFall"),Height)==null,"Fall height already authored.");
        report.AppendLine("BEFORE: reproduced mixed baselines: Walk="+reference+", Jump=2.462, Fall has no root Y key.");
        foreach(string name in new[]{"PlayerJump","PlayerFall","PlayerCrawl","PlayerCrawlWalk","PlayerFallGround"}) {
            var clip=Load(name);
            var bindings=AnimationUtility.GetCurveBindings(clip);
            var unchanged=bindings.Where(b=>!b.Equals(Height)).ToDictionary(b=>b,b=>AnimationUtility.GetEditorCurve(clip,b));
            var old=AnimationUtility.GetEditorCurve(clip,Height);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);
            float length=clip.length;
            AnimationCurve corrected;
            if(old==null) {
                corrected=AnimationCurve.Constant(0,clip.length,reference);
            } else {
                // Old 100-PPU motion offsets must scale with the current 200-PPU rig,
                // while the approved Walk/Run standing height remains unchanged.
                // Jump was authored AFTER changing to 200 PPU; translate it only.
                float scale=name=="PlayerJump"?1f:.5f;
                float offset=importedRest;
                var keys=old.keys;
                for(int i=0;i<keys.Length;i++) {
                    keys[i].value=keys[i].value*scale+offset;
                    if(!float.IsInfinity(keys[i].inTangent)) keys[i].inTangent*=scale;
                    if(!float.IsInfinity(keys[i].outTangent)) keys[i].outTangent*=scale;
                }
                corrected=new AnimationCurve(keys) {preWrapMode=old.preWrapMode,postWrapMode=old.postWrapMode};
                // Preserve interpolation modes; no automatic smoothing/re-timing.
                for(int i=0;i<keys.Length;i++) {
                    AnimationUtility.SetKeyBroken(corrected,i,AnimationUtility.GetKeyBroken(old,i));
                    AnimationUtility.SetKeyLeftTangentMode(corrected,i,AnimationUtility.GetKeyLeftTangentMode(old,i));
                    AnimationUtility.SetKeyRightTangentMode(corrected,i,AnimationUtility.GetKeyRightTangentMode(old,i));
                }
            }
            AnimationUtility.SetEditorCurve(clip,Height,corrected);
            // Unity groups position channels; explicitly retain the standing X anchor.
            if(name=="PlayerFall") AnimationUtility.SetEditorCurve(clip,horizontal,AnimationCurve.Constant(0,length,referenceX));
            foreach(var pair in unchanged) Check(SameCurve(pair.Value,AnimationUtility.GetEditorCurve(clip,pair.Key)),
                name+": unrelated curve changed: "+pair.Key.path+"/"+pair.Key.propertyName);
            Check(Near(clip.length,length),name+": timing changed");
            Check(AnimationUtility.GetAnimationClipSettings(clip).loopTime==settings.loopTime,name+": loop changed");
            // Sample the actual Unity clip, not just the edited text.
            var rig=new GameObject("Animation height verification");
            var body=new GameObject("Body_1"); body.transform.SetParent(rig.transform,false);
            try {
                foreach(float t in new[]{0f,length*.25f,length*.5f,length*.75f,length}) {
                    body.transform.localPosition=new Vector3(.185f,importedRest,0);
                    clip.SampleAnimation(rig,t);
                    Check(Near(body.transform.localPosition.y,corrected.Evaluate(t)),name+": sampled height mismatch");
                    if(name=="PlayerFall") Check(Near(body.transform.localPosition.x,referenceX),"Fall X does not match standing anchor");
                }
            } finally { UnityEngine.Object.DestroyImmediate(rig); }
            report.AppendLine(name+": root Y at t=0 "+(old==null?"<missing>":old.Evaluate(0).ToString("F6"))+
                " -> "+corrected.Evaluate(0).ToString("F6")+"; other curves and timing unchanged; 5 Unity samples passed.");
            EditorUtility.SetDirty(clip);
        }
        Check(Near(AnimationUtility.GetEditorCurve(Load("PlayerFallGround"),Height).Evaluate(Load("PlayerFallGround").length),reference),
            "Landing recovery no longer matches Walk/Run.");
        AssetDatabase.SaveAssets();
        report.AppendLine("PASS: landing ends at Walk/Run height; no changes to Walk/Run or already-aligned Idle/Climb/Wall actions.");
        File.WriteAllText("animation-height-verification.txt",report.ToString());
    }
}
