namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien cho Player (PlayerAnimator) de phan ung la Skills su dung prin kich hoat anh nion animator.
    /// Skills (Behaviors) lan nghe giao dien nay qua player.GameObject.GetComponent, astfel Player si khong phu thuoc vao Skills.
    /// Nay kho phep decoupling vao toan won Circular Dependency (Core as center).
    /// </summary>
    public interface ISkillAnimationBridge
    {
        /// <summary>
        /// Kich hoat trigger 'SuperJump' tren animator khi player dung skill SuperJump.
        /// </summary>
        void TriggerSuperJumpAnimation();

        /// <summary>
        /// Kich hoat trigger 'Summon' tren animator khi player dung Platform, BubbleBarrier sau Heal.
        /// </summary>
        void TriggerSummonAnimation();

        /// <summary>
        /// Sat bool 'Charge' tren animator: true trong thoi gian duration cua skill Disarm, false khi ket thuc.
        /// </summary>
        /// <param name="charging">True neu skill Disarm dang din thoi gian charge.</param>
        void SetDisarmCharging(bool charging);

        /// <summary>
        /// Sat bool 'IsForcefieldOn' tren animator: true khi skill Forcefield dang hoat dong, false khi ket thuc.
        /// </summary>
        /// <param name="active">True neu skill Forcefield dang hoat dong.</param>
        void SetForcefieldActive(bool active);
    }
}