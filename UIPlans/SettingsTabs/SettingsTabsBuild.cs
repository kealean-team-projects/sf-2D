using System;
using System.IO;
using System.Linq;
using System.Reflection;
using _02._Script.UI.Forest;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SettingsTabsBuild {
    const string Path = "Assets/Resources/ForestUI/ForestSettings.prefab";
    static readonly Color TextColor = new Color(.89f,.92f,.83f);
    static readonly Color Muted = new Color(.57f,.66f,.49f);
    static readonly Color Accent = new Color(.69f,.85f,.48f);
    static TMP_Text template;
    static RectTransform Rect(Transform t, float x, float y, float w, float h) {
        var r=(RectTransform)t;
        r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h);
        r.localScale=Vector3.one; r.localRotation=Quaternion.identity;
        return r;
    }
    static RectTransform Node(string name, Transform parent,float x,float y,float w,float h) {
        var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
        return Rect(go.transform,x,y,w,h);
    }
    static TMP_Text Label(Transform parent,string name,string text,float x,float y,float w,float h,float size=24) {
        var label=Object.Instantiate(template,parent); label.name=name;
        label.gameObject.SetActive(true); Rect(label.transform,x,y,w,h);
        label.text=text; label.fontSize=size; label.enableAutoSizing=false;
        label.alignment=TextAlignmentOptions.MidlineLeft;
        label.color=TextColor; label.raycastTarget=false;
        label.overflowMode=TextOverflowModes.Ellipsis;
        return label;
    }
    static Image Line(Transform parent,string name,float x,float y,float w,float h,Color color) {
        var r=Node(name,parent,x,y,w,h);
        var image=r.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false;
        return image;
    }
    static void Move(Transform t,Transform parent,float x,float y,float w,float h) {
        t.SetParent(parent,false); t.gameObject.SetActive(true); Rect(t,x,y,w,h);
    }
    static void SliderRow(Transform page,Slider slider,TMP_Text value,string label,float y) {
        Label(page,label+" Label",label,0,y,310,48);
        Move(slider.transform,page,340,y+9,510,30);
        Move(value.transform,page,870,y,100,48);
        foreach(var childName in new[]{"Track","Fill Area","Handle Area"}) {
            var child=slider.transform.Find(childName) as RectTransform;
            if(child==null) continue;
            child.anchorMin=new Vector2(0,.5f); child.anchorMax=new Vector2(1,.5f);
            child.pivot=new Vector2(.5f,.5f); child.anchoredPosition=Vector2.zero;
            child.sizeDelta=new Vector2(childName=="Track"?0:-24,childName=="Handle Area"?30:10);
        }
        value.fontSize=24; value.alignment=TextAlignmentOptions.MidlineRight;
    }
    static Button Tab(Button source, Transform panel, string name,float x) {
        var b=Object.Instantiate(source,panel); b.name=name+" Tab"; b.gameObject.SetActive(true);
        b.onClick=new Button.ButtonClickedEvent(); Rect(b.transform,x,130,310,48);
        var text=b.GetComponentInChildren<TMP_Text>(true);
        text.text=name; text.fontSize=22; text.characterSpacing=6;
        return b;
    }
    static void KeyRow(Transform page,string action,string keys,float x,float y,float width=290) {
        Label(page,action,action,x,y,width,26,20);
        var bg=Line(page,action+" Key Background",x,y+30,width,34,new Color(.18f,.24f,.17f,.75f));
        var key=Label(bg.transform,action+" Keys",keys,8,0,width-16,34,20);
        key.alignment=TextAlignmentOptions.Center;
    }
    public static void Build() {
        var root=PrefabUtility.LoadPrefabContents(Path);
        try {
            var s=root.GetComponent<ForestSettings>();
            var panel=(RectTransform)s.panelFade.transform;
            template=s.volumeValue;
            foreach(Transform child in panel) child.gameObject.SetActive(false);
            panel.sizeDelta=new Vector2(1100,680);
            var heading=panel.Find("Settings Heading");
            var subtitle=panel.Find("Settings Subtitle");
            Move(heading,panel,70,28,800,58);
            heading.GetComponent<TMP_Text>().text="S E T T I N G S";
            heading.GetComponent<TMP_Text>().fontSize=38;
            Move(subtitle,panel,72,88,800,26);
            subtitle.GetComponent<TMP_Text>().text="CUSTOMIZE YOUR ADVENTURE";
            subtitle.GetComponent<TMP_Text>().fontSize=15;
            subtitle.GetComponent<TMP_Text>().color=Muted;
            Move(s.closeButton.transform,panel,1024,26,44,44);
            Move(s.backButton.transform,panel,626,588,190,54);
            s.backButton.GetComponentInChildren<TMP_Text>(true).text="CANCEL";
            Move(s.applyButton.transform,panel,840,588,190,54);
            s.backButton.GetComponentInChildren<TMP_Text>(true).fontSize=24;
            s.applyButton.GetComponentInChildren<TMP_Text>(true).fontSize=24;
            foreach(string n in new[]{"Top Leaves","Bottom Leaves"}) {
                var leaf=panel.Find(n); if(leaf!=null) Move(leaf,panel,n=="Top Leaves"?4:1032,n=="Top Leaves"?4:610,64,64);
            }
            Line(panel,"Header Rule",65,120,970,1,new Color(.5f,.65f,.4f,.3f));
            Line(panel,"Footer Rule",65,564,970,1,new Color(.5f,.65f,.4f,.3f));
            var footer=Label(panel,"Footer Note",@"A SMALL STEP
FOR A BIG ADVENTURE",70,592,420,46,14);
            footer.color=Muted; footer.characterSpacing=4;
            s.tabs=new Button[3]; s.tabPages=new GameObject[3]; s.tabIndicators=new Image[3];
            var titles=new[]{"AUDIO","DISPLAY","CONTROLS"};
            for(int i=0;i<3;i++) {
                s.tabs[i]=Tab(s.backButton,panel,titles[i],65+i*323);
                s.tabIndicators[i]=Line(panel,titles[i]+" Active",95+i*323,180,250,3,Accent);
                s.tabIndicators[i].enabled=i==0;
                s.tabPages[i]=Node(titles[i]+" Page",panel,65,210,970,330).gameObject;
            }
            var audio=s.tabPages[0].transform;
            s.bgmVolume=Object.Instantiate(s.volume,audio); s.bgmVolume.name="BGM Slider";
            s.sfxVolume=Object.Instantiate(s.volume,audio); s.sfxVolume.name="SFX Slider";
            s.bgmValue=Label(audio,"BGM Value","100%",870,100,100,48);
            s.sfxValue=Label(audio,"SFX Value","100%",870,200,100,48);
            SliderRow(audio,s.volume,s.volumeValue,"Master Volume",24);
            SliderRow(audio,s.bgmVolume,s.bgmValue,"BGM",114);
            SliderRow(audio,s.sfxVolume,s.sfxValue,"SFX",204);
            Label(audio,"Audio Help","SFX includes movement, interactions and UI sounds.",0,286,970,30,17).color=Muted;
            var display=s.tabPages[1].transform;
            SliderRow(display,s.brightness,s.brightnessValue,"Brightness",24);
            Label(display,"Resolution Label","Resolution",0,114,310,48);
            Move(s.resolution.transform,display,340,114,630,48);
            Label(display,"Fullscreen Label","Fullscreen",0,204,310,48);
            Move(s.fullscreen.transform,display,340,204,630,48);
            var checkbox=s.fullscreen.transform.Find("Checkbox");
            if(checkbox!=null) Rect(checkbox,0,8,32,32);
            var hitArea=s.fullscreen.transform.Find("Hit Area");
            if(hitArea!=null) Rect(hitArea,0,0,630,48);
            Move(s.fullscreenValue.transform,display,414,204,480,48);
            Label(display,"Display Help","Display changes are saved when you select APPLY.",0,286,970,30,17).color=Muted;
            var controls=s.tabPages[2].transform;
            Label(controls,"Movement Heading","MOVEMENT",0,0,290,32,22).color=Accent;
            Label(controls,"Wall Heading","WALL ACTIONS",340,0,290,32,22).color=Accent;
            Label(controls,"System Heading","INTERACTION",680,0,290,32,22).color=Accent;
            Line(controls,"Column Rule 1",317,0,1,326,Muted*.5f);
            Line(controls,"Column Rule 2",657,0,1,326,Muted*.5f);
            KeyRow(controls,"Move","A / D",0,42);
            KeyRow(controls,"Sprint","LEFT SHIFT",0,116);
            KeyRow(controls,"Jump","SPACE",0,190);
            KeyRow(controls,"Crouch","C",0,264);
            KeyRow(controls,"Climb up / down","W / S",340,42);
            KeyRow(controls,"Wall jump","A or D + SPACE",340,116);
            KeyRow(controls,"Wall dash upward","W + SPACE",340,190);
            KeyRow(controls,"Release wall","S + SPACE",340,264);
            KeyRow(controls,"Interact","E",680,42);
            KeyRow(controls,"Settings / close","ESC",680,116);
            Label(controls,"Wall Help",@"Wall actions require
holding onto a wall.",680,218,290,80,18).color=Muted;
            for(int i=0;i<3;i++) s.tabPages[i].SetActive(i==0);
            s.modal.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,Path);
            AssetDatabase.SaveAssets();
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        Verify();
    }
    public static void Verify() {
        var scene=EditorSceneManager.NewPreviewScene();
        var menu=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ForestUI/ForestMainMenu.prefab"));
        var settings=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
        var cameraObject=new GameObject("Preview",typeof(Camera));
        var camera=cameraObject.GetComponent<Camera>(); camera.scene=scene;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
        camera.orthographic=true; camera.nearClipPlane=.1f; camera.farClipPlane=100; camera.cullingMask=1<<31;
        foreach(var root in new[]{menu,settings,cameraObject}) SceneManager.MoveGameObjectToScene(root,scene);
        foreach(var root in new[]{menu,settings}) {
            foreach(var c in root.GetComponentsInChildren<Component>(true)) if(c==null) throw new Exception("Missing script");
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
            foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)) {
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=10;
            }
        }
        var controller=settings.GetComponent<ForestSettings>(); controller.Initialize(null,true);
        var render=typeof(ForestUIInstaller).GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Static);
        try {
            controller.Open(menu.GetComponentsInChildren<CanvasGroup>(true).Single(g=>g.name=="Menu Foreground"));
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1920,1200)}) {
                for(int tab=0;tab<3;tab++) {
                    controller.tabs[tab].onClick.Invoke();
                    for(int i=0;i<3;i++) if(controller.tabPages[i].activeSelf!=(i==tab)) throw new Exception("Tab state");
                    render.Invoke(null,new object[]{camera,menu,settings,size,"settings-tab-"+tab});
                }
            }
            controller.ShowTab(0); controller.volume.value=.2f; controller.bgmVolume.value=.3f; controller.sfxVolume.value=.4f;
            controller.ShowTab(1); controller.brightness.value=.8f;
            controller.ShowTab(0);
            if(Mathf.Abs(controller.bgmVolume.value-.3f)>.001f) throw new Exception("Tab lost draft");
            controller.backButton.onClick.Invoke(); controller.Open();
            if(Mathf.Abs(controller.volume.value-.75f)>.001f || controller.bgmVolume.value!=1 || controller.brightness.value!=.5f)
                throw new Exception("Cancel regression");
            controller.bgmVolume.value=.6f; controller.sfxVolume.value=.7f;
            controller.applyButton.onClick.Invoke(); controller.Open();
            if(Mathf.Abs(controller.bgmVolume.value-.6f)>.001f || Mathf.Abs(controller.sfxVolume.value-.7f)>.001f)
                throw new Exception("Apply regression");
            bool closed=false; controller.Closed+=()=>closed=true; controller.Escape();
            if(!closed || controller.IsOpen) throw new Exception("Escape regression");
            File.WriteAllText("settings-tabs-check.txt",@"PASS: all three tabs; draft preserved across tabs; audio/display Cancel; audio Apply; ESC Closed event; no missing scripts; six renders.
");
        } finally {
            Object.DestroyImmediate(menu); Object.DestroyImmediate(settings); Object.DestroyImmediate(cameraObject);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}



