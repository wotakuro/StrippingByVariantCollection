using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UTJ
{
    public class ShaderKeywordDebugWindow : EditorWindow
    {
        private ScrollView keywordsView;
        private ScrollView programsView;

        private void OnEnable()
        {
            var objField = new ObjectField("TargetShader");
            objField.objectType = typeof(Shader);

            objField.RegisterValueChangedCallback(OnChangeShader);
            this.rootVisualElement.Add(objField);

            this.keywordsView = new ScrollView();
            this.rootVisualElement.Add(keywordsView);

            this.programsView = new ScrollView();
            this.rootVisualElement.Add(programsView);

        }

        private void OnChangeShader(ChangeEvent<Object> evt)
        {
            var newShader = evt.newValue as Shader;
            keywordsView.Clear();
            programsView.Clear();
            if (newShader == null)
            {
                return;
            }
            var getter = new ShaderKeywordMaskGetter(newShader);
            SetupKeywordList(getter);
            //SetupProgramList(getter);
        }

        #region KEYWORD_LIST
        private void SetupKeywordList(ShaderKeywordMaskGetter getter) { 
            var allKeywords = getter.allKeywords;
            var listResultElement = new VisualElement();
            var keywordsElement = new VisualElement();
            var vertResultElement = new VisualElement();
            var fragResultElement = new VisualElement();
            var geometryResultElement = new VisualElement();
            var hullResultElement = new VisualElement();
            var domainResultElement = new VisualElement();
            var raytraceResultElement = new VisualElement();

            // setup boarder
            SetupBoarder(keywordsElement);
            SetupBoarder(vertResultElement);
            SetupBoarder(fragResultElement);
            SetupBoarder(geometryResultElement);
            SetupBoarder(hullResultElement);
            SetupBoarder(domainResultElement);
            SetupBoarder(raytraceResultElement, true);
            // headers
            keywordsElement.Add(CreateTableElementLabel("keyword name"));
            vertResultElement.Add(CreateTableElementLabel("vertex"));
            fragResultElement.Add(CreateTableElementLabel("flagment"));
            geometryResultElement.Add(CreateTableElementLabel("geometry"));
            hullResultElement.Add(CreateTableElementLabel("hull"));
            domainResultElement.Add(CreateTableElementLabel("domain"));
            raytraceResultElement.Add(CreateTableElementLabel("raytrace"));
            //
            foreach (var keyword in allKeywords )
            {
                keywordsElement.Add(CreateTableElementLabel(keyword));

                vertResultElement.Add(CreateYesNoLabel(getter.IsUsedForVertexProgram(keyword)));
                fragResultElement.Add(CreateYesNoLabel(getter.IsUsedForFragmentProgram(keyword)));
                geometryResultElement.Add(CreateYesNoLabel(getter.IsUsedForGeometryProgram(keyword)));
                hullResultElement.Add(CreateYesNoLabel(getter.IsUsedForHullProgram(keyword)));
                domainResultElement.Add(CreateYesNoLabel(getter.IsUsedForDomainProgram(keyword)));
                raytraceResultElement.Add(CreateYesNoLabel(getter.IsUsedForRaytraceProgram(keyword)));

            }
            listResultElement.style.flexDirection = FlexDirection.Row;

            listResultElement.Add(keywordsElement);
            listResultElement.Add(vertResultElement);
            listResultElement.Add(fragResultElement);
            listResultElement.Add(geometryResultElement);
            listResultElement.Add(hullResultElement);
            listResultElement.Add(domainResultElement);
            listResultElement.Add(raytraceResultElement);
            listResultElement.style.marginLeft = 20;
            listResultElement.style.marginTop = 10;
            keywordsView.Add(listResultElement);
        }

        private void SetupBoarder(VisualElement elem, bool isRight = false)
        {
            if (isRight)
            {
                elem.style.borderRightWidth = 3;
                elem.style.borderRightColor = Color.white;
            }
            elem.style.borderLeftWidth = 3;
            elem.style.borderLeftColor = Color.white;
        }

        private VisualElement CreateYesNoLabel(bool flag)
        {
            if (flag)
            {
                var label =  CreateTableElementLabel("TRUE");
                label.style.color = Color.red;
                return label;
            }
            else
            {
                var label = CreateTableElementLabel("FALSE");
                label.style.color = Color.blue;
                return label;
            }
        }

        private Label CreateTableElementLabel(string str)
        {
            Label label = new Label(str);
            label.style.paddingLeft = 10;
            label.style.paddingRight = 10;
            label.style.paddingTop = 5;
            label.style.paddingBottom = 5;

            label.style.borderTopWidth = 1;
            label.style.borderBottomWidth = 1;
            label.style.borderTopColor = Color.white;
            label.style.borderBottomColor = Color.white;
            return label;
        }
        #endregion KEYWORD_LIST

    }
}