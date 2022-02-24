using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UTJ.ShaderVariantStripping
{
    public class VariantCollectionUI : VisualElement
    {
        private ObjectField objField;
        private Button removeBtn;
        private Action<VariantCollectionUI> onRemoveData;
        private Action<VariantCollectionUI> onChangeData;

        public ShaderVariantCollection variantCollection
        {
            get
            {
                return objField.value as ShaderVariantCollection;
            }
            set
            {
                objField.value = value;
            }
        }
        public int ListIndex { set; get; }


        public VariantCollectionUI( Action<VariantCollectionUI> onChange, Action<VariantCollectionUI> onRemove) :base()
        {
            this.style.flexDirection = FlexDirection.Row;

            this.objField = new ObjectField();
            this.objField.objectType = typeof(ShaderVariantCollection);
            this.objField.RegisterValueChangedCallback(OnValueChange);

            this.removeBtn = new Button();
            this.removeBtn.style.width = 20;

            this.removeBtn.text = "X";
            this.Add(objField);
            this.Add(removeBtn);

            this.onRemoveData = onRemove;
            this.onChangeData = onChange;
            this.removeBtn.clicked += OnClickRemove;
        }

        private void OnValueChange(ChangeEvent<UnityEngine.Object> obj)
        {

            if (this.onChangeData != null)
            {
                this.onChangeData(this);
            }
        }

        private void OnClickRemove()
        {
            if( this.onRemoveData != null) {
                this.onRemoveData(this); 
            }
        }
    }
}