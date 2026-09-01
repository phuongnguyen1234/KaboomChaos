using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Interfaces;

namespace Skills.Data
{
    /// <summary>
    /// ScriptableObject database chua toan bo danh sach Skill trong game.
    /// Dung cho UI Shop, Inventory va bat ky he thong nao can truy cap Skill theo ID.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Kaboom Chaos/Database/Skill Database")]
    public class SkillDatabase : ScriptableObject, ISkillDatabase
    {
        [Tooltip("Danh sach tat ca cac Skill co trong game.")]
        [SerializeField] private List<BaseSkillData> _skills = new();

        /// <summary>
        /// Danh sach read-only toan bo cac Skill.
        /// </summary>
        public IReadOnlyList<ISkillData> AllSkills => _skills.Cast<ISkillData>().ToList().AsReadOnly();

        /// <summary>
        /// Tim Skill theo ID. Tra ve null neu khong tim thay.
        /// </summary>
        public ISkillData GetById(string id)
        {
            return _skills.FirstOrDefault(s => s.Id == id);
        }
    }
}
