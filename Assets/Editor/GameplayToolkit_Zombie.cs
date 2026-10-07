// GameplayToolkit_Zombie.cs
// Terceira parte da janela (partial class). Colocar na mesma pasta Editor que o GameplayToolkit.cs.
// Separador "Zombie": prepara a área de NavMesh dos corredores, marca o chão como corredor,
// faz o bake e configura/cria zombies com o ZombieAI num clique.

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public partial class GameplayToolkit
{
    // ------------------------------------------------------------------ estado

    [SerializeField] private string _zArea = "Corridor";

    [SerializeField, Min(0.1f)] private float _zRadius = 0.4f;
    [SerializeField, Min(0.5f)] private float _zHeight = 1.8f;
    [SerializeField, Min(0f)] private float _zEyesHeight = 1.6f;

    [SerializeField, Min(0.1f)] private float _zWalk = 1.2f;
    [SerializeField, Min(0.1f)] private float _zRun = 4f;

    [SerializeField, Min(1f)] private float _zViewDist = 15f;
    [SerializeField, Range(10f, 360f)] private float _zViewAngle = 110f;
    [SerializeField, Min(0f)] private float _zLoseSight = 2f;
    [SerializeField, Min(0.05f)] private float _zTolerance = 0.5f;

    [SerializeField, Min(1f)] private float _zWanderMin = 4f;
    [SerializeField, Min(2f)] private float _zWanderMax = 12f;
    [SerializeField, Min(0f)] private float _zWaitMin = 1f;
    [SerializeField, Min(0f)] private float _zWaitMax = 4f;
    [SerializeField, Min(0.1f)] private float _zStop = 1.2f;

    // ------------------------------------------------------------------ UI

    private void DrawZombieTab()
    {
        EditorGUILayout.HelpBox(
            $"Objetos selecionados: {Selection.gameObjects.Length} (da cena).\n" +
            "Ordem rápida: 1) Preparar área  →  2) selecionar o chão dos corredores e Marcar  →  3) Bake  →  4) Criar zombie.",
            MessageType.None);

        // ---- Corredores
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("1. Corredores (área de NavMesh)", EditorStyles.boldLabel);
        _zArea = EditorGUILayout.TextField("Nome da área", _zArea);

        if (GUILayout.Button("Preparar área (cria se não existir)")) { _log.Clear(); EnsureCorridorArea(); }
        if (GUILayout.Button("Marcar selecionados como corredor")) MarkCorridors();
        if (GUILayout.Button("Bake NavMesh (todos os NavMeshSurface da cena)")) BakeSurfaces();
        EditorGUILayout.EndVertical();

        // ---- Zombie
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Zombie", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Corpo", EditorStyles.miniBoldLabel);
        _zRadius = Mathf.Max(0.1f, EditorGUILayout.FloatField("Raio", _zRadius));
        _zHeight = Mathf.Max(0.5f, EditorGUILayout.FloatField("Altura", _zHeight));
        _zEyesHeight = Mathf.Max(0f, EditorGUILayout.FloatField("Altura dos olhos", _zEyesHeight));

        EditorGUILayout.LabelField("Movimento", EditorStyles.miniBoldLabel);
        _zWalk = Mathf.Max(0.1f, EditorGUILayout.FloatField("Velocidade a passear", _zWalk));
        _zRun = Mathf.Max(0.1f, EditorGUILayout.FloatField("Velocidade a correr", _zRun));

        EditorGUILayout.LabelField("Visão", EditorStyles.miniBoldLabel);
        _zViewDist = Mathf.Max(1f, EditorGUILayout.FloatField("Distância de visão", _zViewDist));
        _zViewAngle = EditorGUILayout.Slider("Ângulo de visão", _zViewAngle, 10f, 360f);
        _zLoseSight = Mathf.Max(0f, EditorGUILayout.FloatField("Segundos até desistir", _zLoseSight));
        _zTolerance = Mathf.Max(0.05f, EditorGUILayout.FloatField("Tolerância do corredor", _zTolerance));
        _zStop = Mathf.Max(0.1f, EditorGUILayout.FloatField("Distância a que pára", _zStop));

        EditorGUILayout.LabelField("Passeio", EditorStyles.miniBoldLabel);
        _zWanderMin = Mathf.Max(1f, EditorGUILayout.FloatField("Distância mínima", _zWanderMin));
        _zWanderMax = Mathf.Max(_zWanderMin + 1f, EditorGUILayout.FloatField("Distância máxima", _zWanderMax));
        _zWaitMin = Mathf.Max(0f, EditorGUILayout.FloatField("Espera mínima (s)", _zWaitMin));
        _zWaitMax = Mathf.Max(_zWaitMin, EditorGUILayout.FloatField("Espera máxima (s)", _zWaitMax));

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Configurar selecionados", GUILayout.Height(26))) ApplyZombies();
            if (GUILayout.Button("Criar zombie novo", GUILayout.Height(26))) CreateZombie();
        }
        EditorGUILayout.EndVertical();
    }

    // ------------------------------------------------------------------ área e corredores

    private string AreaName => string.IsNullOrWhiteSpace(_zArea) ? "Corridor" : _zArea.Trim();

    /// <summary>Devolve o índice da área, criando-a em ProjectSettings/NavMeshAreas se ainda não existir; -1 em caso de falha.</summary>
    private int EnsureCorridorArea()
    {
        string name = AreaName;

        int existing = NavMesh.GetAreaFromName(name);
        if (existing >= 0) { Ok($"área '{name}' já existe (índice {existing})."); return existing; }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset");
        if (assets == null || assets.Length == 0)
        {
            Err("Não foi possível abrir ProjectSettings/NavMeshAreas.asset. Cria a área à mão em Window > AI > Navigation > Areas.");
            return -1;
        }

        var so = new SerializedObject(assets[0]);
        SerializedProperty areas = so.FindProperty("areas");
        if (areas == null) { Err("Formato inesperado do NavMeshAreas.asset; cria a área à mão."); return -1; }

        // 0-2 são as áreas internas (Walkable, Not Walkable, Jump)
        for (int i = 3; i < areas.arraySize; i++)
        {
            SerializedProperty n = areas.GetArrayElementAtIndex(i).FindPropertyRelative("name");
            if (n == null || !string.IsNullOrEmpty(n.stringValue)) continue;

            n.stringValue = name;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Ok($"área '{name}' criada (índice {i}).");
            return i;
        }

        Err("Não há índices de área livres.");
        return -1;
    }

    private static Type FindType(params string[] names)
    {
        foreach (string n in names)
        {
            Type t = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).FirstOrDefault(x => x != null);
            if (t != null) return t;
        }
        return null;
    }

    private static readonly string[] ModifierTypes = { "Unity.AI.Navigation.NavMeshModifier", "UnityEngine.AI.NavMeshModifier" };
    private static readonly string[] SurfaceTypes = { "Unity.AI.Navigation.NavMeshSurface", "UnityEngine.AI.NavMeshSurface" };

    /// <summary>Põe um NavMeshModifier (Override Area = corredor) em cada objeto selecionado.</summary>
    private void MarkCorridors()
    {
        _log.Clear();

        Type modifier = FindType(ModifierTypes);
        if (modifier == null)
        {
            Err("Pacote 'AI Navigation' não instalado (Window > Package Manager > AI Navigation).");
            return;
        }

        int area = EnsureCorridorArea();
        if (area < 0) return;

        bool any = false;
        foreach (GameObject go in SelectedObjects())
        {
            Component mod = go.GetComponent(modifier) ?? ObjectFactory.AddComponent(go, modifier);

            var so = new SerializedObject(mod);
            SerializedProperty over = so.FindProperty("m_OverrideArea");
            SerializedProperty areaProp = so.FindProperty("m_Area");
            if (over == null || areaProp == null) { Err($"{go.name}: campos do NavMeshModifier não encontrados."); continue; }

            over.boolValue = true;
            areaProp.intValue = area;
            so.ApplyModifiedProperties();

            MarkDirty(go);
            Ok($"{go.name}: marcado como '{AreaName}'.");
            any = true;
        }

        if (any) Warn("Faz Bake do NavMesh para as alterações terem efeito.");
    }

    private void BakeSurfaces()
    {
        _log.Clear();

        Type surface = FindType(SurfaceTypes);
        if (surface == null)
        {
            Err("Pacote 'AI Navigation' não instalado (Window > Package Manager > AI Navigation).");
            return;
        }

        Object[] surfaces = Object.FindObjectsByType(surface, FindObjectsSortMode.None);
        if (surfaces.Length == 0)
        {
            Err("Não há nenhum NavMeshSurface na cena. Cria um (GameObject > AI > NavMesh Surface) e tenta outra vez.");
            return;
        }

        foreach (Object s in surfaces)
        {
            surface.GetMethod("BuildNavMesh")?.Invoke(s, null);
            EditorUtility.SetDirty(s);
            Ok($"bake feito: {s.name}");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
    }

    // ------------------------------------------------------------------ zombies

    private void ApplyZombies()
    {
        _log.Clear();

        int area = EnsureCorridorArea();
        if (area < 0) return;

        foreach (GameObject go in SelectedObjects())
            ConfigureZombie(go, area);
    }

    private void CreateZombie()
    {
        _log.Clear();

        int area = EnsureCorridorArea();
        if (area < 0) return;

        // Raiz nos pés (o NavMeshAgent assenta a raiz no chão); a cápsula é só visual
        var root = new GameObject("Zombie");
        Undo.RegisterCreatedObjectUndo(root, "Criar zombie");

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, _zHeight * 0.5f, 0f);
        visual.transform.localScale = new Vector3(_zRadius * 2f, _zHeight * 0.5f, _zRadius * 2f);

        // Põe-o no corredor mais perto do centro da vista da cena
        Vector3 pos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 100f, 1 << area)) pos = hit.position;
        else Warn("Não há NavMesh de corredor por perto; marca os corredores e faz Bake, depois move o zombie para lá.");
        root.transform.position = pos;

        ConfigureZombie(root, area);
        Selection.activeGameObject = root;
    }

    private void ConfigureZombie(GameObject go, int area)
    {
        if (EditorUtility.IsPersistent(go))
        {
            Warn($"{go.name}: é um asset do Project. Arrasta-o para a cena (ou abre o prefab) e seleciona-o lá.");
            return;
        }

        // ---- collider (para o jogador/raycasts o verem)
        if (go.GetComponentInChildren<Collider>(true) == null)
        {
            CapsuleCollider cap = ObjectFactory.AddComponent<CapsuleCollider>(go);
            cap.radius = _zRadius;
            cap.height = _zHeight;
            cap.center = new Vector3(0f, _zHeight * 0.5f, 0f);
        }

        // ---- NavMeshAgent restrito à área dos corredores
        NavMeshAgent agent = GetOrAdd<NavMeshAgent>(go);
        agent.radius = _zRadius;
        agent.height = _zHeight;
        agent.speed = _zWalk;
        agent.acceleration = 10f;
        agent.angularSpeed = 240f;
        agent.stoppingDistance = 0.3f;
        agent.areaMask = 1 << area;
        EditorUtility.SetDirty(agent);

        // ---- olhos (origem da visão)
        Transform eyes = FindDeep(go.transform, "Eyes");
        if (eyes == null)
        {
            var e = new GameObject("Eyes");
            Undo.RegisterCreatedObjectUndo(e, "Criar olhos");
            e.transform.SetParent(go.transform, false);
            eyes = e.transform;
        }
        eyes.localPosition = new Vector3(0f, _zEyesHeight, 0f);
        eyes.localRotation = Quaternion.identity;

        // ---- ZombieAI
        ZombieAI ai = GetOrAdd<ZombieAI>(go);
        var so = new SerializedObject(ai);

        SetString(so, "_corridorArea", AreaName);
        SetObject(so, "_eyes", eyes);
        SetFloat(so, "_corridorTolerance", _zTolerance);
        SetFloat(so, "_viewDistance", _zViewDist);
        SetFloat(so, "_viewAngle", _zViewAngle);
        SetFloat(so, "_loseSightTime", _zLoseSight);
        SetFloat(so, "_walkSpeed", _zWalk);
        SetFloat(so, "_runSpeed", _zRun);
        SetFloat(so, "_minWanderDistance", _zWanderMin);
        SetFloat(so, "_maxWanderDistance", _zWanderMax);
        SetFloat(so, "_waitMin", _zWaitMin);
        SetFloat(so, "_waitMax", _zWaitMax);
        SetFloat(so, "_stopDistance", _zStop);
        so.ApplyModifiedProperties();

        // O alvo fica vazio de propósito: o ZombieAI procura o PlayerMove sozinho
        if (Object.FindFirstObjectByType<PlayerMove>() == null)
            Warn($"{go.name}: não há PlayerMove na cena; o zombie não terá a quem perseguir.");

        MarkDirty(go);
        Ok($"{go.name}: zombie configurado (área '{AreaName}').");
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ helpers de propriedades

    private void SetFloat(SerializedObject so, string field, float value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null) p.floatValue = value; else Warn($"campo '{field}' não encontrado no ZombieAI (script desatualizado?).");
    }

    private void SetString(SerializedObject so, string field, string value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null) p.stringValue = value; else Warn($"campo '{field}' não encontrado no ZombieAI (script desatualizado?).");
    }

    private void SetObject(SerializedObject so, string field, Object value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = value; else Warn($"campo '{field}' não encontrado no ZombieAI (script desatualizado?).");
    }
}
