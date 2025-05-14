#if UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UTJ.ShaderVariantStripping
{
    public class GSCListItem : CollectionListItemUI<GSCListItem, GraphicsStateCollection>
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
#endif