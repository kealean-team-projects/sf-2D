using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.U2D.Animation;
using UnityEditor.U2D.PSD;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;

// Kept with the PSB so its bind pose can be regenerated from the same source art.
// The version marker makes importer upgrades reproducible; the menu can rerun it.
[InitializeOnLoad]
internal static class DragonGuardianSkinningSetup
{
    const string Root = "Assets/00. Member/LHS/LHS_Test";
    const string Psb = Root + "/DragonGuardian_Layered_SegmentedTail.psb";
    const string Report = Root + "/rig_result.txt";
    const string Version = "Version: 3";
    const float CanvasHeight = 724f;

    sealed class BoneSpec
    {
        public SpriteBone data;
        public Vector2 start;
        public float worldAngle;
    }

    static readonly List<BoneSpec> Bones = new List<BoneSpec>();

    static DragonGuardianSkinningSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Report) || !File.ReadAllText(Report).Contains(Version)) Run();
        };
    }

    [MenuItem("Tools/LHS Test/Rebuild Dragon Guardian Skinning")]
    static void Run()
    {
        try
        {
            Build();
        }
        catch (Exception exception)
        {
            File.WriteAllText(Root + "/rig_error.txt", exception.ToString());
            Debug.LogException(exception);
        }
    }

    static string StableGuid(string name)
    {
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("LHS_DragonGuardian_v1/" + name));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    static Vector2 Point(float x, float topY) => new Vector2(x, CanvasHeight - topY);

    static int Add(string name, int parent, Vector2 start, Vector2 end, Color32 color)
    {
        float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
        Vector2 localPosition = start;
        float localAngle = angle;
        if (parent >= 0)
        {
            BoneSpec p = Bones[parent];
            localPosition = Quaternion.Euler(0, 0, -p.worldAngle) * (start - p.start);
            localAngle -= p.worldAngle;
        }
        var bone = new SpriteBone
        {
            name = name,
            guid = StableGuid(name),
            parentId = parent,
            position = localPosition,
            rotation = Quaternion.Euler(0, 0, localAngle),
            length = Vector2.Distance(start, end),
            color = color
        };
        Bones.Add(new BoneSpec { data = bone, start = start, worldAngle = angle });
        return Bones.Count - 1;
    }

    static void MakeSkeleton()
    {
        Bones.Clear();
        Color32 spine = new Color32(68, 180, 218, 255);
        Color32 head = new Color32(207, 182, 108, 255);
        Color32 tail = new Color32(131, 169, 235, 255);
        Color32 limb = new Color32(134, 208, 167, 255);
        Add("body_root", -1, Point(250, 355), Point(450, 355), spine);      // 0
        Add("body_mid", 0, Point(450, 355), Point(670, 355), spine);        // 1
        Add("body_hip", 1, Point(670, 355), Point(880, 355), spine);        // 2
        Add("neck", 0, Point(250, 355), Point(185, 352), head);             // 3
        Add("head", 3, Point(185, 352), Point(65, 340), head);              // 4
        Add("tail_01", 2, Point(880, 355), Point(1110, 355), tail);         // 5
        Add("tail_02", 5, Point(1110, 355), Point(1350, 356), tail);        // 6
        Add("tail_03", 6, Point(1350, 356), Point(1580, 358), tail);        // 7
        Add("tail_04", 7, Point(1580, 358), Point(1810, 359), tail);        // 8
        Add("tail_05", 8, Point(1810, 359), Point(2010, 355), tail);        // 9
        Add("tail_tip", 9, Point(2010, 355), Point(2150, 365), tail);       // 10
        Add("front_far_upper", 0, Point(312, 370), Point(273, 427), limb); // 11
        Add("front_far_lower", 11, Point(273, 427), Point(235, 482), limb);// 12
        Add("front_near_upper", 0, Point(347, 365), Point(352, 424), limb);// 13
        Add("front_near_lower", 13, Point(352, 424), Point(315, 506), limb);// 14
        Add("rear_far_upper", 2, Point(814, 369), Point(808, 412), limb);  // 15
        Add("rear_far_lower", 15, Point(808, 412), Point(806, 441), limb); // 16
        Add("rear_near_upper", 2, Point(863, 368), Point(881, 413), limb); // 17
        Add("rear_near_lower", 17, Point(881, 413), Point(884, 446), limb);// 18
    }

    static int[] BoneSet(string name)
    {
        switch (name)
        {
            case "head": return new[] { 3, 4 };
            case "body": return new[] { 0, 1, 2, 5 };
            case "front_far": return new[] { 11, 12 };
            case "front_near": return new[] { 13, 14 };
            case "rear_far": return new[] { 15, 16 };
            case "rear_near": return new[] { 17, 18 };
            case "tail_01": return new[] { 5, 6 };
            case "tail_02": return new[] { 6, 7 };
            case "tail_03": return new[] { 7, 8 };
            case "tail_04": return new[] { 8, 9 };
            case "tail_05": return new[] { 9, 10 };
            default: throw new InvalidOperationException("Unexpected layer " + name);
        }
    }

    static SpriteBone[] LocalBones(int[] selected, Vector2 spritePosition)
    {
        var result = new SpriteBone[selected.Length];
        for (int i = 0; i < selected.Length; i++)
        {
            SpriteBone bone = Bones[selected[i]].data;
            int localParent = Array.IndexOf(selected, bone.parentId);
            if (localParent < 0)
            {
                bone.position = Bones[selected[i]].start - spritePosition;
                bone.rotation = Quaternion.Euler(0, 0, Bones[selected[i]].worldAngle);
            }
            bone.parentId = localParent;
            result[i] = bone;
        }
        return result;
    }

    static BoneWeight Weight(int left, int right, float rightFraction)
    {
        rightFraction = Mathf.Clamp01(rightFraction);
        return new BoneWeight
        {
            boneIndex0 = left,
            boneIndex1 = right,
            weight0 = 1f - rightFraction,
            weight1 = rightFraction
        };
    }

    static BoneWeight BlendByX(float x, float[] centers)
    {
        if (centers.Length == 2)
            return Weight(0, 1, Mathf.InverseLerp(centers[0], centers[1], x));
        for (int i = 0; i < centers.Length - 1; i++)
            if (x <= centers[i + 1])
                return Weight(i, i + 1, Mathf.InverseLerp(centers[i], centers[i + 1], x));
        return Weight(centers.Length - 1, centers.Length - 1, 0);
    }

    static BoneWeight GetWeight(string name, Vector2 canvasPoint)
    {
        float x = canvasPoint.x;
        float topY = CanvasHeight - canvasPoint.y;
        switch (name)
        {
            case "head": return Weight(1, 0, Mathf.InverseLerp(105, 230, x));
            case "body": return BlendByX(x, new[] { 250f, 460f, 680f, 905f });
            case "tail_01": return BlendByX(x, new[] { 885f, 1120f });
            case "tail_02": return BlendByX(x, new[] { 1120f, 1360f });
            case "tail_03": return BlendByX(x, new[] { 1360f, 1590f });
            case "tail_04": return BlendByX(x, new[] { 1590f, 1820f });
            case "tail_05": return BlendByX(x, new[] { 1820f, 2030f });
            case "front_far": return Weight(0, 1, Mathf.InverseLerp(375, 455, topY));
            case "front_near": return Weight(0, 1, Mathf.InverseLerp(373, 470, topY));
            case "rear_far": return Weight(0, 1, Mathf.InverseLerp(375, 425, topY));
            case "rear_near": return Weight(0, 1, Mathf.InverseLerp(375, 428, topY));
            default: throw new InvalidOperationException(name);
        }
    }

    static void GridSize(string name, out int columns, out int rows)
    {
        if (name == "body") { columns = 28; rows = 4; }
        else if (name.StartsWith("tail_")) { columns = 9; rows = 3; }
        else if (name == "head") { columns = 9; rows = 4; }
        else { columns = 4; rows = 10; }
    }

    static void MakeSkeletonV2()
    {
        Bones.Clear();
        Color32 spine = new Color32(68, 180, 218, 255);
        Color32 headColor = new Color32(207, 182, 108, 255);
        Color32 tailColor = new Color32(131, 169, 235, 255);
        Color32 limb = new Color32(134, 208, 167, 255);
        Vector2[] body = { Point(250,355), Point(360,355), Point(470,355),
            Point(580,355), Point(690,355), Point(790,355), Point(880,355) };
        for (int i = 0; i < body.Length - 1; i++)
            Add(i == 0 ? "body_root" : "body_0" + i, i - 1, body[i], body[i+1], spine);

        Add("neck_01", 0, Point(250,355), Point(215,353), headColor); // 6
        Add("neck_02", 6, Point(215,353), Point(185,352), headColor); // 7
        Add("head_01", 7, Point(185,352), Point(120,345), headColor); // 8
        Add("head_02", 8, Point(120,345), Point(65,340), headColor);  // 9

        Vector2[] tailPoints = { Point(880,355), Point(1000,355), Point(1110,355),
            Point(1230,356), Point(1350,356), Point(1465,357), Point(1580,358),
            Point(1695,359), Point(1810,359), Point(1910,357), Point(2010,355),
            Point(2150,365) };
        string[] tailNames = { "tail_01_root", "tail_01_mid", "tail_02_root",
            "tail_02_mid", "tail_03_root", "tail_03_mid", "tail_04_root",
            "tail_04_mid", "tail_05_root", "tail_05_mid", "tail_tip" };
        for (int i = 0; i < tailNames.Length; i++)
            Add(tailNames[i], i == 0 ? 5 : 9 + i, tailPoints[i], tailPoints[i+1], tailColor);

        Add("front_far_upper", 0, Point(312,370), Point(276,417), limb);  // 21
        Add("front_far_elbow", 21, Point(276,417), Point(251,452), limb); // 22
        Add("front_far_wrist", 22, Point(251,452), Point(235,474), limb); // 23
        Add("front_far_claw", 23, Point(235,474), Point(221,489), limb);  // 24
        Add("front_near_upper", 0, Point(347,365), Point(352,420), limb); // 25
        Add("front_near_elbow", 25, Point(352,420), Point(333,465), limb);// 26
        Add("front_near_wrist", 26, Point(333,465), Point(310,498), limb);// 27
        Add("front_near_claw", 27, Point(310,498), Point(296,517), limb); // 28
        Add("rear_far_upper", 5, Point(814,369), Point(808,408), limb);   // 29
        Add("rear_far_lower", 29, Point(808,408), Point(808,430), limb);  // 30
        Add("rear_far_claw", 30, Point(808,430), Point(806,448), limb);   // 31
        Add("rear_near_upper", 5, Point(863,368), Point(881,408), limb);  // 32
        Add("rear_near_lower", 32, Point(881,408), Point(884,430), limb); // 33
        Add("rear_near_claw", 33, Point(884,430), Point(884,448), limb);  // 34
    }

    static int[] BoneSetV2(string name)
    {
        switch (name)
        {
            case "head": return new[] { 0, 6, 7, 8, 9 };
            case "body": return new[] { 0, 1, 2, 3, 4, 5, 10 };
            case "front_far": return new[] { 0, 21, 22, 23, 24 };
            case "front_near": return new[] { 0, 25, 26, 27, 28 };
            case "rear_far": return new[] { 5, 29, 30, 31 };
            case "rear_near": return new[] { 5, 32, 33, 34 };
            case "tail_01": return new[] { 10, 11, 12 };
            case "tail_02": return new[] { 12, 13, 14 };
            case "tail_03": return new[] { 14, 15, 16 };
            case "tail_04": return new[] { 16, 17, 18 };
            case "tail_05": return new[] { 18, 19, 20 };
            default: throw new InvalidOperationException("Unexpected layer " + name);
        }
    }

    static BoneWeight GetWeightV2(string name, Vector2 canvasPoint)
    {
        float x = canvasPoint.x;
        float y = CanvasHeight - canvasPoint.y;
        switch (name)
        {
            case "head": return BlendByX(-x, new[] { -250f, -215f, -185f, -120f, -65f });
            case "body": return BlendByX(x, new[] { 250f, 360f, 470f, 580f, 690f, 790f, 880f });
            case "tail_01": return BlendByX(x, new[] { 880f, 1000f, 1110f });
            case "tail_02": return BlendByX(x, new[] { 1110f, 1230f, 1350f });
            case "tail_03": return BlendByX(x, new[] { 1350f, 1465f, 1580f });
            case "tail_04": return BlendByX(x, new[] { 1580f, 1695f, 1810f });
            case "tail_05": return BlendByX(x, new[] { 1810f, 1910f, 2010f });
            // At each shoulder the torso pattern belongs to the body bone.
            // Only the actual limb starts blending into the moving bones.
            case "front_far": return BlendByX(y, new[] { 374f, 402f, 435f, 465f, 490f });
            case "front_near": return BlendByX(y, new[] { 378f, 404f, 442f, 480f, 510f });
            case "rear_far": return BlendByX(y, new[] { 381f, 399f, 423f, 445f });
            case "rear_near": return BlendByX(y, new[] { 382f, 400f, 423f, 445f });
            default: throw new InvalidOperationException(name);
        }
    }

    static void GridSizeV2(string name, out int columns, out int rows)
    {
        if (name == "body") { columns = 48; rows = 8; }
        else if (name.StartsWith("tail_")) { columns = 18; rows = 5; }
        else if (name == "head") { columns = 18; rows = 8; }
        else if (name.StartsWith("front_")) { columns = 12; rows = 26; }
        else { columns = 12; rows = 18; }
    }

    static void MakeMesh(string name, SpriteRect rect, CharacterPart part,
        ISpriteMeshDataProvider meshProvider)
    {
        GridSizeV2(name, out int columns, out int rows);
        float width = rect.rect.width;
        float height = rect.rect.height;
        var vertices = new Vertex2DMetaData[(columns + 1) * (rows + 1)];
        Vector2 partPosition = part.spritePosition.position;
        for (int row = 0; row <= rows; row++)
        for (int col = 0; col <= columns; col++)
        {
            Vector2 position = new Vector2(width * col / columns, height * row / rows);
            int i = row * (columns + 1) + col;
            vertices[i] = new Vertex2DMetaData
            {
                position = position,
                boneWeight = GetWeightV2(name, partPosition + position)
            };
        }
        var indices = new List<int>(columns * rows * 6);
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < columns; col++)
        {
            int a = row * (columns + 1) + col;
            int b = a + 1;
            int c = a + columns + 1;
            int d = c + 1;
            indices.AddRange(new[] { a, b, c, b, d, c });
        }
        var edges = new List<Vector2Int>();
        for (int c = 0; c < columns; c++)
        {
            edges.Add(new Vector2Int(c, c + 1));
            int top = rows * (columns + 1);
            edges.Add(new Vector2Int(top + c, top + c + 1));
        }
        for (int r = 0; r < rows; r++)
        {
            int a = r * (columns + 1);
            int b = (r + 1) * (columns + 1);
            edges.Add(new Vector2Int(a, b));
            edges.Add(new Vector2Int(a + columns, b + columns));
        }
        meshProvider.SetVertices(rect.spriteID, vertices);
        meshProvider.SetIndices(rect.spriteID, indices.ToArray());
        meshProvider.SetEdges(rect.spriteID, edges.ToArray());
    }

    static void Build()
    {
        var importer = AssetImporter.GetAtPath(Psb) as PSDImporter;
        if (importer == null) throw new InvalidOperationException("PSD Importer is unavailable for " + Psb);
        importer.useMosaicMode = true;
        importer.useCharacterMode = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        var defaultPlatform = new TextureImporterPlatformSettings
        {
            name = "DefaultTexturePlatform",
            overridden = true,
            maxTextureSize = 4096,
            format = TextureImporterFormat.RGBA32,
            textureCompression = TextureImporterCompression.Uncompressed
        };
        importer.SetImporterPlatformSettings(defaultPlatform);
        var activePlatform = importer.GetImporterPlatformSettings(EditorUserBuildSettings.activeBuildTarget);
        activePlatform.overridden = true;
        activePlatform.maxTextureSize = 4096;
        activePlatform.format = TextureImporterFormat.RGBA32;
        activePlatform.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetImporterPlatformSettings(activePlatform);
        var provider = (ISpriteEditorDataProvider)importer;
        provider.InitSpriteEditorDataProvider();
        var boneProvider = provider.GetDataProvider<ISpriteBoneDataProvider>();
        var meshProvider = provider.GetDataProvider<ISpriteMeshDataProvider>();
        var characterProvider = provider.GetDataProvider<ICharacterDataProvider>();
        if (boneProvider == null || meshProvider == null || characterProvider == null)
            throw new InvalidOperationException("Unity 2D skinning data providers are unavailable");
        MakeSkeletonV2();
        CharacterData character = characterProvider.GetCharacterData();
        SpriteRect[] sprites = provider.GetSpriteRects();
        if (sprites.Length != 11 || character.parts.Length != 11)
            throw new InvalidOperationException("Expected 11 sprites and parts, got " + sprites.Length + " / " + character.parts.Length);
        var parts = character.parts.ToDictionary(p => p.spriteId);
        foreach (SpriteRect sprite in sprites)
        {
            string id = sprite.spriteID.ToString();
            if (!parts.TryGetValue(id, out CharacterPart part))
                throw new InvalidOperationException("No character part for " + sprite.name);
            int[] selected = BoneSetV2(sprite.name);
            boneProvider.SetBones(sprite.spriteID, LocalBones(selected, part.spritePosition.position).ToList());
            MakeMesh(sprite.name, sprite, part, meshProvider);
            part.bones = selected;
            parts[id] = part;
        }
        character.bones = Bones.Select(b => b.data).ToArray();
        character.parts = character.parts.Select(p => parts[p.spriteId]).ToArray();
        characterProvider.SetCharacterData(character);
        provider.Apply();
        importer.SaveAndReimport();
        var check = (ISpriteEditorDataProvider)AssetImporter.GetAtPath(Psb);
        check.InitSpriteEditorDataProvider();
        if (check.GetDataProvider<ICharacterDataProvider>().GetCharacterData().bones.Length != 35)
            throw new InvalidOperationException("Rig did not preserve all 35 character bones");
        int weightedVertices = 0;
        foreach (SpriteRect sprite in check.GetSpriteRects())
        {
            var verts = check.GetDataProvider<ISpriteMeshDataProvider>().GetVertices(sprite.spriteID);
            var boneList = check.GetDataProvider<ISpriteBoneDataProvider>().GetBones(sprite.spriteID);
            if (verts.Length == 0 || boneList.Count == 0 ||
                verts.Any(v => v.boneWeight.weight0 + v.boneWeight.weight1 + v.boneWeight.weight2 + v.boneWeight.weight3 < 0.999f))
                throw new InvalidOperationException("Invalid skin weights in " + sprite.name);
            weightedVertices += verts.Length;
        }
        var importedSprites = AssetDatabase.LoadAllAssetsAtPath(Psb).OfType<Sprite>().ToArray();
        if (importedSprites.Length != 11)
            throw new InvalidOperationException("Expected 11 imported Sprite objects, got " + importedSprites.Length);
        Texture2D atlas = importedSprites[0].texture;
        string textureInfo = "Imported atlas: " + atlas.width + "x" + atlas.height +
            ", " + atlas.format + ", mip levels " + atlas.mipmapCount + ".\n";
        File.WriteAllText(Report, "Dragon Guardian segmented-tail PSB rig complete\n" + Version + "\n" +
            "Layers: 11\nBones: " + Bones.Count + "\nWeighted vertices: " + weightedVertices +
            "\nTorso pixels on limb sprites follow the body bone.\n" +
            "Mipmaps off; Default and active texture platforms uncompressed at up to 4096 px.\n" +
            textureInfo +
            "Mesh and weights are stored in the PSB .meta through Unity PSD Importer.\n");
        AssetDatabase.ImportAsset(Report);
        Debug.Log("Dragon Guardian rig complete: " + Bones.Count + " bones, " + weightedVertices + " weighted vertices");
    }
}
