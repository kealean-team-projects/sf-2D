using System.Collections.Generic;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;
using UnityEngine;
using UnityEngine.AI;

namespace _02._Script.Boss
{
    // NavMeshPlus source provider: an XY flight area with Collider2D terrain cut out.
    public sealed class BossNavigation : NavMeshExtension
    {
        private readonly List<NavMeshBuildSource> sources = new();
        private readonly List<Mesh> meshes = new();
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private NavMeshPath path;
        private NavMeshQueryFilter query;
        private ContactFilter2D obstacleFilter;
        private Vector3[] corners = System.Array.Empty<Vector3>();
        private int corner;
        [SerializeField] private int agentType = -1;
        [SerializeField] private float radius;
        [SerializeField] private LayerMask obstacleLayers;
        private float nextRepath;
        private bool ready;
        private bool reportedMissingPath;
        [SerializeField] private Bounds flightBounds;

        public bool IsReady => ready;

        protected override void Awake()
        {
            base.Awake();
            if (!Application.isPlaying) return;
            ConfigureQuery();
            ready = NavMeshSurfaceOwner.navMeshData != null;
        }

        private void ConfigureQuery()
        {
            query = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
            query.SetAreaCost(0, 1f);
            path = new NavMeshPath();
            obstacleFilter.SetLayerMask(obstacleLayers);
            obstacleFilter.useTriggers = false;
        }

#if UNITY_EDITOR
        public NavMeshData Bake(BoxCollider2D area, Collider2D body, LayerMask layers, bool wholeMap = false)
        {
            Physics2D.SyncTransforms();
            // The boss is inactive before room entry, so Collider.bounds can be empty here.
            if (body is CircleCollider2D circle)
                radius = circle.radius * Mathf.Max(Mathf.Abs(body.transform.lossyScale.x),
                    Mathf.Abs(body.transform.lossyScale.y)) + 0.15f;
            else
                throw new System.InvalidOperationException("보스의 CircleCollider2D를 연결하세요.");
            obstacleLayers = layers;
            var settings = agentType != -1 ? NavMesh.GetSettingsByID(agentType) : NavMesh.CreateSettings();
            if (settings.agentTypeID == -1) settings = NavMesh.CreateSettings();
            agentType = settings.agentTypeID;
            settings.agentRadius = radius;
            settings.agentHeight = 1f;
            settings.agentClimb = 0f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = Mathf.Max(0.1f, radius / 3f);
            ConfigureQuery();
            sources.Clear();
            foreach (var previousMesh in meshes) if (previousMesh != null) DestroyImmediate(previousMesh);
            meshes.Clear();

            var bounds = area.bounds;
            // The spawn zone can end above the playable floor. Include lower terrain
            // within the same horizontal room span without changing the spawn zone.
            var terrain = new HashSet<Collider2D>();
            foreach (var root in area.gameObject.scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<Collider2D>())
            {
                var collider = candidate.compositeOperation != Collider2D.CompositeOperation.None
                    ? candidate.composite : candidate;
                if (collider == null || !collider.enabled || collider.isTrigger || collider == body ||
                    (obstacleLayers.value & (1 << collider.gameObject.layer)) == 0) continue;
                var b = collider.bounds;
                if (!wholeMap &&
                    (b.max.x < bounds.min.x || b.min.x > bounds.max.x || b.min.y > bounds.max.y)) continue;
                terrain.Add(collider);
                if (wholeMap) bounds.Encapsulate(b);
                else bounds.SetMinMax(new Vector3(bounds.min.x, Mathf.Min(bounds.min.y, b.min.y - radius), 0f),
                    new Vector3(bounds.max.x, bounds.max.y, 0f));
            }
            if (wholeMap) bounds.Expand(new Vector3(radius * 2f, radius * 2f, 0f));
            flightBounds = bounds;
            transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.center.y, 0f),
                Quaternion.Euler(-90f, 0f, 0f));
            var surface = NavMeshSurfaceOwner;
            surface.agentTypeID = agentType;
            surface.collectObjects = CollectObjects.Volume;
            surface.center = Vector3.zero;
            surface.size = new Vector3(bounds.size.x, 10f, bounds.size.y);
            surface.layerMask = 0; // Only the explicit 2D sources below are baked.
            surface.hideEditorLogs = true;
            surface.overrideVoxelSize = true;
            surface.voxelSize = settings.voxelSize;

            var floor = new Mesh { name = "Boss flight area" };
            floor.vertices = new[] {
                new Vector3(bounds.min.x, bounds.min.y, 0f),
                new Vector3(bounds.max.x, bounds.min.y, 0f),
                new Vector3(bounds.max.x, bounds.max.y, 0f),
                new Vector3(bounds.min.x, bounds.max.y, 0f)
            };
            floor.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            floor.RecalculateBounds();
            meshes.Add(floor);
            sources.Add(new NavMeshBuildSource {
                shape = NavMeshBuildSourceShape.Mesh, sourceObject = floor,
                transform = Matrix4x4.identity, area = 0
            });

            foreach (var collider in terrain)
            {
                var mesh = collider.CreateMesh(false, false);
                if (mesh == null) continue;
                meshes.Add(mesh);
                var rb = collider.attachedRigidbody;
                sources.Add(new NavMeshBuildSource {
                    shape = NavMeshBuildSourceShape.Mesh, sourceObject = mesh, area = 1,
                    transform = rb != null
                        ? Matrix4x4.TRS(rb.transform.position, rb.transform.rotation, Vector3.one)
                        : Matrix4x4.identity
                });
            }

            surface.RemoveData();
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources,
                new Bounds(surface.center, surface.size), transform.position, transform.rotation);
            if (data == null) throw new System.InvalidOperationException("보스 NavMesh Bake 실패");
            data.name = "SecondMap Boss Navigation";
            surface.navMeshData = data;
            surface.AddData();
            ready = true;
            return data;
        }
#endif

        public override void CollectSources(NavMeshSurface surface,
            List<NavMeshBuildSource> result, NavMeshBuilderState state)
        {
            result.Clear();
            result.AddRange(sources);
        }

        public void ResetPath()
        {
            corners = System.Array.Empty<Vector3>();
            corner = 0;
            nextRepath = 0f;
            reportedMissingPath = false;
        }

        public void MoveTowards(Rigidbody2D body, Vector2 destination, float speed)
        {
            if (!ready) return;
            path ??= new NavMeshPath();
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + 0.25f;
                corners = System.Array.Empty<Vector3>();
                corner = 0;
                bool startFound = NavMesh.SamplePosition(body.position, out var start, radius * 2f, query);
                bool endFound = NavMesh.SamplePosition(destination, out var end, radius * 3f, query);
                bool pathFound = startFound && endFound &&
                    NavMesh.CalculatePath(start.position, end.position, query, path);
                if (pathFound && path.status != NavMeshPathStatus.PathInvalid)
                    corners = path.corners;
                if (corners.Length == 0 && !reportedMissingPath) {
                    string reason = !startFound ? "보스 위치 주변에 NavMesh 없음" :
                        !endFound ? "목표 위치 주변에 NavMesh 없음" : "두 위치 사이 경로 계산 실패";
                    Debug.LogWarning($"보스 경로 실패: {reason}. 보스={body.position}, 목표={destination}, " +
                        $"시작점={startFound}, 도착점={endFound}, " +
                        $"이동영역 min={flightBounds.min}, max={flightBounds.max}. 목표는 유지 중입니다.", this);
                    reportedMissingPath = true;
                }
                if (corners.Length > 0) reportedMissingPath = false;
            }

            // Near the target, allow a collider-checked approach to its actual position.
            // NavMesh projection alone can stop short of a player standing beside terrain.
            if (Vector2.Distance(body.position, destination) <= radius * 3f &&
                CanMove(body, destination - body.position, out _))
            {
                Step(body, destination, speed);
                return;
            }

            while (corner < corners.Length &&
                   Vector2.Distance(body.position, corners[corner]) <= 0.1f) corner++;
            if (corner < corners.Length) Step(body, corners[corner], speed);
            // No route yet: keep the target and retry, never move through a wall.
        }

        private void Step(Rigidbody2D body, Vector2 destination, float speed)
        {
            Vector2 delta = Vector2.ClampMagnitude(destination - body.position,
                Mathf.Max(0f, speed) * Time.fixedDeltaTime);
            if (delta.sqrMagnitude < 0.000001f) return;
            if (!CanMove(body, delta, out float allowed)) {
                delta = delta.normalized * allowed;
            }
            body.MovePosition(body.position + delta);
        }

        private bool CanMove(Rigidbody2D body, Vector2 delta, out float allowed)
        {
            float distance = delta.magnitude;
            allowed = distance;
            if (distance <= 0.0001f) return true;
            int count = body.Cast(delta / distance, obstacleFilter, hits, distance + 0.05f);
            for (int i = 0; i < count; i++) {
                if (hits[i].distance <= 0f && Vector2.Dot(hits[i].normal, delta) >= 0f) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hits[i].distance - 0.05f));
            }
            return allowed >= distance;
        }

        protected override void OnDestroy()
        {
            var surface = NavMeshSurfaceOwner;
            if (surface != null) {
                surface.RemoveData();
            }
            foreach (var mesh in meshes) if (mesh != null) {
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
            base.OnDestroy();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(flightBounds.center, flightBounds.size);
            for (int i = 1; i < corners.Length; i++)
                Gizmos.DrawLine(corners[i - 1], corners[i]);
        }
    }
}
