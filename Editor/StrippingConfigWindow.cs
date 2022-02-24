using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UTJ.ShaderVariantStripping
{

    internal class StrippingConfigWindow : EditorWindow
    {
        [MenuItem("Tools/UTJ/ShaderStrippingConfig")]
        public static void Create()
        {
            EditorWindow.GetWindow<StrippingConfigWindow>();
        }

        private Toggle enableToggle;
        private Toggle logToggle;
        private Toggle strictModeToggle;
        private Toggle disableUnityStrip;
        private IntegerField orderIntField;

        private Button executeOrderMinBtn;
        private Button executeOrderMaxBtn;

        private Button resetTimestampBtn;

        private Button addExcludeBtn;
        private ListView excludeVariantListView;
        private Button debugListViewBtn;

        private List<ShaderVariantCollection> collections;

        // Start is called before the first frame update
        void OnEnable()
        {
            this.name = "ShaderStrippingConfig";
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.utj.stripvariant/Editor/UXML/ConfigUI.uxml");

            this.rootVisualElement.Add(tree.CloneTree());

            enableToggle = this.rootVisualElement.Q<Toggle>("Enabled");
            logToggle = this.rootVisualElement.Q<Toggle>("LogEnable");
            strictModeToggle = this.rootVisualElement.Q<Toggle>("StrictVariantStripping");
            disableUnityStrip = this.rootVisualElement.Q<Toggle>("DisableUnityStrip");

            orderIntField = this.rootVisualElement.Q<IntegerField>("ExecuteOrder");
            executeOrderMinBtn = this.rootVisualElement.Q<Button>("ExecOrderMinBtn");
            executeOrderMaxBtn = this.rootVisualElement.Q<Button>("ExecOrderMaxBtn");

            resetTimestampBtn = this.rootVisualElement.Q<Button>("ResetTimestampBtn");

            addExcludeBtn = this.rootVisualElement.Q<Button>("AppendExcludeBtn");
            excludeVariantListView = this.rootVisualElement.Q<ListView>("ExcludeList");

            debugListViewBtn = this.rootVisualElement.Q<Button>("DebugListProcessorBtn");


            enableToggle.SetValueWithoutNotify(StripShaderConfig.IsEnable);
            logToggle.SetValueWithoutNotify(StripShaderConfig.IsLogEnable);
            strictModeToggle.SetValueWithoutNotify(StripShaderConfig.StrictVariantStripping);
            disableUnityStrip.SetValueWithoutNotify(StripShaderConfig.DisableUnityStrip);
            orderIntField.SetValueWithoutNotify(StripShaderConfig.Order);

            enableToggle.RegisterValueChangedCallback(OnChangeEnabbleToggle);
            logToggle.RegisterValueChangedCallback(OnChangeLogEnabbleToggle);
            strictModeToggle.RegisterValueChangedCallback(OnChangeStrictModeToggle);
            disableUnityStrip.RegisterValueChangedCallback(OnChangeDisableUnityStripToggle);

            resetTimestampBtn.clicked += OnClickResetTimestamp;

            debugListViewBtn.clicked += OnClickDebugListViewBtn;

            orderIntField.RegisterCallback<FocusOutEvent>(OnLostFocusIntField);
            executeOrderMinBtn.clicked += OnClickMinButton;
            executeOrderMaxBtn.clicked += OnClickMaxButton;
            addExcludeBtn.clicked += OnClickAddExclude;

            SetUIActiveAtEnabled(enableToggle.value);
            SetUIActiveAtStrictMode(strictModeToggle.value);

            SetupExcludeRules();
            StrippingByVariantCollection.ResetData();
        }



        private void OnChangeEnabbleToggle(ChangeEvent<bool> val)
        {
            StripShaderConfig.IsEnable = val.newValue;
            SetUIActiveAtEnabled(val.newValue);
        }
        private void OnChangeLogEnabbleToggle(ChangeEvent<bool> val)
        {
            StripShaderConfig.IsLogEnable = val.newValue;
        }
        private void OnChangeStrictModeToggle(ChangeEvent<bool> val)
        {
            StripShaderConfig.StrictVariantStripping = val.newValue;
            SetUIActiveAtStrictMode(val.newValue);
        }
        private void OnChangeDisableUnityStripToggle(ChangeEvent<bool> val)
        {
            StripShaderConfig.DisableUnityStrip = val.newValue;
        }

        private void SetUIActiveAtEnabled(bool enabled)
        {
            strictModeToggle.SetEnabled(enabled);
            disableUnityStrip.SetEnabled(enabled);
            orderIntField.SetEnabled(enabled);

            orderIntField.SetEnabled(enabled);
            executeOrderMinBtn.SetEnabled(enabled);
            executeOrderMaxBtn.SetEnabled(enabled);
        }

        private void SetUIActiveAtStrictMode(bool enabled)
        {
            disableUnityStrip.SetEnabled(enabled);
            disableUnityStrip.SetValueWithoutNotify(StripShaderConfig.DisableUnityStrip);
        }

        private void OnLostFocusIntField(FocusOutEvent evt)
        {
            StripShaderConfig.Order = this.orderIntField.value;
        }

        private void OnClickMinButton()
        {
            StripShaderConfig.Order = int.MinValue;
            this.orderIntField.SetValueWithoutNotify(int.MinValue);
        }
        private void OnClickMaxButton()
        {
            StripShaderConfig.Order = int.MaxValue;
            this.orderIntField.SetValueWithoutNotify(int.MaxValue);
        }


        private void SetupExcludeRules()
        {
            this.collections = StripShaderConfig.GetVariantCollectionAsset();
#if UNITY_2021_2_OR_NEWER
            excludeVariantListView.fixedItemHeight = 20;
#else
            excludeVariantListView.itemHeight = 20;
#endif
            excludeVariantListView.reorderable = true;

            excludeVariantListView.makeItem = () =>
            {
                return new VariantCollectionUI(OnChangeExclueValue, OnRemoveExclude);
            };
            excludeVariantListView.bindItem = (e, i) => {
                var variantUI = (e as VariantCollectionUI);
                variantUI.variantCollection = collections[i];
                variantUI.ListIndex = i;
            };
            excludeVariantListView.itemsSource = collections;

            excludeVariantListView.Rebuild();
            RefleshExcludeUI();


        }

        private void OnClickAddExclude()
        {
            collections.Add(null); 
            RefleshExcludeUI();

        }
        private void OnChangeExclueValue(VariantCollectionUI variantCollectionUI)
        {
            collections[variantCollectionUI.ListIndex] = variantCollectionUI.variantCollection;
            StripShaderConfig.SetVariantCollection(this.collections);
        }

        private void OnRemoveExclude(VariantCollectionUI variantCollectionUI)
        {
            collections.RemoveAt(variantCollectionUI.ListIndex);
            RefleshExcludeUI();
            StripShaderConfig.SetVariantCollection(this.collections);
        }

        private void RefleshExcludeUI()
        {
#if UNITY_2021_2_OR_NEWER
            excludeVariantListView.Rebuild();
            if (collections.Count == 0)
            {
                excludeVariantListView.style.height = excludeVariantListView.fixedItemHeight;
            }
            else
            {
                excludeVariantListView.style.height = excludeVariantListView.fixedItemHeight * collections.Count;
            }
#else
            excludeVariantListView.Refresh();
            if (collections.Count == 0)
            {
                excludeVariantListView.style.height = excludeVariantListView.itemHeight;
            }
            else
            {
                excludeVariantListView.style.height = excludeVariantListView.itemHeight * collections.Count;
            }
#endif

        }

        void OnClickResetTimestamp()
        {
            StrippingByVariantCollection.ResetData();

        }

        private void OnClickDebugListViewBtn()
        {
            ListShaderPreProcessClasses.ShowDebugWindow();
        }

        void OnDisable()
        {
            StripShaderConfig.SetVariantCollection(this.collections);
        }
    }
}
