#if UNITY_6000_0_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UTJ.ShaderVariantStripping
{

    internal class StrippingConfigForUnity6Window : EditorWindow
    {
        [MenuItem("Tools/UTJ/ShaderStrippingConfigU6")]
        public static void Create()
        {
            EditorWindow.GetWindow<StrippingConfigForUnity6Window>();
        }

        private Toggle enableToggle;
        private Toggle logToggle;
        private Toggle strictModeToggle;
        private Toggle disableUnityStrip;
        private Toggle ignoreStageOnlyKeyword;
        private IntegerField orderIntField;

        // from U6
        private Toggle useShaderVariantCollection;
        private Toggle useGraphicsStateCollection;
        private Toggle GSCmatchGraphicsAPIOnly;
        private Toggle GSCmatchTargetPlatformOnly;
        private Button addExcludeGSCBtn;
        private ListView excludeGSCListView;


        private Button executeOrderMinBtn;
        private Button executeOrderMaxBtn;

        private Button resetTimestampBtn;

        private Button addExcludeSVCBtn;
        private ListView excludeSVCListView;
        private Button debugListViewBtn;
        private Button debugShaderKeywordBtn;

        private List<ShaderVariantCollection> collections;

        // Start is called before the first frame update
        void OnEnable()
        {
            this.name = "ShaderStrippingConfig";
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.utj.stripvariant/Editor/UXML/ConfigUI_Unity6.uxml");

            this.rootVisualElement.Add(tree.CloneTree());

            this.enableToggle = this.rootVisualElement.Q<Toggle>("Enabled");
            this.logToggle = this.rootVisualElement.Q<Toggle>("LogEnable");
            this.strictModeToggle = this.rootVisualElement.Q<Toggle>("StrictVariantStripping");
            this.disableUnityStrip = this.rootVisualElement.Q<Toggle>("DisableUnityStrip");
            this.ignoreStageOnlyKeyword = this.rootVisualElement.Q<Toggle>("IgnoreStgeOnlyKeyword");

            this.orderIntField = this.rootVisualElement.Q<IntegerField>("ExecuteOrder");
            this.executeOrderMinBtn = this.rootVisualElement.Q<Button>("ExecOrderMinBtn");
            this.executeOrderMaxBtn = this.rootVisualElement.Q<Button>("ExecOrderMaxBtn");

            this.resetTimestampBtn = this.rootVisualElement.Q<Button>("ResetTimestampBtn");

            this.addExcludeSVCBtn = this.rootVisualElement.Q<Button>("AppendExcludeBtn");
            this.excludeSVCListView = this.rootVisualElement.Q<ListView>("ExcludeList");

            this.debugListViewBtn = this.rootVisualElement.Q<Button>("DebugListProcessorBtn");
            this.debugShaderKeywordBtn = this.rootVisualElement.Q<Button>("DebugShaderKeywords");

            // From U6
            this.useShaderVariantCollection = this.rootVisualElement.Q<Toggle>("UseSVC");
            this.useGraphicsStateCollection = this.rootVisualElement.Q<Toggle>("UseGSC");
            this.GSCmatchGraphicsAPIOnly = this.rootVisualElement.Q<Toggle>("matchGSCGraphicsAPI");        
            this.GSCmatchTargetPlatformOnly = this.rootVisualElement.Q<Toggle>("matchGSCPlatform");

            this.addExcludeGSCBtn = this.rootVisualElement.Q<Button>("AppendExcludeGSCBtn");
            this.excludeGSCListView = this.rootVisualElement.Q<ListView>("ExcludeGSCList");


            this.enableToggle.SetValueWithoutNotify(StripShaderConfig.IsEnable);
            this.logToggle.SetValueWithoutNotify(StripShaderConfig.IsLogEnable);
            this.strictModeToggle.SetValueWithoutNotify(StripShaderConfig.StrictVariantStripping);
            this.disableUnityStrip.SetValueWithoutNotify(StripShaderConfig.DisableUnityStrip);
            this.ignoreStageOnlyKeyword.SetValueWithoutNotify(StripShaderConfig.IgnoreStageOnlyKeyword);
            this.orderIntField.SetValueWithoutNotify(StripShaderConfig.Order);
            //from U6
            this.useShaderVariantCollection.SetValueWithoutNotify(StripShaderConfig.UseSVC);
            this.useGraphicsStateCollection.SetValueWithoutNotify(StripShaderConfig.UseGSC);
            this.GSCmatchGraphicsAPIOnly.SetValueWithoutNotify(StripShaderConfig.MatchGSCGraphicsAPI);
            this.GSCmatchTargetPlatformOnly.SetValueWithoutNotify(StripShaderConfig.MatchGSCPlatform);



            this.enableToggle.RegisterValueChangedCallback(OnChangeEnabbleToggle);
            this.logToggle.RegisterValueChangedCallback(OnChangeLogEnabbleToggle);
            this.strictModeToggle.RegisterValueChangedCallback(OnChangeStrictModeToggle);
            this.disableUnityStrip.RegisterValueChangedCallback(OnChangeDisableUnityStripToggle);
            this.ignoreStageOnlyKeyword.RegisterValueChangedCallback(OnChangeIgnoreStageOnlyKeywordToggle);


            //from U6
            this.useShaderVariantCollection.RegisterValueChangedCallback(OnChangeUseSVC);
            this.useGraphicsStateCollection.RegisterValueChangedCallback(OnChangeUseGSC);
            this.GSCmatchGraphicsAPIOnly.RegisterValueChangedCallback(OnChangeGSCMatchGraphicsAPI);
            this.GSCmatchTargetPlatformOnly.RegisterValueChangedCallback(OnChangeGSCMatchPlatform);



            this.resetTimestampBtn.clicked += OnClickResetTimestamp;

            this.debugListViewBtn.clicked += OnClickDebugListViewBtn;
            this.debugShaderKeywordBtn.clicked += OnClickShaderKeywordDebugBtn;

            this.orderIntField.RegisterCallback<FocusOutEvent>(OnLostFocusIntField);
            this.executeOrderMinBtn.clicked += OnClickMinButton;
            this.executeOrderMaxBtn.clicked += OnClickMaxButton;
            this.addExcludeSVCBtn.clicked += OnClickAddExcludeSVC;

            SetUIActiveAtEnabled(enableToggle.value);
            SetUIActiveAtStrictMode(strictModeToggle.value);

            SetupExcludeSVCRules();
            StripProcessShaders.ResetData();
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

        private void OnChangeIgnoreStageOnlyKeywordToggle(ChangeEvent<bool> val)
        {
            StripShaderConfig.IgnoreStageOnlyKeyword = val.newValue;
        }

        private void OnChangeUseSVC(ChangeEvent<bool> val)
        {
            StripShaderConfig.UseSVC = val.newValue;
        }
        private void OnChangeUseGSC(ChangeEvent<bool> val)
        {
            StripShaderConfig.UseGSC = val.newValue;
        }
        private void OnChangeGSCMatchGraphicsAPI(ChangeEvent<bool> val)
        {
            StripShaderConfig.MatchGSCGraphicsAPI = val.newValue;
        }
        private void OnChangeGSCMatchPlatform(ChangeEvent<bool> val)
        {
            StripShaderConfig.MatchGSCPlatform = val.newValue;
        }


        private void SetUIActiveAtEnabled(bool enabled)
        {
            strictModeToggle.SetEnabled(enabled);
            disableUnityStrip.SetEnabled(enabled);
            ignoreStageOnlyKeyword.SetEnabled(enabled);
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

        private void SetupExcludeSVCRules()
        {
            this.collections = StripShaderConfig.GetExcludeVariantCollectionAsset();
            excludeSVCListView.fixedItemHeight = 20;
            excludeSVCListView.reorderable = true;

            excludeSVCListView.makeItem = () =>
            {
                return new SVCListItem(OnChangeSVCExclueValue, OnRemoveExcludeSVC);
            };
            excludeSVCListView.bindItem = (e, i) => {
                var variantUI = (e as SVCListItem);
                variantUI.variantCollection = collections[i];
                variantUI.ListIndex = i;
            };
            excludeSVCListView.itemsSource = collections;

            RefleshExcludeUI(this.excludeSVCListView);
        }

        private void OnClickAddExcludeSVC()
        {
            collections.Add(null); 
            RefleshExcludeUI(this.excludeSVCListView);

        }
        private void OnChangeSVCExclueValue(SVCListItem variantCollectionUI)
        {
            collections[variantCollectionUI.ListIndex] = variantCollectionUI.variantCollection;
            StripShaderConfig.SetExcludeVariantCollection(this.collections);
        }

        private void OnRemoveExcludeSVC(SVCListItem variantCollectionUI)
        {
            collections.RemoveAt(variantCollectionUI.ListIndex);
            RefleshExcludeUI(this.excludeSVCListView);
            StripShaderConfig.SetExcludeVariantCollection(this.collections);
        }

        private void RefleshExcludeUI(ListView listView)
        {
            listView.Rebuild();
            if (collections.Count == 0)
            {
                listView.style.height = listView.fixedItemHeight;
            }
            else
            {
                listView.style.height = listView.fixedItemHeight * collections.Count;
            }
        }

        void OnClickResetTimestamp()
        {
            StripProcessShaders.ResetData();

        }

        private void OnClickDebugListViewBtn()
        {
            ListShaderPreProcessClasses.ShowDebugWindow();
        }

        private void OnClickShaderKeywordDebugBtn()
        {
            ShaderKeywordDebugWindow.CreateWindow<ShaderKeywordDebugWindow>();
        }


        void OnDisable()
        {
            StripShaderConfig.SetExcludeVariantCollection(this.collections);
        }
    }
}

#endif