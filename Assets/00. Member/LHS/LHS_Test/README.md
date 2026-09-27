# Dragon Guardian layered PSB

## Files

- `DragonGuardian_Layered_SegmentedTail.psb`: 11 visible layers (head, body, four legs, five overlapping tail sections). This version has a Unity Skinning Editor rig.
- `DragonGuardian_Layered_SegmentedTail.psb.meta`: bone hierarchy, meshes, and vertex weights for the segmented version. Keep this file with the PSB when moving the asset.
- `DragonGuardian_Layered_FullTail.psb`: 7 visible layers (head, body, four legs, one continuous tail). Use this if you plan to deform the tail with a mesh and weights.
- `assembled_preview_segmented_tail.png`: transparent reference image.
- `layout.json`: suggested canvas-space joint pivots and crop rectangles.
- `Editor/DragonGuardianSkinningSetup.cs`: source for rebuilding the segmented rig through **Tools > LHS Test > Rebuild Dragon Guardian Skinning**.

Both PSBs are 2172 × 724 RGBA. Each movable part contains a small painted overlap beneath its neighboring part to reduce gaps at the joints. Use one PSB version at a time.

## Unity

The sf-2D project includes 2D PSD Importer and 2D Animation. Select `DragonGuardian_Layered_SegmentedTail.psb`, open Sprite Editor, then switch to Skinning Editor to inspect or adjust the rig. Rig version 3 has 35 bones and custom meshes with 2,378 weighted vertices across 11 sprites. The torso section painted into each limb layer is weighted to the body bone; the actual limb transitions into its shoulder, elbow, wrist, and claw bones. The five tail sprites have 11 connected tail bones. Its importer is set for multiple sprites, Mosaic, and Use as Rig. The source suggestion is 100 pixels per unit.

For a sharper Unity import, the segmented PSB uses uncompressed RGBA32, a 4096 maximum texture size, and no mipmaps. Unity reports an imported 1024 × 512 RGBA32 atlas with one mip level. This prevents extra import blur/compression, but close zooms are still limited by the original painted pixels: for example, the near foreleg cutout is only 99 × 197 pixels. A truly higher-resolution in-game close-up needs new source artwork, not just an importer setting or enlarged canvas.

The full-tail PSB is an unrigged alternative. Neither version includes animation clips.

Validation: Photoshop 2026 opened the segmented PSB with 11 layers and the full-tail PSB with 7 layers. The segmented PSB was composited to PNG and visually checked. Unity's importer completed rig version 3 and reported 35 bones, 2,378 weighted vertices, and the 1024 × 512 RGBA32 atlas; the corresponding bone, mesh, and weight records are present in the PSB `.meta` file. Animation poses have not been visually reviewed.
