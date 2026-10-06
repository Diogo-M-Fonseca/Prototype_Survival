// GameplayToolkit.cs
// Instalar: colocar numa pasta chamada "Editor" (ex.: Assets/Editor/GameplayToolkit.cs).
// Abrir:    menu Tools > Gameplay Toolkit
//
// Cria itens (FoodsAndDrinks / OtherItems), tags, receitas, pickups, storages e ItemTargets
// com regras, tudo a partir de linhas de texto, sem andar a preencher o Inspector campo a campo.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public partial class GameplayToolkit : EditorWindow
{
    // ------------------------------------------------------------------ tipos auxiliares

    private enum Kind { Food, Other }

    private sealed class Line
    {
        public int Number;
        public string Name;
        public readonly Dictionary<string, string> Opts = new Dictionary<string, string>();

        /// <summary>Item a atualizar (formulário); se null procura-se pelo nome.</summary>
        public Item Existing;

        /// <summary>Referências diretas (formulário): prefab, icon, leftover. Um valor null limpa o campo.</summary>
        public readonly Dictionary<string, Object> Refs = new Dictionary<string, Object>();

        /// <summary>Tags escolhidas no formulário (null = usar a opção 'tags' em texto).</summary>
        public List<ItemTag> TagRefs;

        public bool Has(string key) => Opts.ContainsKey(key);
        public string Get(string key) => Opts.TryGetValue(key, out string v) ? v : null;
    }

    private sealed class RuleSpec
    {
        public ItemTag Tag;
        public bool Consume = true;
        public Item Result;
        public string Action;   // none | enable | disable | trigger
        public string Arg;      // nome do filho (enable/disable) ou do trigger (trigger)
    }

    // ------------------------------------------------------------------ constantes

    private static readonly HashSet<string> KnownItemKeys = new HashSet<string>
    {
        "type", "display", "desc", "description", "stack", "prefab", "icon", "durability",
        "leftover", "tags", "hunger", "health", "heal", "thirst", "mats", "materials"
    };

    private static readonly (string key, RestoreStat stat)[] RestoreKeys =
    {
        ("hunger", RestoreStat.Hunger),
        ("health", RestoreStat.Health),
        ("heal", RestoreStat.Health),
        ("thirst", RestoreStat.Thirst),
    };

    private static readonly Regex RecipeRx =
        new Regex(@"^\s*(?<name>[^:]+?)\s*:\s*(?<ing>.+?)\s*(?:->|=>|>)\s*(?<res>.+?)\s*$");

    private static readonly Regex TermRx =
        new Regex(@"^\s*(?:(?<n>\d+)\s*[xX×]?\s+)?(?<name>.+?)\s*$");

    private const string ItemsExample =
@"# Nome | opção=valor | ...
# comida/bebida: hunger= health= thirst=   |  outros: mats=Wood+Metal
# comum: stack= desc= prefab= icon= durability= leftover= tags=a+b display= type=food|other
Bandage | stack=10 | health=30 | prefab=Bandage
Energy Bar | stack=20 | hunger=30 | prefab=EnergyBar
Water Bottle | stack=10 | thirst=30 | leftover=Empty Bottle | prefab=WaterBottle
Empty Bottle | stack=10 | mats=Plastic | tags=Container
Raw Meat | durability=100 | hunger=15 | display=Raw Meat
Door | mats=Wood+Metal | tags=Placeable
";

    private const string RecipesExample =
@"# Nome da receita: 2 Ingrediente + 1 Outro -> 3 Resultado
Bandage Recipe: 2 Cloth + 1 Alcohol -> 3 Bandage
";

    private const string RulesExample =
@"# Etiqueta | consume=true/false | result=Item | on=enable:Filho / disable:Filho / trigger:NomeDoTrigger
Key | consume=true | result=UsedKey | on=trigger:Open
";

    // ------------------------------------------------------------------ estado da janela

    [SerializeField] private int _tab;
    [SerializeField] private string _folder = "Assets/Items";
    [SerializeField] private Kind _defaultKind = Kind.Food;
    [SerializeField] private string _itemsText;
    [SerializeField] private string _tagsText = "Key, Container";
    [SerializeField] private string _recipesText;
    [SerializeField] private string _rulesText;
    [SerializeField] private bool _replaceRules = true;

    [SerializeField] private Item _pickupItem;
    [SerializeField, Min(1)] private int _pickupAmount = 1;
    [SerializeField] private string _pickupPrompt;

    [SerializeField] private string _storageTitle = "Storage Container";
    [SerializeField, Min(1)] private int _storageSize = 20;
    [SerializeField, Min(0.5f)] private float _storageDistance = 4f;

    private Vector2 _scroll;
    private Vector2 _logScroll;
    private GUIStyle _logStyle;
    private readonly List<string> _log = new List<string>();

    private Dictionary<string, string> _prefabPaths;
    private Dictionary<string, string> _spritePaths;

    [MenuItem("Tools/Gameplay Toolkit")]
    public static void Open()
    {
        GameplayToolkit w = GetWindow<GameplayToolkit>("Gameplay Toolkit");
        w.minSize = new Vector2(420f, 420f);
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(_itemsText)) _itemsText = ItemsExample;
        if (string.IsNullOrEmpty(_recipesText)) _recipesText = RecipesExample;
        if (string.IsNullOrEmpty(_rulesText)) _rulesText = RulesExample;
        if (string.IsNullOrEmpty(_folder)) _folder = "Assets/Items";
    }

    // ------------------------------------------------------------------ UI

    private void OnGUI()
    {
        _tab = GUILayout.Toolbar(_tab, new[] { "Itens", "Receitas", "Mundo" }, GUILayout.Height(24));
        _textMode = GUILayout.Toolbar(_textMode ? 1 : 0, new[] { "Formulário", "Texto" }) == 1;
        EditorGUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        switch (_tab)
        {
            case 0: if (_textMode) DrawItemsTab(); else DrawItemForm(); break;
            case 1: if (_textMode) DrawRecipesTab(); else DrawRecipeForm(); break;
            default: DrawWorldTab(); break;
        }
        EditorGUILayout.EndScrollView();

        DrawLog();
    }

    private void DrawFolderField()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            _folder = EditorGUILayout.TextField("Pasta de destino", _folder);
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string abs = EditorUtility.OpenFolderPanel("Pasta de destino", Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs) && abs.Replace('\\', '/').StartsWith(Application.dataPath.Replace('\\', '/')))
                {
                    _folder = "Assets" + abs.Replace('\\', '/').Substring(Application.dataPath.Replace('\\', '/').Length);
                    GUI.FocusControl(null);
                }
            }
        }
    }

    private void DrawItemsTab()
    {
        DrawFolderField();
        _defaultKind = (Kind)EditorGUILayout.EnumPopup("Tipo por omissão", _defaultKind);

        EditorGUILayout.HelpBox(
            "Uma linha por item, opções separadas por |.\n" +
            "• O tipo é deduzido: hunger/health/thirst → comida/bebida; mats → outro.\n" +
            "• Se o item já existir (em qualquer pasta), é atualizado: só mexe nas opções que escreveres.\n" +
            "• Tags e itens referidos (leftover, prefab) são procurados por nome; tags em falta são criadas.",
            MessageType.Info);

        _itemsText = EditorGUILayout.TextArea(_itemsText, GUILayout.MinHeight(170));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Criar / atualizar itens", GUILayout.Height(26))) CreateItems();
            if (GUILayout.Button("Exemplo", GUILayout.Width(80), GUILayout.Height(26))) _itemsText = ItemsExample;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
        _tagsText = EditorGUILayout.TextField("Nomes (separados por vírgula)", _tagsText);
        if (GUILayout.Button("Criar tags")) CreateTags();
    }

    private void DrawRecipesTab()
    {
        DrawFolderField();
        EditorGUILayout.HelpBox(
            "Formato:  Nome: 2 Ingrediente + 1 Outro -> 3 Resultado\n" +
            "Os itens são procurados por nome. As receitas ficam em <pasta>/Recipes.",
            MessageType.Info);

        _recipesText = EditorGUILayout.TextArea(_recipesText, GUILayout.MinHeight(140));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Criar / atualizar receitas", GUILayout.Height(26))) CreateRecipes();
            if (GUILayout.Button("Exemplo", GUILayout.Width(80), GUILayout.Height(26))) _recipesText = RecipesExample;
        }
    }

    private void DrawWorldTab()
    {
        EditorGUILayout.HelpBox(
            $"Objetos selecionados: {Selection.gameObjects.Length}  (da cena ou prefabs do Project). " +
            "Os botões abaixo aplicam-se a todos.",
            MessageType.None);

        // ---- Pickup
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Pickup (ItemPickup)", EditorStyles.boldLabel);
        _pickupItem = (Item)EditorGUILayout.ObjectField("Item", _pickupItem, typeof(Item), false);
        _pickupAmount = Mathf.Max(1, EditorGUILayout.IntField("Quantidade", _pickupAmount));
        _pickupPrompt = EditorGUILayout.TextField("Prompt (opcional)", _pickupPrompt);
        if (GUILayout.Button("Tornar pickup")) ApplyPickup();
        EditorGUILayout.EndVertical();

        // ---- Storage
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Storage (StorageContainer + Inventory)", EditorStyles.boldLabel);
        _storageTitle = EditorGUILayout.TextField("Título", _storageTitle);
        _storageSize = Mathf.Max(1, EditorGUILayout.IntField("Slots", _storageSize));
        _storageDistance = Mathf.Max(0.5f, EditorGUILayout.FloatField("Distância para fechar", _storageDistance));
        if (GUILayout.Button("Tornar storage")) ApplyStorage();
        EditorGUILayout.EndVertical();

        // ---- Target + regras
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Alvo de itens (ItemTarget + regras)", EditorStyles.boldLabel);
        if (_textMode)
        {
            EditorGUILayout.HelpBox(
                "Uma linha por regra:  Etiqueta | consume=true/false | result=Item | on=...\n" +
                "on=enable:Filho  /  disable:Filho  (sem nome = o próprio objeto)  /  trigger:NomeDoTrigger (Animator).",
                MessageType.None);
            _rulesText = EditorGUILayout.TextArea(_rulesText, GUILayout.MinHeight(70));
        }
        else DrawRuleRows();
        _replaceRules = EditorGUILayout.ToggleLeft("Substituir regras existentes (desligado = acrescentar)", _replaceRules);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Aplicar regras", GUILayout.Height(24))) ApplyTargets();
            if (_textMode && GUILayout.Button("Exemplo", GUILayout.Width(80), GUILayout.Height(24))) _rulesText = RulesExample;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawLog()
    {
        if (_log.Count == 0) return;

        if (_logStyle == null)
            _logStyle = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };

        EditorGUILayout.Space(4);
        _logScroll = EditorGUILayout.BeginScrollView(_logScroll, EditorStyles.helpBox, GUILayout.Height(120));
        foreach (string entry in _log) EditorGUILayout.LabelField(entry, _logStyle);
        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------------ log

    private void Ok(string msg) => _log.Add("<color=#6c6>✔</color> " + msg);
    private void Warn(string msg) => _log.Add("<color=#fc3>⚠</color> " + msg);
    private void Err(string msg) => _log.Add("<color=#f66>✖</color> " + msg);
    private void Warn(Line l, string msg) => Warn($"linha {l.Number} ({l.Name}): {msg}");
    private void Err(Line l, string msg) => Err($"linha {l.Number} ({l.Name}): {msg}");

    // ------------------------------------------------------------------ itens

    private void CreateItems() => ProcessItems(ParseLines(_itemsText));

    private void ProcessItems(List<Line> lines)
    {
        _log.Clear();
        _prefabPaths = null;
        _spritePaths = null;

        if (lines.Count == 0) { Warn("Não há linhas para processar."); return; }
        if (!PrepareFolder()) return;

        Dictionary<string, Item> items = IndexItems();
        Dictionary<string, ItemTag> tags = Index<ItemTag>();
        var work = new List<(Line line, Item item, bool isNew)>();

        // Passagem 1: criar (ou encontrar) todos os itens, para que referências entre eles funcionem
        foreach (Line line in lines)
        {
            string key = Norm(line.Name);
            if (key.Length == 0) { Err(line, "nome inválido."); continue; }

            Kind? wanted = WantedKind(line);

            Item existing = line.Existing;
            if (existing == null) items.TryGetValue(key, out existing);

            if (existing != null)
            {
                Kind actual = existing is FoodsAndDrinks ? Kind.Food : Kind.Other;
                if (wanted.HasValue && wanted.Value != actual)
                {
                    Err(line, $"já existe como {actual}, mas as opções pedem {wanted.Value}. Ignorado.");
                    continue;
                }
                work.Add((line, existing, false));
                continue;
            }

            Kind kind = wanted ?? _defaultKind;
            Item created = kind == Kind.Food
                ? (Item)ScriptableObject.CreateInstance<FoodsAndDrinks>()
                : ScriptableObject.CreateInstance<OtherItems>();

            string fileName = FileName(line.Name);
            if (fileName.Length == 0) { Err(line, "nome sem caracteres válidos para ficheiro."); continue; }

            string path = AssetDatabase.GenerateUniqueAssetPath($"{_folder}/{fileName}.asset");
            AssetDatabase.CreateAsset(created, path);
            items[key] = created;
            work.Add((line, created, true));
        }

        // Passagem 2: preencher os campos
        var touched = new List<Object>();
        foreach ((Line line, Item item, bool isNew) in work)
        {
            ApplyItem(line, item, isNew, items, tags);
            Ok($"{(isNew ? "criado" : "atualizado")}: {item.name}");
            touched.Add(item);
        }

        FinishAssets(touched);
    }

    private void ApplyItem(Line line, Item item, bool isNew,
                           Dictionary<string, Item> items, Dictionary<string, ItemTag> tags)
    {
        var so = new SerializedObject(item);
        bool isFood = item is FoodsAndDrinks;
        string v;

        foreach (string k in line.Opts.Keys)
            if (!KnownItemKeys.Contains(k)) Warn(line, $"opção desconhecida '{k}' ignorada.");

        if (isNew) so.FindProperty("_displayName").stringValue = line.Name;
        if ((v = line.Get("display")) != null) so.FindProperty("_displayName").stringValue = v;
        if ((v = line.Get("desc") ?? line.Get("description")) != null) so.FindProperty("_description").stringValue = v;

        if ((v = line.Get("stack")) != null)
        {
            if (int.TryParse(v, out int stack)) so.FindProperty("_maxStack").intValue = Mathf.Max(1, stack);
            else Err(line, $"stack inválido '{v}'.");
        }

        if ((v = line.Get("durability")) != null)
        {
            if (int.TryParse(v, out int dur) && dur > 0)
            {
                so.FindProperty("_hasDurability").boolValue = true;
                so.FindProperty("_maxDurability").intValue = dur;
            }
            else if (IsOff(v)) so.FindProperty("_hasDurability").boolValue = false;
            else Err(line, $"durability inválida '{v}'.");
        }

        if (line.Refs.TryGetValue("prefab", out Object prefabRef)) so.FindProperty("_heldPrefab").objectReferenceValue = prefabRef;
        else if ((v = line.Get("prefab")) != null)
        {
            GameObject prefab = FindByName<GameObject>(ref _prefabPaths, "t:Prefab", v);
            if (prefab != null) so.FindProperty("_heldPrefab").objectReferenceValue = prefab;
            else Warn(line, $"prefab '{v}' não encontrado.");
        }

        if (line.Refs.TryGetValue("icon", out Object iconRef)) so.FindProperty("_icon").objectReferenceValue = iconRef;
        else if ((v = line.Get("icon")) != null)
        {
            Sprite sprite = FindByName<Sprite>(ref _spritePaths, "t:Sprite", v);
            if (sprite != null) so.FindProperty("_icon").objectReferenceValue = sprite;
            else Warn(line, $"sprite '{v}' não encontrado.");
        }

        if (line.Refs.TryGetValue("leftover", out Object leftRef)) so.FindProperty("_leftoverItem").objectReferenceValue = leftRef;
        else if ((v = line.Get("leftover")) != null)
        {
            if (IsOff(v)) so.FindProperty("_leftoverItem").objectReferenceValue = null;
            else if (items.TryGetValue(Norm(v), out Item left)) so.FindProperty("_leftoverItem").objectReferenceValue = left;
            else Warn(line, $"leftover '{v}' não encontrado.");
        }

        if (line.TagRefs != null)
        {
            SerializedProperty tagArr = so.FindProperty("_tags");
            tagArr.arraySize = line.TagRefs.Count;
            for (int t = 0; t < line.TagRefs.Count; t++)
                tagArr.GetArrayElementAtIndex(t).objectReferenceValue = line.TagRefs[t];
        }
        else if ((v = line.Get("tags")) != null)
        {
            SerializedProperty arr = so.FindProperty("_tags");
            var list = new List<ItemTag>();
            if (!IsOff(v))
            {
                foreach (string n in v.Split(new[] { '+', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    ItemTag tag = GetOrCreateTag(n, tags);
                    if (tag != null && !list.Contains(tag)) list.Add(tag);
                }
            }
            arr.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }

        bool hasFoodKeys = RestoreKeys.Any(r => line.Has(r.key));
        if (hasFoodKeys)
        {
            if (isFood) ApplyRestores(so, line);
            else Warn(line, "hunger/health/thirst só se aplicam a comida/bebida. Ignorado.");
        }

        string mats = line.Get("mats") ?? line.Get("materials");
        if (mats != null)
        {
            if (!isFood) ApplyMaterials(so, line, mats);
            else Warn(line, "mats só se aplica a itens 'Other'. Ignorado.");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(item);
    }

    private void ApplyRestores(SerializedObject so, Line line)
    {
        SerializedProperty arr = so.FindProperty("_restores");
        var amounts = new SortedDictionary<int, int>();

        // Começa pelo que já existe, para atualizar só o que foi pedido
        for (int i = 0; i < arr.arraySize; i++)
        {
            SerializedProperty e = arr.GetArrayElementAtIndex(i);
            int amt = e.FindPropertyRelative("_amount").intValue;
            if (amt > 0) amounts[e.FindPropertyRelative("_stat").enumValueIndex] = amt;
        }

        foreach ((string key, RestoreStat stat) in RestoreKeys)
        {
            string v = line.Get(key);
            if (v == null) continue;

            if (!int.TryParse(v, out int n)) { Err(line, $"{key} inválido '{v}'."); continue; }
            if (n <= 0) amounts.Remove((int)stat);
            else amounts[(int)stat] = n;
        }

        arr.arraySize = amounts.Count;
        int idx = 0;
        foreach (KeyValuePair<int, int> kv in amounts)
        {
            SerializedProperty e = arr.GetArrayElementAtIndex(idx++);
            e.FindPropertyRelative("_stat").enumValueIndex = kv.Key;
            e.FindPropertyRelative("_amount").intValue = kv.Value;
        }
    }

    private void ApplyMaterials(SerializedObject so, Line line, string text)
    {
        var mats = new List<BaseMaterials>();
        foreach (string n in text.Split(new[] { '+', ',', '/' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse(n.Trim(), true, out BaseMaterials m)) { if (!mats.Contains(m)) mats.Add(m); }
            else Warn(line, $"material '{n.Trim()}' desconhecido ({string.Join(", ", Enum.GetNames(typeof(BaseMaterials)))}).");
        }

        if (mats.Count > 3) { Warn(line, "máximo de 3 materiais; os restantes foram ignorados."); mats = mats.Take(3).ToList(); }

        SerializedProperty arr = so.FindProperty("_baseMaterials");
        arr.arraySize = mats.Count;
        for (int i = 0; i < mats.Count; i++) arr.GetArrayElementAtIndex(i).enumValueIndex = (int)mats[i];
    }

    private static Kind? WantedKind(Line line)
    {
        string t = line.Get("type");
        if (t != null)
        {
            t = Norm(t);
            if (new[] { "food", "drink", "foodsanddrinks", "comida", "bebida", "consumable" }.Contains(t)) return Kind.Food;
            if (new[] { "other", "otheritems", "outro", "outros", "material", "misc" }.Contains(t)) return Kind.Other;
        }

        if (RestoreKeys.Any(r => line.Has(r.key))) return Kind.Food;
        if (line.Has("mats") || line.Has("materials")) return Kind.Other;
        return null;
    }

    private void CreateTags()
    {
        _log.Clear();
        if (!PrepareFolder()) return;

        Dictionary<string, ItemTag> tags = Index<ItemTag>();
        var touched = new List<Object>();

        foreach (string n in (_tagsText ?? "").Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int before = tags.Count;
            ItemTag tag = GetOrCreateTag(n, tags);
            if (tag == null) { Warn($"nome de tag inválido: '{n.Trim()}'."); continue; }
            if (tags.Count == before) Ok($"já existia: {tag.name}");
            touched.Add(tag);
        }

        FinishAssets(touched);
    }

    private ItemTag GetOrCreateTag(string name, Dictionary<string, ItemTag> tags)
    {
        string key = Norm(name);
        if (key.Length == 0) return null;
        if (tags.TryGetValue(key, out ItemTag tag) && tag != null) return tag;

        string dir = _folder + "/Tags";
        EnsureFolder(dir);

        tag = ScriptableObject.CreateInstance<ItemTag>();
        AssetDatabase.CreateAsset(tag, AssetDatabase.GenerateUniqueAssetPath($"{dir}/{FileName(name)}.asset"));
        tags[key] = tag;
        Ok($"tag criada: {tag.name}");
        return tag;
    }

    // ------------------------------------------------------------------ receitas

    private void CreateRecipes()
    {
        _log.Clear();
        if (!PrepareFolder()) return;

        Dictionary<string, Item> items = IndexItems();
        Dictionary<string, ItemRecipe> recipes = Index<ItemRecipe>();
        string dir = _folder + "/Recipes";
        var touched = new List<Object>();

        string[] raw = (_recipesText ?? "").Replace("\r", "").Split('\n');
        for (int i = 0; i < raw.Length; i++)
        {
            string text = raw[i].Trim();
            if (text.Length == 0 || text.StartsWith("#") || text.StartsWith("//")) continue;
            int lineNo = i + 1;

            Match m = RecipeRx.Match(text);
            if (!m.Success) { Err($"linha {lineNo}: formato inválido (esperado 'Nome: 2 A + 1 B -> 3 C')."); continue; }

            string name = m.Groups["name"].Value;
            var ingredients = new List<(Item item, int amount)>();
            bool ok = true;

            foreach (string term in m.Groups["ing"].Value.Split('+'))
            {
                if (!ParseTerm(term, items, out Item item, out int amount)) { Err($"linha {lineNo}: ingrediente '{term.Trim()}' não encontrado."); ok = false; break; }
                ingredients.Add((item, amount));
            }
            if (!ok) continue;

            if (!ParseTerm(m.Groups["res"].Value, items, out Item resItem, out int resAmount))
            {
                Err($"linha {lineNo}: resultado '{m.Groups["res"].Value.Trim()}' não encontrado.");
                continue;
            }

            string key = Norm(name);
            bool isNew = !recipes.TryGetValue(key, out ItemRecipe recipe) || recipe == null;
            if (isNew)
            {
                EnsureFolder(dir);
                recipe = ScriptableObject.CreateInstance<ItemRecipe>();
                AssetDatabase.CreateAsset(recipe, AssetDatabase.GenerateUniqueAssetPath($"{dir}/{FileName(name)}.asset"));
                recipes[key] = recipe;
            }

            var so = new SerializedObject(recipe);
            SerializedProperty ing = so.FindProperty("_ingredients");
            ing.arraySize = ingredients.Count;
            for (int k = 0; k < ingredients.Count; k++)
                WriteStack(ing.GetArrayElementAtIndex(k), ingredients[k].item, ingredients[k].amount);
            WriteStack(so.FindProperty("_result"), resItem, resAmount);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(recipe);

            Ok($"{(isNew ? "criada" : "atualizada")}: {recipe.name}");
            touched.Add(recipe);
        }

        FinishAssets(touched);
    }

    private static bool ParseTerm(string term, Dictionary<string, Item> items, out Item item, out int amount)
    {
        item = null;
        amount = 1;

        Match m = TermRx.Match(term);
        if (!m.Success) return false;
        if (m.Groups["n"].Success) amount = Mathf.Max(1, int.Parse(m.Groups["n"].Value));

        return items.TryGetValue(Norm(m.Groups["name"].Value), out item) && item != null;
    }

    private static void WriteStack(SerializedProperty p, Item item, int amount)
    {
        p.FindPropertyRelative("_item").objectReferenceValue = item;
        p.FindPropertyRelative("_amount").intValue = amount;
        p.FindPropertyRelative("_durabilityRemaining").floatValue = item.HasDurability ? item.MaxDurability : -1f;
    }

    // ------------------------------------------------------------------ mundo: pickup / storage

    private void ApplyPickup()
    {
        _log.Clear();
        if (_pickupItem == null) { Err("Escolhe um item."); return; }

        bool any = false;
        foreach (GameObject go in SelectedObjects())
        {
            ItemPickup pickup = GetOrAdd<ItemPickup>(go);

            var so = new SerializedObject(pickup);
            so.FindProperty("_item").objectReferenceValue = _pickupItem;
            so.FindProperty("_amount").intValue = Mathf.Max(1, _pickupAmount);
            if (!string.IsNullOrEmpty(_pickupPrompt)) so.FindProperty("_prompt").stringValue = _pickupPrompt;
            so.ApplyModifiedProperties();

            EnsureCollider(go);
            MarkDirty(go);
            Ok($"{go.name}: pickup de {_pickupAmount}x {_pickupItem.name}");
            any = true;
        }

        if (any) AssetDatabase.SaveAssets();
    }

    private void ApplyStorage()
    {
        _log.Clear();
        if (Object.FindFirstObjectByType<StorageUI>() == null)
            Warn("Não há nenhum StorageUI na cena aberta: o storage não abre sem ele.");

        bool any = false;
        foreach (GameObject go in SelectedObjects())
        {
            StorageContainer container = GetOrAdd<StorageContainer>(go);   // [RequireComponent] junta o Inventory
            Inventory inventory = GetOrAdd<Inventory>(go);

            var invSo = new SerializedObject(inventory);
            invSo.FindProperty("_size").intValue = Mathf.Max(1, _storageSize);
            invSo.ApplyModifiedProperties();

            var so = new SerializedObject(container);
            so.FindProperty("_title").stringValue = _storageTitle;
            so.FindProperty("_closeDistance").floatValue = Mathf.Max(0.5f, _storageDistance);
            so.ApplyModifiedProperties();

            EnsureCollider(go);
            MarkDirty(go);
            Ok($"{go.name}: storage '{_storageTitle}' com {_storageSize} slots");
            any = true;
        }

        if (any) AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ mundo: targets + regras

    private void ApplyTargets()
    {
        _log.Clear();
        if (!PrepareFolder()) return;
        ApplySpecs(_textMode ? SpecsFromText() : SpecsFromRows());
    }

    private List<RuleSpec> SpecsFromText()
    {
        Dictionary<string, Item> items = IndexItems();
        Dictionary<string, ItemTag> tags = Index<ItemTag>();

        var specs = new List<RuleSpec>();
        foreach (Line line in ParseLines(_rulesText))
        {
            ItemTag tag = GetOrCreateTag(line.Name, tags);
            if (tag == null) { Err(line, "etiqueta inválida."); continue; }

            var spec = new RuleSpec { Tag = tag, Consume = ParseBool(line.Get("consume"), true) };

            string result = line.Get("result");
            if (result != null && !IsOff(result))
            {
                if (items.TryGetValue(Norm(result), out Item r) && r != null) spec.Result = r;
                else { Err(line, $"result '{result}' não encontrado."); continue; }
            }

            string on = line.Get("on");
            if (!string.IsNullOrEmpty(on))
            {
                int colon = on.IndexOf(':');
                spec.Action = (colon < 0 ? on : on.Substring(0, colon)).Trim().ToLowerInvariant();
                spec.Arg = colon < 0 ? "" : on.Substring(colon + 1).Trim();
                if (!new[] { "none", "enable", "disable", "trigger" }.Contains(spec.Action))
                {
                    Err(line, $"ação 'on={on}' desconhecida (usa enable, disable ou trigger).");
                    continue;
                }
                if (spec.Action == "trigger" && spec.Arg.Length == 0)
                {
                    Err(line, "trigger precisa de um nome (on=trigger:Open).");
                    continue;
                }
            }

            specs.Add(spec);
        }

        return specs;
    }

    private void ApplySpecs(List<RuleSpec> specs)
    {
        if (specs.Count == 0) { Warn("Nenhuma regra válida."); return; }

        bool any = false;
        foreach (GameObject go in SelectedObjects())
        {
            ItemTarget target = GetOrAdd<ItemTarget>(go);

            var so = new SerializedObject(target);
            SerializedProperty rules = so.FindProperty("_rules");
            if (rules == null)
            {
                Err("O campo _rules não é serializável: adiciona [System.Serializable] à classe ItemUseRule.");
                return;
            }

            int start = _replaceRules ? 0 : rules.arraySize;
            rules.arraySize = start + specs.Count;

            for (int i = 0; i < specs.Count; i++)
            {
                RuleSpec spec = specs[i];
                SerializedProperty el = rules.GetArrayElementAtIndex(start + i);
                el.FindPropertyRelative("_requiredTag").objectReferenceValue = spec.Tag;
                el.FindPropertyRelative("_consumeItem").boolValue = spec.Consume;
                el.FindPropertyRelative("_result").objectReferenceValue = spec.Result;
                WriteAction(el.FindPropertyRelative("_onUsed"), go, spec);
            }

            so.ApplyModifiedProperties();
            EnsureCollider(go);
            MarkDirty(go);
            Ok($"{go.name}: {specs.Count} regra(s) {(_replaceRules ? "definida(s)" : "acrescentada(s)")}");
            any = true;
        }

        if (any) AssetDatabase.SaveAssets();
    }

    /// <summary>Escreve a ação de um OnUsed como chamada persistente (igual à feita no Inspector).</summary>
    private void WriteAction(SerializedProperty onUsed, GameObject self, RuleSpec spec)
    {
        SerializedProperty calls = onUsed?.FindPropertyRelative("m_PersistentCalls.m_Calls");
        if (calls == null) { Warn("Não foi possível escrever o evento OnUsed."); return; }

        calls.arraySize = 0;

        switch (spec.Action)
        {
            case "enable":
            case "disable":
            {
                GameObject go = self;
                if (!string.IsNullOrEmpty(spec.Arg))
                {
                    Transform child = FindDeep(self.transform, spec.Arg);
                    if (child == null) { Warn($"{self.name}: filho '{spec.Arg}' não encontrado; evento ignorado."); return; }
                    go = child.gameObject;
                }

                bool enable = spec.Action == "enable";
                AddCall(calls, go, typeof(GameObject), "SetActive", 6,
                        args => args.FindPropertyRelative("m_BoolArgument").boolValue = enable);
                break;
            }

            case "trigger":
            {
                Animator anim = self.GetComponentInChildren<Animator>(true);
                if (anim == null) anim = self.GetComponentInParent<Animator>();
                if (anim == null) { Warn($"{self.name}: não há Animator; evento ignorado."); return; }

                string trigger = spec.Arg;
                AddCall(calls, anim, typeof(Animator), "SetTrigger", 5,
                        args => args.FindPropertyRelative("m_StringArgument").stringValue = trigger);
                break;
            }
        }
    }

    // PersistentListenerMode: 0 EventDefined, 1 Void, 2 Object, 3 Int, 4 Float, 5 String, 6 Bool
    private static void AddCall(SerializedProperty calls, Object target, Type type, string method,
                                int mode, Action<SerializedProperty> setArgs)
    {
        calls.arraySize++;
        SerializedProperty call = calls.GetArrayElementAtIndex(calls.arraySize - 1);

        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = $"{type.FullName}, {type.Assembly.GetName().Name}";
        call.FindPropertyRelative("m_MethodName").stringValue = method;
        call.FindPropertyRelative("m_Mode").enumValueIndex = mode;
        call.FindPropertyRelative("m_CallState").enumValueIndex = 2;   // RuntimeOnly

        SerializedProperty args = call.FindPropertyRelative("m_Arguments");
        args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = "UnityEngine.Object, UnityEngine.CoreModule";
        setArgs(args);
    }

    // ------------------------------------------------------------------ helpers de cena

    private IEnumerable<GameObject> SelectedObjects()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0) Warn("Seleciona primeiro um ou mais objetos (cena ou prefab).");
        return selected;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
        => go.TryGetComponent(out T existing) ? existing : ObjectFactory.AddComponent<T>(go);

    /// <summary>O raycast do jogador precisa de um collider por baixo do alvo.</summary>
    private void EnsureCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>(true) != null) return;

        BoxCollider box = ObjectFactory.AddComponent<BoxCollider>(go);
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            box.center = mf.sharedMesh.bounds.center;
            box.size = mf.sharedMesh.bounds.size;
        }
        Warn($"{go.name}: não tinha Collider; adicionei um BoxCollider (confirma o tamanho).");
    }

    private static void MarkDirty(GameObject go)
    {
        EditorUtility.SetDirty(go);
        if (go.scene.IsValid() && !EditorApplication.isPlaying)
            EditorSceneManager.MarkSceneDirty(go.scene);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        string n = Norm(name);
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (Norm(t.name) == n) return t;
        return null;
    }

    // ------------------------------------------------------------------ helpers de assets e texto

    private bool PrepareFolder()
    {
        _folder = (_folder ?? "").Replace('\\', '/').Trim().TrimEnd('/');
        if (_folder != "Assets" && !_folder.StartsWith("Assets/"))
        {
            Err("A pasta de destino tem de estar dentro de Assets (ex.: Assets/Items).");
            return false;
        }
        EnsureFolder(_folder);
        return true;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(parent)) return;

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private void FinishAssets(List<Object> touched)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (touched.Count > 0)
        {
            Selection.objects = touched.ToArray();
            EditorGUIUtility.PingObject(touched[0]);
        }
    }

    /// <summary>Nome sem espaços/símbolos e em minúsculas, para comparar nomes de forma tolerante.</summary>
    private static string Norm(string s) => Regex.Replace(s ?? "", @"[^\p{L}\p{Nd}]", "").ToLowerInvariant();

    /// <summary>Nome de ficheiro: "Energy Bar" -> "EnergyBar".</summary>
    private static string FileName(string s) => Regex.Replace((s ?? "").Trim(), @"[^\p{L}\p{Nd}_]", "");

    private static bool IsOff(string v)
    {
        v = (v ?? "").Trim().ToLowerInvariant();
        return v == "0" || v == "false" || v == "no" || v == "não" || v == "nao" || v == "none" || v == "off" || v == "null";
    }

    private static bool ParseBool(string v, bool fallback)
    {
        if (string.IsNullOrEmpty(v)) return fallback;
        v = v.Trim().ToLowerInvariant();
        if (v == "1" || v == "true" || v == "yes" || v == "sim" || v == "on") return true;
        if (IsOff(v)) return false;
        return fallback;
    }

    private static List<Line> ParseLines(string text)
    {
        var result = new List<Line>();
        if (string.IsNullOrEmpty(text)) return result;

        string[] raw = text.Replace("\r", "").Split('\n');
        for (int i = 0; i < raw.Length; i++)
        {
            string l = raw[i].Trim();
            if (l.Length == 0 || l.StartsWith("#") || l.StartsWith("//")) continue;

            string[] parts = l.Split('|');
            var line = new Line { Number = i + 1, Name = parts[0].Trim() };

            for (int p = 1; p < parts.Length; p++)
            {
                string part = parts[p].Trim();
                if (part.Length == 0) continue;

                int eq = part.IndexOf('=');
                string key = (eq < 0 ? part : part.Substring(0, eq)).Trim().ToLowerInvariant();
                string val = eq < 0 ? "true" : part.Substring(eq + 1).Trim();
                line.Opts[key] = val;
            }
            result.Add(line);
        }
        return result;
    }

    /// <summary>Todos os assets de um tipo, indexados por nome normalizado.</summary>
    private static Dictionary<string, T> Index<T>() where T : Object
    {
        var map = new Dictionary<string, T>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            var obj = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (obj != null) map[Norm(obj.name)] = obj;
        }
        return map;
    }

    /// <summary>Itens indexados pelo nome do asset e, em segundo lugar, pelo nome mostrado.</summary>
    private static Dictionary<string, Item> IndexItems()
    {
        Dictionary<string, Item> map = Index<Item>();
        foreach (Item item in map.Values.ToList())
        {
            string display = Norm(item.DisplayName);
            if (display.Length > 0 && !map.ContainsKey(display)) map[display] = item;
        }
        return map;
    }

    private static T FindByName<T>(ref Dictionary<string, string> cache, string filter, string name) where T : Object
    {
        if (cache == null)
        {
            cache = new Dictionary<string, string>();
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;

                string key = Norm(Path.GetFileNameWithoutExtension(path));
                if (!cache.ContainsKey(key)) cache[key] = path;
            }
        }

        return cache.TryGetValue(Norm(name), out string found) ? AssetDatabase.LoadAssetAtPath<T>(found) : null;
    }
}
