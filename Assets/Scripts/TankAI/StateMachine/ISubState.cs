public interface ISubState
{
    public bool PauseParentState { get; set; }
    void FrameUpdate();
    void PhysicsUpdate();
    void OnEnter();
    void OnExit();
}