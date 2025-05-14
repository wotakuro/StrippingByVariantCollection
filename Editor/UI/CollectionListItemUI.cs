using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UTJ.ShaderVariantStripping
{
    public abstract class CollectionListItemUI<T1,T2> : VisualElement 
        where T1 : CollectionListItemUI<T1,T2>
        where T2:UnityEngine.Object
    {
        private ObjectField objField;
        private Button removeBtn;
        private Action<T1> onRemoveData;
        private Action<T1> onChangeData;

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


        public CollectionListItemUI( Action<T1> onChange, Action<T1> onRemove) :base()
        {
            this.style.flexDirection = FlexDirection.Row;

            this.objField = new ObjectField();
            this.objField.objectType = typeof(T2);
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

        protected abstract T1 GetThisValue(); 

        private void OnValueChange(ChangeEvent<UnityEngine.Object> obj)
        {

            if (this.onChangeData != null)
            {
                this.onChangeData(GetThisValue());
            }
        }

        private void OnClickRemove()
        {
            if( this.onRemoveData != null) {
                this.onRemoveData(GetThisValue()); 
            }
        }
    }
}