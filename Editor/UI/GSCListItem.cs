using System;
using System.Collections.Generic;
#if UNITY_6000_5_OR_NEWER
using UnityEngine.Rendering;
#else
using UnityEngine.Experimental.Rendering;
#endif

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

