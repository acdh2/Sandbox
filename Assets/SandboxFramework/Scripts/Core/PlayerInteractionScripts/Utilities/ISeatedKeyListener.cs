/// <summary>
/// Implemented by components that react to keys pressed while the player sits on a connected Seat.
/// </summary>
public interface ISeatedKeyListener
{
    void OnKeyPress(UnityEngine.InputSystem.Key keyCode);
}

