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
        private Toggle disableOtherToggle;
        private IntegerField orderIntField;

        private Button executeOrderMinBtn;
        private Button executeOrderMaxBtn;

        // Start is called before the first frame update
        void OnEnable()
        {
            this.name = "ShaderStrippingConfig";
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.utj.stripvariant/Editor/UXML/ConfigUI.uxml");

            this.rootVisualElement.Add(tree.CloneTree());

            enableToggle = this.rootVisualElement.Q<Toggle>("Enabled");
            logToggle = this.rootVisualElement.Q<Toggle>("LogEnable");
            strictModeToggle = this.rootVisualElement.Q<Toggle>("StrictVariantStripping");
            disableOtherToggle = this.rootVisualElement.Q<Toggle>("DisalbeOthers");

            orderIntField = this.rootVisualElement.Q<IntegerField>("ExecuteOrder");
            executeOrderMinBtn = this.rootVisualElement.Q<Button>("ExecOrderMinBtn");
            executeOrderMaxBtn = this.rootVisualElement.Q<Button>("ExecOrderMaxBtn");


            enableToggle.SetValueWithoutNotify(StripShaderConfig.IsEnable);
            logToggle.SetValueWithoutNotify(StripShaderConfig.IsLogEnable);
            strictModeToggle.SetValueWithoutNotify(StripShaderConfig.StrictVariantStripping);
            disableOtherToggle.SetValueWithoutNotify(StripShaderConfig.DisableOtherStipper);
            orderIntField.SetValueWithoutNotify(StripShaderConfig.Order);

            enableToggle.RegisterValueChangedCallback(OnChangeEnabbleToggle);
            logToggle.RegisterValueChangedCallback(OnChangeLogEnabbleToggle);
            strictModeToggle.RegisterValueChangedCallback(OnChangeStrictModeToggle);
            disableOtherToggle.RegisterValueChangedCallback(OnChangeDisableOthersToggle);

            orderIntField.RegisterCallback<FocusOutEvent>(OnLostFocusIntField);
            executeOrderMinBtn.clicked += OnClickMinButton;
            executeOrderMaxBtn.clicked += OnClickMaxButton;

            SetUIActiveAtEnabled(enableToggle.value);
            SetUIActiveAtStrictMode(strictModeToggle.value);

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
        private void OnChangeDisableOthersToggle(ChangeEvent<bool> val)
        {
            StripShaderConfig.DisableOtherStipper = val.newValue;
        }

        private void SetUIActiveAtEnabled(bool enabled)
        {
            strictModeToggle.SetEnabled(enabled);
            disableOtherToggle.SetEnabled(enabled);
            orderIntField.SetEnabled(enabled);

            orderIntField.SetEnabled(enabled);
            executeOrderMinBtn.SetEnabled(enabled);
            executeOrderMaxBtn.SetEnabled(enabled);
        }

        private void SetUIActiveAtStrictMode(bool enabled)
        {
            disableOtherToggle.SetEnabled(enabled);
            disableOtherToggle.SetValueWithoutNotify(StripShaderConfig.DisableOtherStipper);
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
    }
}
