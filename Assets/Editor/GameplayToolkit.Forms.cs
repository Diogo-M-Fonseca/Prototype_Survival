// GameplayToolkit.Forms.cs
// Segunda metade da janela (partial class). Colocar na mesma pasta Editor que o GameplayToolkit.cs.
// Contém o modo "Formulário": campos, dropdowns e listas em vez de texto.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public partial class GameplayToolkit
{
    // ------------------------------------------------------------------ dados dos formulários

    private enum ActionKind { Nenhuma, Ativar, Desativar, Trigger }

    [Serializable] private struct RestoreRow { public RestoreStat Stat; public int Amount; }
    [Serializable] private struct IngredientRow { public Item Item; public int Amount; }

    [Serializable]
    private struct RuleRow
    {
        public ItemTag Tag;
        public bool Consume;
        public Item Result;
        public ActionKind Action;
        public string Arg;
        public string Prompt;
    }

    [Serializable]
    private sealed class ItemForm
    {
        public Item Source;                 // item existente que está a ser editado (null = novo)
        public Kind Kind;
        public string Name;
        public string Description;
        public int Stack = 1;
        public GameObject Prefab;
        public Sprite Icon;
        public bool Durable;
        public int Durability = 100;
        public Item Leftover;
        public List<ItemTag> Tags = new List<ItemTag>();
        public List<RestoreRow> Restores = new List<RestoreRow>();
        public List<BaseMaterials> Materials = new List<BaseMaterials>();
    }

    [SerializeField] private bool _textMode;
    [SerializeField] private ItemForm _form = new ItemForm();
    [SerializeField] private string _newTagName;

    [SerializeField] private ItemRecipe _recipeSource;
    [SerializeField] private string _recipeName;
    [SerializeField] private List<IngredientRow> _recipeIngredients = new List<IngredientRow> { new IngredientRow { Amount = 1 } };
    [SerializeField] private IngredientRow _recipeResult = new IngredientRow { Amount = 1 };

    [SerializeField] private List<RuleRow> _ruleRows = new List<RuleRow> { new RuleRow { Consume = true } };

    // ------------------------------------------------------------------ formulário: item

    private void DrawItemForm()
    {
        ItemForm f = _form;
        DrawFolderField();

        EditorGUI.BeginChangeCheck();
        f.Source = (Item)EditorGUILayout.ObjectField("Editar existente", f.Source, typeof(Item), false);
        if (EditorGUI.EndChangeCheck() && f.Source != null) LoadForm(f.Source);

        EditorGUILayout.Space(4);

        using (new EditorGUI.DisabledScope(f.Source != null))
            f.Kind = (Kind)EditorGUILayout.EnumPopup("Tipo", f.Kind);

        f.Name = EditorGUILayout.TextField("Nome", f.Name);
        f.Description = EditorGUILayout.TextField("Descrição", f.Description);
        f.Stack = Mathf.Max(1, EditorGUILayout.IntField("Stack máximo", f.Stack));
        f.Prefab = (GameObject)EditorGUILayout.ObjectField("Prefab (mão / ícone)", f.Prefab, typeof(GameObject), false);
        f.Icon = (Sprite)EditorGUILayout.ObjectField("Ícone manual", f.Icon, typeof(Sprite), false);
        f.Leftover = (Item)EditorGUILayout.ObjectField("Item deixado ao gastar", f.Leftover, typeof(Item), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            f.Durable = EditorGUILayout.Toggle("Durabilidade", f.Durable);
            using (new EditorGUI.DisabledScope(!f.Durable))
                f.Durability = Mathf.Max(1, EditorGUILayout.IntField(f.Durability, GUILayout.Width(60)));
        }

        // ---- tags
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
        int removeTag = -1;
        for (int i = 0; i < f.Tags.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                f.Tags[i] = (ItemTag)EditorGUILayout.ObjectField(f.Tags[i], typeof(ItemTag), false);
                if (GUILayout.Button("−", GUILayout.Width(24))) removeTag = i;
            }
        }
        if (removeTag >= 0) f.Tags.RemoveAt(removeTag);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ Tag", GUILayout.Width(60))) f.Tags.Add(null);
            _newTagName = EditorGUILayout.TextField(_newTagName);
            if (GUILayout.Button("Criar tag nova", GUILayout.Width(100))) CreateTagForForm();
        }

        // ---- efeitos (comida/bebida) ou materiais (outros)
        EditorGUILayout.Space(4);
        if (f.Kind == Kind.Food)
        {
            EditorGUILayout.LabelField("Efeitos ao usar (máx. 3)", EditorStyles.boldLabel);
            int removeRestore = -1;
            for (int i = 0; i < f.Restores.Count; i++)
            {
                RestoreRow r = f.Restores[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    r.Stat = (RestoreStat)EditorGUILayout.EnumPopup(r.Stat, GUILayout.Width(110));
                    r.Amount = EditorGUILayout.IntSlider(r.Amount, 1, 100);
                    if (GUILayout.Button("−", GUILayout.Width(24))) removeRestore = i;
                }
                f.Restores[i] = r;
            }
            if (removeRestore >= 0) f.Restores.RemoveAt(removeRestore);

            using (new EditorGUI.DisabledScope(f.Restores.Count >= 3))
                if (GUILayout.Button("+ Efeito"))
                    f.Restores.Add(new RestoreRow { Stat = RestoreStat.Hunger, Amount = 10 });
        }
        else
        {
            EditorGUILayout.LabelField("Materiais (máx. 3)", EditorStyles.boldLabel);
            int removeMat = -1;
            for (int i = 0; i < f.Materials.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    f.Materials[i] = (BaseMaterials)EditorGUILayout.EnumPopup(f.Materials[i]);
                    if (GUILayout.Button("−", GUILayout.Width(24))) removeMat = i;
                }
            }
            if (removeMat >= 0) f.Materials.RemoveAt(removeMat);

            using (new EditorGUI.DisabledScope(f.Materials.Count >= 3))
                if (GUILayout.Button("+ Material")) f.Materials.Add(BaseMaterials.Wood);
        }

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(f.Source != null ? "Atualizar item" : "Criar item", GUILayout.Height(28)))
                ApplyItemForm();
            if (GUILayout.Button("Limpar", GUILayout.Width(70), GUILayout.Height(28)))
            {
                _form = new ItemForm();
                GUI.FocusControl(null);
            }
        }
    }

    private void LoadForm(Item item)
    {
        ItemForm f = _form;
        var so = new SerializedObject(item);

        f.Kind = item is FoodsAndDrinks ? Kind.Food : Kind.Other;
        f.Name = item.DisplayName;
        f.Description = so.FindProperty("_description").stringValue;
        f.Stack = Mathf.Max(1, so.FindProperty("_maxStack").intValue);
        f.Prefab = so.FindProperty("_heldPrefab").objectReferenceValue as GameObject;
        f.Icon = so.FindProperty("_icon").objectReferenceValue as Sprite;
        f.Durable = so.FindProperty("_hasDurability").boolValue;
        f.Durability = Mathf.Max(1, so.FindProperty("_maxDurability").intValue);
        f.Leftover = so.FindProperty("_leftoverItem").objectReferenceValue as Item;

        f.Tags = new List<ItemTag>();
        SerializedProperty tags = so.FindProperty("_tags");
        for (int i = 0; tags != null && i < tags.arraySize; i++)
        {
            ItemTag t = tags.GetArrayElementAtIndex(i).objectReferenceValue as ItemTag;
            if (t != null) f.Tags.Add(t);
        }

        f.Restores = new List<RestoreRow>();
        SerializedProperty restores = so.FindProperty("_restores");
        for (int i = 0; restores != null && i < restores.arraySize; i++)
        {
            SerializedProperty e = restores.GetArrayElementAtIndex(i);
            int amount = e.FindPropertyRelative("_amount").intValue;
            if (amount > 0)
                f.Restores.Add(new RestoreRow { Stat = (RestoreStat)e.FindPropertyRelative("_stat").enumValueIndex, Amount = amount });
        }

        f.Materials = new List<BaseMaterials>();
        SerializedProperty mats = so.FindProperty("_baseMaterials");
        for (int i = 0; mats != null && i < mats.arraySize; i++)
            f.Materials.Add((BaseMaterials)mats.GetArrayElementAtIndex(i).enumValueIndex);

        GUI.FocusControl(null);
    }

    private void ApplyItemForm()
    {
        ItemForm f = _form;
        string name = (f.Name ?? "").Trim();
        if (name.Length == 0) { _log.Clear(); Err("Escreve um nome para o item."); return; }

        var line = new Line { Number = 1, Name = name, Existing = f.Source };
        line.Opts["type"] = f.Kind == Kind.Food ? "food" : "other";
        line.Opts["display"] = name;
        line.Opts["desc"] = f.Description ?? "";
        line.Opts["stack"] = f.Stack.ToString();
        line.Opts["durability"] = f.Durable ? f.Durability.ToString() : "0";

        if (f.Kind == Kind.Food)
        {
            // Escreve sempre os três: o que não estiver nas linhas fica a 0 (= removido)
            var amounts = new Dictionary<RestoreStat, int>
            {
                { RestoreStat.Hunger, 0 }, { RestoreStat.Health, 0 }, { RestoreStat.Thirst, 0 }
            };
            foreach (RestoreRow r in f.Restores) amounts[r.Stat] = r.Amount;

            line.Opts["hunger"] = amounts[RestoreStat.Hunger].ToString();
            line.Opts["health"] = amounts[RestoreStat.Health].ToString();
            line.Opts["thirst"] = amounts[RestoreStat.Thirst].ToString();
        }
        else
        {
            line.Opts["mats"] = string.Join("+", f.Materials.Select(m => m.ToString()));
        }

        line.Refs["prefab"] = f.Prefab;
        line.Refs["icon"] = f.Icon;
        line.Refs["leftover"] = f.Leftover;
        line.TagRefs = f.Tags.Where(t => t != null).Distinct().ToList();

        ProcessItems(new List<Line> { line });

        // Depois de criar passa a editar o asset que acabou de nascer
        if (f.Source == null)
        {
            Item created = IndexItems().TryGetValue(Norm(name), out Item found) ? found : null;
            if (created != null) f.Source = created;
        }
    }

    private void CreateTagForForm()
    {
        _log.Clear();
        if (!PrepareFolder()) return;

        ItemTag tag = GetOrCreateTag(_newTagName, Index<ItemTag>());
        if (tag == null) { Warn("Escreve o nome da tag."); return; }

        if (!_form.Tags.Contains(tag)) _form.Tags.Add(tag);
        _newTagName = "";
        AssetDatabase.SaveAssets();
        GUI.FocusControl(null);
    }

    // ------------------------------------------------------------------ formulário: receita

    private void DrawRecipeForm()
    {
        DrawFolderField();

        EditorGUI.BeginChangeCheck();
        _recipeSource = (ItemRecipe)EditorGUILayout.ObjectField("Editar existente", _recipeSource, typeof(ItemRecipe), false);
        if (EditorGUI.EndChangeCheck() && _recipeSource != null) LoadRecipe(_recipeSource);

        EditorGUILayout.Space(4);
        using (new EditorGUI.DisabledScope(_recipeSource != null))
            _recipeName = EditorGUILayout.TextField("Nome da receita", _recipeName);

        EditorGUILayout.LabelField("Ingredientes", EditorStyles.boldLabel);
        int remove = -1;
        for (int i = 0; i < _recipeIngredients.Count; i++)
        {
            IngredientRow row = _recipeIngredients[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                row.Amount = Mathf.Max(1, EditorGUILayout.IntField(row.Amount, GUILayout.Width(40)));
                row.Item = (Item)EditorGUILayout.ObjectField(row.Item, typeof(Item), false);
                if (GUILayout.Button("−", GUILayout.Width(24))) remove = i;
            }
            _recipeIngredients[i] = row;
        }
        if (remove >= 0) _recipeIngredients.RemoveAt(remove);
        if (GUILayout.Button("+ Ingrediente")) _recipeIngredients.Add(new IngredientRow { Amount = 1 });

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Resultado", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            _recipeResult.Amount = Mathf.Max(1, EditorGUILayout.IntField(_recipeResult.Amount, GUILayout.Width(40)));
            _recipeResult.Item = (Item)EditorGUILayout.ObjectField(_recipeResult.Item, typeof(Item), false);
        }

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(_recipeSource != null ? "Atualizar receita" : "Criar receita", GUILayout.Height(28)))
                SaveRecipeForm();
            if (GUILayout.Button("Limpar", GUILayout.Width(70), GUILayout.Height(28)))
            {
                _recipeSource = null;
                _recipeName = "";
                _recipeIngredients = new List<IngredientRow> { new IngredientRow { Amount = 1 } };
                _recipeResult = new IngredientRow { Amount = 1 };
                GUI.FocusControl(null);
            }
        }
    }

    private void LoadRecipe(ItemRecipe recipe)
    {
        var so = new SerializedObject(recipe);
        _recipeName = recipe.name;

        _recipeIngredients = new List<IngredientRow>();
        SerializedProperty ing = so.FindProperty("_ingredients");
        for (int i = 0; ing != null && i < ing.arraySize; i++)
        {
            SerializedProperty e = ing.GetArrayElementAtIndex(i);
            _recipeIngredients.Add(new IngredientRow
            {
                Item = e.FindPropertyRelative("_item").objectReferenceValue as Item,
                Amount = Mathf.Max(1, e.FindPropertyRelative("_amount").intValue)
            });
        }
        if (_recipeIngredients.Count == 0) _recipeIngredients.Add(new IngredientRow { Amount = 1 });

        SerializedProperty res = so.FindProperty("_result");
        _recipeResult = new IngredientRow
        {
            Item = res.FindPropertyRelative("_item").objectReferenceValue as Item,
            Amount = Mathf.Max(1, res.FindPropertyRelative("_amount").intValue)
        };

        GUI.FocusControl(null);
    }

    private void SaveRecipeForm()
    {
        _log.Clear();
        if (!PrepareFolder()) return;

        string name = (_recipeName ?? "").Trim();
        if (_recipeSource == null && FileName(name).Length == 0) { Err("Escreve um nome para a receita."); return; }

        List<IngredientRow> ingredients = _recipeIngredients.Where(r => r.Item != null).ToList();
        if (ingredients.Count == 0) { Err("Escolhe pelo menos um ingrediente."); return; }
        if (_recipeResult.Item == null) { Err("Escolhe o item resultado."); return; }

        ItemRecipe recipe = _recipeSource;
        bool isNew = recipe == null;
        if (isNew)
        {
            string dir = _folder + "/Recipes";
            EnsureFolder(dir);
            recipe = ScriptableObject.CreateInstance<ItemRecipe>();
            AssetDatabase.CreateAsset(recipe, AssetDatabase.GenerateUniqueAssetPath($"{dir}/{FileName(name)}.asset"));
        }

        var so = new SerializedObject(recipe);
        SerializedProperty ing = so.FindProperty("_ingredients");
        ing.arraySize = ingredients.Count;
        for (int i = 0; i < ingredients.Count; i++)
            WriteStack(ing.GetArrayElementAtIndex(i), ingredients[i].Item, ingredients[i].Amount);
        WriteStack(so.FindProperty("_result"), _recipeResult.Item, _recipeResult.Amount);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(recipe);

        Ok($"{(isNew ? "criada" : "atualizada")}: {recipe.name}");
        _recipeSource = recipe;
        FinishAssets(new List<Object> { recipe });
    }

    // ------------------------------------------------------------------ formulário: regras do ItemTarget

    private void DrawRuleRows()
    {
        int remove = -1;
        for (int i = 0; i < _ruleRows.Count; i++)
        {
            RuleRow r = _ruleRows[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                r.Tag = (ItemTag)EditorGUILayout.ObjectField("Etiqueta exigida", r.Tag, typeof(ItemTag), false);
                if (GUILayout.Button("×", GUILayout.Width(24))) remove = i;
            }

            r.Consume = EditorGUILayout.Toggle("Gasta o item", r.Consume);
            using (new EditorGUI.DisabledScope(!r.Consume))
                r.Result = (Item)EditorGUILayout.ObjectField("Deixa no lugar", r.Result, typeof(Item), false);

            r.Action = (ActionKind)EditorGUILayout.EnumPopup("Ao usar", r.Action);
            if (r.Action == ActionKind.Ativar || r.Action == ActionKind.Desativar)
                r.Arg = EditorGUILayout.TextField("Filho (vazio = o próprio)", r.Arg);
            else if (r.Action == ActionKind.Trigger)
                r.Arg = EditorGUILayout.TextField("Nome do trigger (Animator)", r.Arg);
            r.Prompt = EditorGUILayout.TextField("Mensagem no retículo", r.Prompt);
            EditorGUILayout.EndVertical();

            _ruleRows[i] = r;
        }

        if (remove >= 0) _ruleRows.RemoveAt(remove);
        if (GUILayout.Button("+ Regra")) _ruleRows.Add(new RuleRow { Consume = true });
    }

    private List<RuleSpec> SpecsFromRows()
    {
        var specs = new List<RuleSpec>();

        for (int i = 0; i < _ruleRows.Count; i++)
        {
            RuleRow r = _ruleRows[i];
            if (r.Tag == null) { Err($"regra {i + 1}: escolhe uma etiqueta."); continue; }

            string arg = (r.Arg ?? "").Trim();
            if (r.Action == ActionKind.Trigger && arg.Length == 0)
            {
                Err($"regra {i + 1}: o trigger precisa de um nome.");
                continue;
            }

            string action;
            switch (r.Action)
            {
                case ActionKind.Ativar: action = "enable"; break;
                case ActionKind.Desativar: action = "disable"; break;
                case ActionKind.Trigger: action = "trigger"; break;
                default: action = "none"; break;
            }

            specs.Add(new RuleSpec
            {
                Tag = r.Tag,
                Consume = r.Consume,
                Result = r.Consume ? r.Result : null,
                Action = action,
                Arg = arg,
                Prompt = r.Prompt
            });
        }

        return specs;
    }
}
