namespace Expo.Core
{
    /// <summary>
    /// Represents a manager's internal state (Idle, Active, etc.).
    /// </summary>
    public interface IManagerState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
