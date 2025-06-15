
public interface IActivatable
{
    public void Activate();
    public void Deactivate();

    public bool IsActive();

    public bool MatchActivationGroup(UnityEngine.Color group);
}
