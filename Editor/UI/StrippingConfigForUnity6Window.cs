using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Rendering;

namespace UTJ.ShaderVariantStripping
{

    internal class StrippingConfigForUnity6Window : EditorWindow
    {
        [MenuItem("Tools/UTJ/ShaderStrippingConfig")]
        public static void Create()
        {
            EditorWindow.GetWindow<StrippingConfigForUnity6Window>();
        }

        private Toggle enableToggle;
        private Toggle logToggle;
        private Toggle strictModeToggle;
        private Toggle disableUnityStrip;
        private IntegerField orderIntField;

        // from U6
        private Toggle useShaderVariantCollection;
        private Toggle useGraphicsStateCollection;
        private Toggle GSCmatchGraphicsAPIOnly;
        private Toggle GSCmatchTargetPlatformOnly;
        private Button addExcludeGSCBtn;
        private ListView excludeGSCListView;
        private Toggle safeMode;


        private Button executeOrderMinBtn;
        private Button executeOrderMaxBtn;

        private Button resetTimestampBtn;

        private Button addExcludeSVCBtn;
        private ListView excludeSVCListView;
        private Button debugListViewBtn;
        private Button debugShaderKeywordBtn;

        private List<ShaderVariantCollection> svcAssets;
        private List<GraphicsStateCollection> gscAssets;

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
            this.safeMode = this.rootVisualElement.Q<Toggle>("SafeMode");

            this.enableToggle.SetValueWithoutNotify(StripShaderConfig.IsEnable);
            this.logToggle.SetValueWithoutNotify(StripShaderConfig.IsLogEnable);
            this.strictModeToggle.SetValueWithoutNotify(StripShaderConfig.StrictVariantStripping);
            this.disableUnityStrip.SetValueWithoutNotify(StripShaderConfig.DisableUnityStrip);
            this.orderIntField.SetValueWithoutNotify(StripShaderConfig.Order);
            //from U6
            this.useShaderVariantCollection.SetValueWithoutNotify(StripShaderConfig.UseSVC);
            this.useGraphicsStateCollection.SetValueWithoutNotify(StripShaderConfig.UseGSC);
            this.GSCmatchGraphicsAPIOnly.SetValueWithoutNotify(StripShaderConfig.MatchGSCGraphicsAPI);
            this.GSCmatchTargetPlatformOnly.SetValueWithoutNotify(StripShaderConfig.MatchGSCPlatform);
            this.safeMode.SetValueWithoutNotify( StripShaderConfig.SafeMode );



            this.enableToggle.RegisterValueChangedCallback(OnChangeEnabbleToggle);
            this.logToggle.RegisterValueChangedCallback(OnChangeLogEnabbleToggle);
            this.strictModeToggle.RegisterValueChangedCallback(OnChangeStrictModeToggle);
            this.disableUnityStrip.RegisterValueChangedCallback(OnChangeDisableUnityStripToggle);


            //from U6
            this.useShaderVariantCollection.RegisterValueChangedCallback(OnChangeUseSVC);
            this.useGraphicsStateCollection.RegisterValueChangedCallback(OnChangeUseGSC);
            this.GSCmatchGraphicsAPIOnly.RegisterValueChangedCallback(OnChangeGSCMatchGraphicsAPI);
            this.GSCmatchTargetPlatformOnly.RegisterValueChangedCallback(OnChangeGSCMatchPlatform);
            this.safeMode.RegisterValueChangedCallback(OnChangeSafeMode);



            this.resetTimestampBtn.clicked += OnClickResetTimestamp;

            this.debugListViewBtn.clicked += OnClickDebugListViewBtn;
            this.debugShaderKeywordBtn.clicked += OnClickShaderKeywordDebugBtn;

            this.orderIntField.RegisterCallback<FocusOutEvent>(OnLostFocusIntField);
            this.executeOrderMinBtn.clicked += OnClickMinButton;
            this.executeOrderMaxBtn.clicked += OnClickMaxButton;
            this.addExcludeSVCBtn.clicked += OnClickAddExcludeSVC;
            this.addExcludeGSCBtn.clicked += OnClickAddExcludeGSC;

            SetUIActiveAtEnabled(enableToggle.value);
            SetUIActiveAtStrictMode(strictModeToggle.value);

            SetupExcludeSVCRules();
            this.SetupExcludeGSCRules();
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

        private void OnChangeSafeMode(ChangeEvent<bool> val)
        {
            StripShaderConfig.SafeMode = val.newValue;
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

        #region SVC Rules

        private void SetupExcludeSVCRules()
        {
            this.svcAssets = StripShaderConfig.GetExcludeVariantCollectionAsset();
            excludeSVCListView.fixedItemHeight = 20;
            excludeSVCListView.reorderable = true;

            excludeSVCListView.makeItem = () =>
            {
                return new SVCListItem(this.OnChangeSVCExclueValue, this.OnRemoveExcludeSVC);
            };
            excludeSVCListView.bindItem = (e, i) => {
                var variantUI = (e as SVCListItem);
                variantUI.assetData = this.svcAssets[i];
                variantUI.ListIndex = i;
            };
            excludeSVCListView.itemsSource = svcAssets;

            RefleshExcludeUI(this.excludeSVCListView, svcAssets.Count);
        }

        private void OnClickAddExcludeSVC()
        {
            svcAssets.Add(null); 
            RefleshExcludeUI(this.excludeSVCListView, svcAssets.Count);

        }
        private void OnChangeSVCExclueValue(SVCListItem variantCollectionUI)
        {
            svcAssets[variantCollectionUI.ListIndex] = variantCollectionUI.assetData;
            StripShaderConfig.SetExcludeVariantCollection(this.svcAssets);
        }

        private void OnRemoveExcludeSVC(SVCListItem variantCollectionUI)
        {
            svcAssets.RemoveAt(variantCollectionUI.ListIndex);
            RefleshExcludeUI(this.excludeSVCListView, svcAssets.Count);
            StripShaderConfig.SetExcludeVariantCollection(this.svcAssets);
        }


        #endregion SVC Rules



        #region GSC Rules
        // from U6
        private void SetupExcludeGSCRules()
        {
            this.gscAssets = StripShaderConfig.GetExcludeGSC();
            excludeGSCListView.fixedItemHeight = 20;
            excludeGSCListView.reorderable = true;

            excludeGSCListView.makeItem = () =>
            {
                return new GSCListItem(OnChangeGSCExclueValue, OnRemoveExcludeGSC);
            };
            excludeGSCListView.bindItem = (e, i) => {
                var variantUI = (e as GSCListItem);
                variantUI.assetData = gscAssets[i];
                variantUI.ListIndex = i;
            };
            excludeGSCListView.itemsSource = gscAssets;

            RefleshExcludeUI(this.excludeGSCListView, gscAssets.Count);
        }

        private void OnClickAddExcludeGSC()
        {
            gscAssets.Add(null);
            RefleshExcludeUI(this.excludeGSCListView, gscAssets.Count);

        }
        private void OnChangeGSCExclueValue(GSCListItem variantCollectionUI)
        {
            gscAssets[variantCollectionUI.ListIndex] = variantCollectionUI.assetData;
            StripShaderConfig.SetExcludeGSC(this.gscAssets);
        }

        private void OnRemoveExcludeGSC(GSCListItem variantCollectionUI)
        {
            gscAssets.RemoveAt(variantCollectionUI.ListIndex);
            RefleshExcludeUI(this.excludeGSCListView, gscAssets.Count);
            StripShaderConfig.SetExcludeGSC(this.gscAssets);
        }

        #endregion GSC Rules

        private void RefleshExcludeUI(ListView listView,int count)
        {
            listView.Rebuild();
            if (count== 0)
            {
                listView.style.height = listView.fixedItemHeight;
            }
            else
            {
                listView.style.height = listView.fixedItemHeight * count;
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
            StripShaderConfig.SetExcludeVariantCollection(this.svcAssets);
        }
    }
}
