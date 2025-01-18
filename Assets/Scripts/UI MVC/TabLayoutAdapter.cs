using System.Collections.Generic;
using UnityEngine;
using VertextFormCore;

namespace VertextFormCore
{
    public class TabLayoutAdapter<T> : ListAdapter<T>, OnListItemClikedListener
    {

        OnListItemClikedListener tempListener;

        public TabLayoutAdapter(SelectableViewElement<T> viewPrefab, RectTransform listRoot, List<T> data, OnListItemClikedListener onListItemClikedListener = null, int defultSelectedIndex = 0) : base(viewPrefab, listRoot, data, onListItemClikedListener)
        {
            this.defultSelectedIndex = defultSelectedIndex;
            tempListener = onListItemClikedListener; //used for higt end (menu that use tab)
            this.onListItemClikedListener = this;
        }

        protected override void OnCreatedSucess()
        {
            if (viewElements.Count == 0) return;

            //unselect all
            for (int i = 0; i < viewElements.Count; i++)
            {
                ((SelectableViewElement<T>)viewElements[i]).UnSelect();
            }

            //select default one (index = 0)
            ((SelectableViewElement<T>)viewElements[defultSelectedIndex]).Select();
        }


        public void OnListClicked(int index)
        {

            for (int i = 0; i < viewElements.Count; i++)
            {
                ((SelectableViewElement<T>)viewElements[i]).UnSelect();
            }
            ((SelectableViewElement<T>)viewElements[index]).Select();

            tempListener.OnListClicked(index);
        }
    }
}