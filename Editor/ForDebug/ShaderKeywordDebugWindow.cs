using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UTJ
{
    public class ShaderKeywordDebugWindow : EditorWindow
    {
        private ScrollView scrollView;

        private Foldout allShader;

        private void OnEnable()
        {
            var objField = new ObjectField("TargetShader");
            objField.objectType = typeof(Shader);

            objField.RegisterValueChangedCallback(OnChangeShader);
            this.rootVisualElement.Add(objField);

            this.scrollView = new ScrollView();
            this.rootVisualElement.Add(scrollView);


        }

        private void OnChangeShader(ChangeEvent<Object> evt)
        {
            var newShader = evt.newValue as Shader;
            scrollView.Clear();
            if (newShader == null)
            {
                return;
            }
            var getter = new ShaderKeywordMaskGetter(newShader);
            SetupWholeKeywordList(getter);
            SetupEachPassInfo(getter);
        }

        #region KEYWORD_LIST
        private void SetupWholeKeywordList(ShaderKeywordMaskGetter getter) { 
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

            allShader = new Foldout();
            allShader.Add(listResultElement);
            allShader.text = "Whole Shader";
            scrollView.Add(allShader);
        }

        private void SetupEachPassInfo(ShaderKeywordMaskGetter getter)
        {
            var allPasses = getter.allPasses;
            foreach (var pass in allPasses)
            {
                var passDetial = getter.GetPassDetail(pass);
                var foldOut = new Foldout();
                foldOut.text = pass.subShaderIndex + "-" + pass.passIndex +"  " + passDetial.passName;
                foldOut.value = false;

                var tagFold = new Foldout();
                tagFold.text = "Tags";
                foreach (var tag in passDetial.tags)
                {
                    tagFold.Add(new Label(tag) );
                }
                foldOut.Add(tagFold);
                var keywordFold = SetupPassKeywordList(getter, pass);
                foldOut.Add(keywordFold);
                this.scrollView.Add(foldOut);
            }
        }

        private Foldout SetupPassKeywordList(ShaderKeywordMaskGetter getter, ShaderKeywordMaskGetter.PassInfo passInfo)
        {
            Foldout foldout = new Foldout();
            foldout.text = "Keyword";

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
            foreach (var keyword in allKeywords)
            {
                keywordsElement.Add(CreateTableElementLabel(keyword));

                vertResultElement.Add(CreateYesNoLabel(getter.IsUsedForVertexProgram(passInfo.subShaderIndex,passInfo.passIndex,keyword)));
                fragResultElement.Add(CreateYesNoLabel(getter.IsUsedForFragmentProgram(passInfo.subShaderIndex, passInfo.passIndex, keyword)));
                geometryResultElement.Add(CreateYesNoLabel(getter.IsUsedForGeometryProgram(passInfo.subShaderIndex, passInfo.passIndex, keyword)));
                hullResultElement.Add(CreateYesNoLabel(getter.IsUsedForHullProgram(passInfo.subShaderIndex, passInfo.passIndex, keyword)));
                domainResultElement.Add(CreateYesNoLabel(getter.IsUsedForDomainProgram(passInfo.subShaderIndex, passInfo.passIndex, keyword)));
                raytraceResultElement.Add(CreateYesNoLabel(getter.IsUsedForRaytraceProgram(passInfo.subShaderIndex, passInfo.passIndex, keyword)));

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

            foldout.Add(listResultElement);
            return foldout;
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