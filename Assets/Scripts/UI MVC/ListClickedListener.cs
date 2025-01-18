using System;

namespace VertextFormCore
{
    public class ListClickedListener : OnListItemClikedListener
    {
        Action<int> onclicked;

        public ListClickedListener(Action<int> onclicked)
        {
            this.onclicked = onclicked;
        }

        public void OnListClicked(int index)
        {
            onclicked?.Invoke(index);
        }
    }
}