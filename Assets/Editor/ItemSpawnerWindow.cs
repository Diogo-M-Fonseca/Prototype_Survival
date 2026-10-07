using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Janela do editor para spawnar itens (como ItemPickup) rapidamente no mapa.
/// Menu: Tools > Item Spawner. Colocar este ficheiro numa pasta chamada "Editor".
/// </summary>
public class ItemSpawnerWindow : EditorWindow
{
    private readonly List<Item> _items = new List<Item>();
    private string _search = "";
    private int _amount = 1;
    private bool _usePhysics = true;
    private float _playDistance = 3f;
    private Vector2 _scroll;

    [MenuItem("Tools/Item Spawner")]
    private static void Open() => GetWindow<ItemSpawnerWindow>("Item Spawner");

    private void OnEnable() => Reload();

    private void OnProjectChange()
    {
        Reload();
        Repaint();
    }

    private void Reload()
    {
        _items.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:Item"))
        {
            Item item = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null) _items.Add(item);
        }
        _items.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        _search = EditorGUILayout.TextField("Pesquisar", _search);
        _amount = Mathf.Max(1, EditorGUILayout.IntField("Quantidade", _amount));
        _usePhysics = EditorGUILayout.Toggle(new GUIContent("Com física (cai ao chão)", "Adiciona um Rigidbody se o objeto ainda não tiver."), _usePhysics);
        _playDistance = Mathf.Max(0.5f, EditorGUILayout.FloatField(new GUIContent("Distância (Play)", "Distância à frente da câmara quando nada é atingido."), _playDistance));

        EditorGUILayout.HelpBox(
            Application.isPlaying
                ? "Play Mode: spawna onde a câmara principal está a olhar."
                : "Edit Mode: spawna onde a Scene View está a olhar (centro do ecrã).",
            MessageType.None);

        IEnumerable<Item> filtered = string.IsNullOrWhiteSpace(_search)
            ? _items
            : _items.Where(i => i.name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0
                             || i.DisplayName.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (Item item in filtered)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(44));

            Texture preview = item.HeldPrefab != null ? AssetPreview.GetAssetPreview(item.HeldPrefab) : null;
            if (preview == null && item.HeldPrefab != null && AssetPreview.IsLoadingAssetPreview(item.HeldPrefab.GetInstanceID()))
                Repaint();
            GUILayout.Label(preview, GUILayout.Width(40), GUILayout.Height(40));

            GUILayout.Label(item.DisplayName, GUILayout.ExpandWidth(true), GUILayout.Height(40));

            if (GUILayout.Button("Spawn", GUILayout.Width(70), GUILayout.Height(40)))
                Spawn(item);

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private bool TryGetSpawnPoint(out Vector3 point)
    {
        point = default;

        if (Application.isPlaying && Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            Ray ray = new Ray(cam.position, cam.forward);
            point = Physics.Raycast(ray, out RaycastHit hit, _playDistance) ? hit.point : ray.GetPoint(_playDistance);
            return true;
        }

        SceneView view = SceneView.lastActiveSceneView;
        if (view == null)
        {
            Debug.LogWarning("Item Spawner: abre uma Scene View para escolher onde spawnar.");
            return false;
        }

        Transform sceneCam = view.camera.transform;
        Ray sceneRay = new Ray(sceneCam.position, sceneCam.forward);
        point = Physics.Raycast(sceneRay, out RaycastHit sceneHit, 200f) ? sceneHit.point : view.pivot;
        return true;
    }

    private void Spawn(Item item)
    {
        if (!TryGetSpawnPoint(out Vector3 point)) return;

        GameObject go;
        if (item.HeldPrefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(item.HeldPrefab);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = Vector3.one * 0.3f;
        }

        go.name = "Pickup_" + item.name;
        Undo.RegisterCreatedObjectUndo(go, "Spawn " + item.name);
        go.transform.SetPositionAndRotation(point, Quaternion.identity);

        // Garante um collider (sem ele o raycast do PlayerInteraction não o apanha)
        if (go.GetComponentInChildren<Collider>() == null)
            AddBoxCollider(go);

        // Pousa o objeto em cima da superfície em vez de o enterrar
        if (TryGetBounds(go, out Bounds bounds))
            go.transform.position += Vector3.up * (point.y - bounds.min.y);

        // ItemPickup com o item e a quantidade
        ItemPickup pickup = go.GetComponent<ItemPickup>();
        if (pickup == null) pickup = Undo.AddComponent<ItemPickup>(go);

        SerializedObject so = new SerializedObject(pickup);
        so.FindProperty("_item").objectReferenceValue = item;
        so.FindProperty("_amount").intValue = _amount;
        so.ApplyModifiedProperties();

        if (_usePhysics && go.GetComponent<Rigidbody>() == null)
        {
            // Rigidbody exige MeshColliders convexos
            foreach (MeshCollider mc in go.GetComponentsInChildren<MeshCollider>())
                if (!mc.convex) mc.convex = true;

            Undo.AddComponent<Rigidbody>(go);
        }

        Selection.activeGameObject = go;
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(go.scene);
    }

    private static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;

        bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        return true;
    }

    private static void AddBoxCollider(GameObject go)
    {
        BoxCollider box = Undo.AddComponent<BoxCollider>(go);
        if (!TryGetBounds(go, out Bounds b)) return;

        Vector3 s = go.transform.lossyScale;
        box.center = go.transform.InverseTransformPoint(b.center);
        box.size = new Vector3(
            s.x != 0f ? b.size.x / Mathf.Abs(s.x) : b.size.x,
            s.y != 0f ? b.size.y / Mathf.Abs(s.y) : b.size.y,
            s.z != 0f ? b.size.z / Mathf.Abs(s.z) : b.size.z);
    }
}
