public interface IState
{
    void FrameUpdate();
    void PhysicsUpdate();
    void OnEnter();
    void OnExit();
}