# Dragon Guardian layered PSB

## Files

- `DragonGuardian_Layered_SegmentedTail.psb`: 11 visible layers (head, body, four legs, five overlapping tail sections). Use this for simple cutout tail animation.
- `DragonGuardian_Layered_FullTail.psb`: 7 visible layers (head, body, four legs, one continuous tail). Use this if you plan to deform the tail with a mesh and weights.
- `assembled_preview_segmented_tail.png`: transparent reference image.
- `layout.json`: suggested canvas-space joint pivots and crop rectangles.

Both PSBs are 2172 × 724 RGBA. Each movable part contains a small painted overlap beneath its neighboring part to reduce gaps at the joints. Use one PSB version at a time.

## Unity

The sf-2D project already includes 2D PSD Importer and 2D Animation. For the chosen PSB, set Texture Type to Sprite, Sprite Mode to Multiple, and Import Mode to Individual Sprites (Mosaic). Enable Use as Rig to generate an assembled character prefab. Set Pixels Per Unit to the project's preferred scale (100 is the source suggestion). Use the Skinning Editor to create the bone hierarchy and mesh weights.

These files contain separated art layers and pivot suggestions. They do **not** contain bones, meshes, weights, or animation clips yet.

Validation: Photoshop 2026 opened the segmented PSB with 11 layers and the full-tail PSB with 7 layers. The segmented PSB was composited to PNG and visually checked. Unity's PSD Importer generated `.meta` entries for all 11 segmented layers and all 7 full-tail layers. No rig weights were generated.
