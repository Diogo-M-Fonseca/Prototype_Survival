// ZombieAnimatorBuilder.cs
// Instalar: colocar numa pasta chamada "Editor" (ex.: Assets/Editor/ZombieAnimatorBuilder.cs).
// Abrir:    menu Tools > Zombie Animator Builder
//
// Cria um Animator Controller pronto para o ZombieAI:
//   Parâmetros: Speed (Float) e Attack (Trigger)
//   Estado inicial "Locomotion": Blend Tree por Speed (Idle / Walk / Run)
//   Estado "Attack": entra por Any State com o trigger Attack e volta ao Locomotion no fim

using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class ZombieAnimatorBuilder : EditorWindow
{
    [SerializeField] private AnimationClip _idle;
    [SerializeField] private AnimationClip _walk;
    [SerializeField] private AnimationClip _run;
    [SerializeField] private AnimationClip _attack;

    [SerializeField] private string _folder = "Assets/Animations";
    [SerializeField] private string _controllerName = "ZombieController";

    [SerializeField, Min(0.1f)] private float _walkSpeed = 1.2f;
    [SerializeField, Min(0.1f)] private float _runSpeed = 4f;

    private AnimatorController _lastCreated;

    [MenuItem("Tools/Zombie Animator Builder")]
    private static void Open()
    {
        ZombieAnimatorBuilder w = GetWindow<ZombieAnimatorBuilder>("Zombie Animator");
        w.minSize = new Vector2(380f, 360f);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Arrasta os clips (a animação dentro do FBX, não o FBX inteiro) e carrega em Criar. " +
            "O controller fica com os parâmetros Speed e Attack que o ZombieAI usa.",
            MessageType.Info);

        EditorGUILayout.LabelField("Clips", EditorStyles.boldLabel);
        _idle = (AnimationClip)EditorGUILayout.ObjectField("Idle (parado)", _idle, typeof(AnimationClip), false);
        _walk = (AnimationClip)EditorGUILayout.ObjectField("Walk (andar)", _walk, typeof(AnimationClip), false);
        _run = (AnimationClip)EditorGUILayout.ObjectField("Run (correr)", _run, typeof(AnimationClip), false);
        _attack = (AnimationClip)EditorGUILayout.ObjectField("Attack (ataque)", _attack, typeof(AnimationClip), false);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Velocidades do Blend Tree", EditorStyles.boldLabel);
        _walkSpeed = Mathf.Max(0.1f, EditorGUILayout.FloatField("Walk (= velocidade a passear)", _walkSpeed));
        _runSpeed = Mathf.Max(0.1f, EditorGUILayout.FloatField("Run (= velocidade a correr)", _runSpeed));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Destino", EditorStyles.boldLabel);
        _folder = EditorGUILayout.TextField("Pasta", _folder);
        _controllerName = EditorGUILayout.TextField("Nome", _controllerName);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Criar Animator Controller", GUILayout.Height(28)))
            _lastCreated = Build();

        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            string label = $"Criar e aplicar aos selecionados ({Selection.gameObjects.Length})";
            if (GUILayout.Button(label, GUILayout.Height(28)))
            {
                _lastCreated = Build();
                if (_lastCreated != null) ApplyToSelection(_lastCreated);
            }
        }

        if (_lastCreated != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.ObjectField("Último criado", _lastCreated, typeof(AnimatorController), false);

            using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
                if (GUILayout.Button("Aplicar o último aos selecionados"))
                    ApplyToSelection(_lastCreated);
        }
    }

    private AnimatorController Build()
    {
        if (_idle == null && _walk == null && _run == null)
        {
            Debug.LogError("Zombie Animator: arrasta pelo menos um clip de movimento (Idle, Walk ou Run).");
            return null;
        }

        string folder = (_folder ?? "").Replace('\\', '/').Trim().TrimEnd('/');
        if (folder != "Assets" && !folder.StartsWith("Assets/"))
        {
            Debug.LogError("Zombie Animator: a pasta tem de estar dentro de Assets (ex.: Assets/Animations).");
            return null;
        }

        EnsureFolder(folder);
        string fileName = string.IsNullOrWhiteSpace(_controllerName) ? "ZombieController" : _controllerName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileName}.controller");

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // ---- Locomotion: Blend Tree 1D por Speed
        AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;

        if (_idle != null) tree.AddChild(_idle, 0f);
        if (_walk != null) tree.AddChild(_walk, _walkSpeed);
        if (_run != null) tree.AddChild(_run, Mathf.Max(_runSpeed, _walkSpeed + 0.1f));

        sm.defaultState = locomotion;

        // ---- Attack: entra de qualquer estado com o trigger e volta ao Locomotion no fim
        if (_attack != null)
        {
            AnimatorState attack = sm.AddState("Attack");
            attack.motion = _attack;

            AnimatorStateTransition enter = sm.AddAnyStateTransition(attack);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            enter.hasExitTime = false;
            enter.duration = 0.1f;
            enter.canTransitionToSelf = false;

            AnimatorStateTransition leave = attack.AddTransition(locomotion);
            leave.hasExitTime = true;
            leave.exitTime = 0.9f;
            leave.duration = 0.15f;
        }
        else
        {
            Debug.LogWarning("Zombie Animator: sem clip de ataque, o estado Attack não foi criado.");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(controller);
        Debug.Log($"Zombie Animator: criado '{path}'.", controller);
        return controller;
    }

    private static void ApplyToSelection(AnimatorController controller)
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Animator animator = go.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"Zombie Animator: '{go.name}' não tem Animator (o modelo precisa de um).", go);
                continue;
            }

            Undo.RecordObject(animator, "Aplicar Animator Controller");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false; // quem move o zombie é o NavMeshAgent
            EditorUtility.SetDirty(animator);

            if (go.scene.IsValid() && !EditorApplication.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);

            Debug.Log($"Zombie Animator: controller aplicado a '{go.name}'.", go);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(parent)) return;

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
