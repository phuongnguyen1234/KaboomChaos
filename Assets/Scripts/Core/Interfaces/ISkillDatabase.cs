using System.Collections.Generic;

namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien cho database chua danh sach tat ca Skill co trong game.
    /// </summary>
    public interface ISkillDatabase
    {
        /// <summary>
        /// Danh sach read-only toan bo cac Skill co trong game.
        /// </summary>
        IReadOnlyList<ISkillData> AllSkills { get; }

        /// <summary>
        /// Tim kiem Skill theo ID.
        /// </summary>
        /// <param name="id">ID cua Skill can tim.</param>
        /// <returns>ISkillData neu tim thay, null neu khong co.</returns>
        ISkillData GetById(string id);
    }
}
