public interface IBindable <T>
{
   public void Bind(T t,int index,OnListItemClikedListener onItemClikedListener);
}


public interface IClickable {
  public void OnClicked();
}

public interface OnListItemClikedListener
{
    public void OnListClicked(int index);
}

public interface ISelectable {
    public void Select();
    public void UnSelect();
}