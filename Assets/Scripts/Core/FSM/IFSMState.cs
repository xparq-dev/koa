namespace KOA.Core.FSM
{
    /// <summary>
    /// Interface สถานะสำหรับ Finite State Machine (Section 4 ใน Roadmap และ Section 9 ใน Requirement)
    /// </summary>
    public interface IFSMState<TContext>
    {
        void OnEnter(TContext context);
        void OnUpdate(TContext context, float deltaTime);
        void OnExit(TContext context);
    }
}
