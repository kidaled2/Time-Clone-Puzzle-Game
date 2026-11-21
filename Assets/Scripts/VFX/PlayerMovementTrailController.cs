using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PlayerMovementTrailController : MonoBehaviour
{
    private const int MaxPoints = 48;
    [Header("Movement Trail")]
    [SerializeField, Min(0.02f)] private float ribbonLifetime = 0.3f;
    [SerializeField, Min(0.01f)] private float ribbonWidth = 0.4f;
    [SerializeField, Min(0.001f)] private float pointSpacing = 0.025f;
    [SerializeField] private float ribbonHeight = 0.3f;
    [SerializeField, Min(0.1f)] private float teleportClearDistance = 1.25f;

    private readonly List<Vector3> points = new List<Vector3>(MaxPoints);
    private readonly List<float> pointTimes = new List<float>(MaxPoints);
    private Ribbon[] ribbons;
    private Material ribbonMaterial;
    private Camera viewCamera;
    private Vector3 lastPosition;
    private bool wasMoving;
    private Color trailColor = Color.cyan;

    private sealed class Ribbon
    {
        public Mesh Mesh;
        public MeshRenderer Renderer;
        public float Lifetime;
        public float Width;
        public Vector2 Offset;
        public float Whiteness;
        public float Opacity;
        public readonly Vector3[] Vertices = new Vector3[MaxPoints * 2];
        public readonly Color[] Colors = new Color[MaxPoints * 2];
        public readonly Vector2[] UVs = new Vector2[MaxPoints * 2];
        public readonly int[] Triangles = new int[(MaxPoints - 1) * 6];
    }

    private void Awake()
    {
        foreach (TrailRenderer oldTrail in GetComponentsInChildren<TrailRenderer>(true))
        {
            oldTrail.emitting = false;
            oldTrail.Clear();
            oldTrail.enabled = false;
        }

        Shader shader = Resources.Load<Shader>("MovementTrail");
        if (shader == null)
        {
            return;
        }

        viewCamera = Camera.main;
        ribbonMaterial = new Material(shader);
        var root = new GameObject("MovementTrailFX");
        root.transform.SetParent(transform, false);
        ribbons = new[]
        {
            CreateRibbon(root.transform, "FlowGlow", 1f, 1f, Vector2.zero, 0f, 0.65f),
            CreateRibbon(root.transform, "FlowCore", 0.85f, 0.25f, new Vector2(0f, 0.01f), 0.15f, 1f),
            CreateRibbon(root.transform, "FlowUpperStreak", 0.72f, 0.15f, new Vector2(0.09f, 0.14f), 0f, 0.8f),
            CreateRibbon(root.transform, "FlowLowerStreak", 0.62f, 0.1f, new Vector2(-0.11f, -0.08f), 0f, 0.8f)
        };

        Transform body = transform.Find("Character_Root/Body");
        if (body != null && body.TryGetComponent(out MeshRenderer renderer) && renderer.sharedMaterial != null)
        {
            SetColor(renderer.sharedMaterial.color);
        }
    }

    private Ribbon CreateRibbon(Transform parent, string ribbonName, float lifetime, float width,
        Vector2 offset, float whiteness, float opacity)
    {
        var ribbonObject = new GameObject(ribbonName, typeof(MeshFilter), typeof(MeshRenderer));
        ribbonObject.transform.SetParent(parent, false);
        var mesh = new Mesh { name = ribbonName };
        mesh.MarkDynamic();
        ribbonObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = ribbonObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = ribbonMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.enabled = false;
        return new Ribbon
        {
            Mesh = mesh,
            Renderer = renderer,
            Lifetime = ribbonLifetime * lifetime,
            Width = ribbonWidth * width,
            Offset = offset,
            Whiteness = whiteness,
            Opacity = opacity
        };
    }

    private void OnEnable()
    {
        ClearTrail();
    }

    private void LateUpdate()
    {
        Vector3 current = transform.position;
        if ((current - lastPosition).sqrMagnitude > teleportClearDistance * teleportClearDistance)
        {
            ClearTrail();
            return;
        }

        if (ribbons == null || Time.timeScale <= 0f || Time.deltaTime <= 0f)
        {
            lastPosition = current;
            return;
        }

        bool moving = (current - lastPosition).sqrMagnitude > 0.000001f;
        if (moving)
        {
            if (!wasMoving)
            {
                AddPoint(lastPosition);
            }

            if (points.Count == 0 || (current - points[points.Count - 1]).sqrMagnitude >= pointSpacing * pointSpacing)
            {
                AddPoint(current);
            }
        }

        int expired = 0;
        while (expired < points.Count && Time.time - pointTimes[expired] >= ribbonLifetime)
        {
            expired++;
        }

        if (expired > 0)
        {
            points.RemoveRange(0, expired);
            pointTimes.RemoveRange(0, expired);
        }

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        foreach (Ribbon ribbon in ribbons)
        {
            BuildRibbon(ribbon);
        }

        wasMoving = moving;
        lastPosition = current;
    }

    private void AddPoint(Vector3 position)
    {
        if (points.Count == MaxPoints)
        {
            points.RemoveAt(0);
            pointTimes.RemoveAt(0);
        }

        points.Add(position);
        pointTimes.Add(Time.time);
    }

    private void BuildRibbon(Ribbon ribbon)
    {
        int first = 0;
        while (first < points.Count && Time.time - pointTimes[first] >= ribbon.Lifetime)
        {
            first++;
        }

        int count = points.Count - first;
        ribbon.Renderer.enabled = count >= 2 && viewCamera != null;
        if (!ribbon.Renderer.enabled)
        {
            return;
        }

        Matrix4x4 toLocal = ribbon.Renderer.transform.worldToLocalMatrix;
        for (int i = 0; i < count; i++)
        {
            int index = first + i;
            Vector3 tangent = points[Mathf.Min(index + 1, points.Count - 1)] - points[Mathf.Max(index - 1, first)];
            Vector3 right = Vector3.Cross(tangent.normalized, viewCamera.transform.forward).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, tangent.normalized);
            Vector3 center = points[index] + Vector3.up * (ribbonHeight + ribbon.Offset.y) + side * ribbon.Offset.x;
            float freshness = Mathf.Clamp01(1f - (Time.time - pointTimes[index]) / ribbon.Lifetime);
            float halfWidth = ribbon.Width * freshness * 0.5f;
            ribbon.Vertices[i * 2] = toLocal.MultiplyPoint3x4(center - right * halfWidth);
            ribbon.Vertices[i * 2 + 1] = toLocal.MultiplyPoint3x4(center + right * halfWidth);
            Color color = Color.Lerp(trailColor, Color.white, ribbon.Whiteness);
            color.a = ribbon.Opacity * freshness;
            ribbon.Colors[i * 2] = color;
            ribbon.Colors[i * 2 + 1] = color;
            ribbon.UVs[i * 2] = new Vector2(freshness, 0f);
            ribbon.UVs[i * 2 + 1] = new Vector2(freshness, 1f);
        }

        for (int i = 0; i < count - 1; i++)
        {
            int vertex = i * 2;
            int triangle = i * 6;
            ribbon.Triangles[triangle] = vertex;
            ribbon.Triangles[triangle + 1] = vertex + 1;
            ribbon.Triangles[triangle + 2] = vertex + 2;
            ribbon.Triangles[triangle + 3] = vertex + 2;
            ribbon.Triangles[triangle + 4] = vertex + 1;
            ribbon.Triangles[triangle + 5] = vertex + 3;
        }

        ribbon.Mesh.Clear();
        ribbon.Mesh.SetVertices(ribbon.Vertices, 0, count * 2);
        ribbon.Mesh.SetColors(ribbon.Colors, 0, count * 2);
        ribbon.Mesh.SetUVs(0, ribbon.UVs, 0, count * 2);
        ribbon.Mesh.SetTriangles(ribbon.Triangles, 0, (count - 1) * 6, 0);
    }

    public void SetColor(Color color)
    {
        trailColor = new Color(color.r, color.g, color.b, 1f);
    }

    public void ClearTrail()
    {
        lastPosition = transform.position;
        wasMoving = false;
        points.Clear();
        pointTimes.Clear();
        if (ribbons == null)
        {
            return;
        }

        foreach (Ribbon ribbon in ribbons)
        {
            if (ribbon.Renderer != null)
            {
                ribbon.Renderer.enabled = false;
                ribbon.Mesh.Clear();
            }
        }
    }

    private void OnDisable()
    {
        ClearTrail();
    }

    private void OnDestroy()
    {
        if (ribbons != null)
        {
            foreach (Ribbon ribbon in ribbons)
            {
                Destroy(ribbon.Mesh);
            }
        }

        Destroy(ribbonMaterial);
    }
}
