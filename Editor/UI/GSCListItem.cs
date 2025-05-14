using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UTJ.ShaderVariantStripping
{
    public class GSCListItem : CollectionListItemUI<GSCListItem, ShaderVariantCollection>
    {
        public GSCListItem(Action<GSCListItem> onChange, Action<GSCListItem> onRemove) : base(onChange, onRemove)
        {
        }

        protected override GSCListItem GetThisValue()
        {
            return this;
        }
    }
}