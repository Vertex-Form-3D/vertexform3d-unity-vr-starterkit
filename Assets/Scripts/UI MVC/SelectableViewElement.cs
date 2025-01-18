using VertextFormCore;

public abstract class SelectableViewElement<T> : ViewElement<T>, ISelectable
{
    public abstract void Select();
    public abstract void UnSelect();
}
