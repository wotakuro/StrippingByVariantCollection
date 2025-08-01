using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UTJ.ShaderVariantStripping
{
    public class SVCListItem : CollectionListItemUI<SVCListItem,ShaderVariantCollection>
    {
        public SVCListItem(Action<SVCListItem> onChange, Action<SVCListItem> onRemove) : base(onChange, onRemove)
        {
        }

        protected override SVCListItem GetThisValue()
        {
            return this;
        }
    }
}