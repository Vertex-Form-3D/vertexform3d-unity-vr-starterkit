using System.Collections.Generic;
using UnityEngine;
//bind data to view
namespace VertextFormCore
{
    public class ListAdapter<T>
    {
        protected int defultSelectedIndex = 0;
        protected List<T> data;
        protected ViewElement<T> viewPrefab;
        protected RectTransform listRoot;
        protected List<ViewElement<T>> viewElements = new List<ViewElement<T>>();
        protected OnListItemClikedListener onListItemClikedListener;


        public ListAdapter(
            ViewElement<T> viewPrefab,
            RectTransform listRoot,
            List<T> data,
            OnListItemClikedListener onListItemClikedListener = null, int defultSelectedIndex = 0)
        {

            this.viewPrefab = viewPrefab;
            this.data = data;
            this.listRoot = listRoot;
            this.onListItemClikedListener = onListItemClikedListener;
            this.defultSelectedIndex = defultSelectedIndex;
            viewElements = new List<ViewElement<T>>(data.Count);
        }



        public virtual void CreateViews()
        {
            for (int i = 0; i < data.Count; i++)
            {
                ViewElement<T> viewElement = GetElementView(i);
                viewElement.Bind(data[i], i, onListItemClikedListener);
            }
            OnCreatedSucess();
        }

        public virtual void UpdateViews()
        {
            for (int i = 0; i < viewElements.Count; i++)
            {
                ViewElement<T> viewElement = viewElements[i];
                viewElement.UpdateView(data[i]);
            }
        }

        protected virtual void OnCreatedSucess() { }

        protected virtual ViewElement<T> GetElementView(int i)
        {

            ViewElement<T> viewElement;

            if (i < viewElements.Count)
            {
                viewElement = viewElements[i];
            }
            else
            {
                viewElement = GameObject.Instantiate(viewPrefab, listRoot);
                viewElements.Add(viewElement);
            }

            viewElement.gameObject.SetActive(true);
            return viewElement;
        }
    }
}