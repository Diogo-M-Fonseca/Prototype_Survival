using System.Collections.Generic;
using UnityEngine;

public class ItemIconRenderer
{
    private const int Size = 128;
    private static readonly Vector3 Origin = new Vector3(10000f, 10000f, 10000f);

    private static readonly Dictionary<GameObject, Sprite> cache = new Dictionary<GameObject, Sprite>();

    public static Sprite Get(GameObject prefab)
    {
        if (prefab == null) return null;

        if (cache.TryGetValue(prefab, out Sprite cached))
        {
            if (cached != null || ReferenceEquals(cached, null)) return cached;
        }

        Sprite sprite = Render(prefab);
        cache[prefab] = sprite;
        return sprite;
    }

    private static Sprite Render(GameObject prefab)
    {
        GameObject instance = null;
        GameObject camObj = null;
        GameObject lightObj = null;
        RenderTexture rt = null;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            instance = Object.Instantiate(prefab, Origin, Quaternion.identity);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return null;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            float radius = bounds.extents.magnitude;
            float distance = radius * 2f + 1f;

            camObj = new GameObject("IconCamera");
            Camera cam = camObj.AddComponent<Camera>();

            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.orthographic = true;
            cam.orthographicSize = radius;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = distance + radius * 2f;

            camObj.transform.position = bounds.center + new Vector3(1f, 1f, -1f).normalized * distance;
            camObj.transform.LookAt(bounds.center);

            lightObj = new GameObject("IconLight");
            lightObj.AddComponent<Light>().type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            rt = RenderTexture.GetTemporary(Size, Size, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();

            cam.targetTexture = null;

            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
            if (instance != null) Object.DestroyImmediate(instance);
            if (camObj != null) Object.DestroyImmediate(camObj);
            if (lightObj != null) Object.DestroyImmediate(lightObj);
        }
    }
}
