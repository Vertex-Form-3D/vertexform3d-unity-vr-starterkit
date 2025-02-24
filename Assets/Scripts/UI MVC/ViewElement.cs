using UnityEngine;

namespace VertextFormCore
{
    public abstract class ViewElement<T> : MonoBehaviour, IBindable<T>, IClickable
    {

        protected OnListItemClikedListener onListItemClikedListener;
        protected int index;

        public void Bind(T t, int index, OnListItemClikedListener onListItemClikedListener)
        {
            this.index = index;
            this.onListItemClikedListener = onListItemClikedListener;

            UpdateView(t);
        }

        public abstract void UpdateView(T t);

        public virtual void OnClicked()
        {
            Debug.Log("ViewElement OnClicked: " + index);
            onListItemClikedListener.OnListClicked(index);
        }
    }
}