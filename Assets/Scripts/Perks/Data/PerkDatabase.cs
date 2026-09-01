using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Interfaces;

namespace Perks.Data
{
    /// <summary>
    /// ScriptableObject database chua toan bo danh sach Perk trong game.
    /// Dung cho UI Shop, Inventory va bat ky he thong nao can truy cap Perk theo ID.
    /// </summary>
    [CreateAssetMenu(fileName = "PerkDatabase", menuName = "Kaboom Chaos/Database/Perk Database")]
    public class PerkDatabase : ScriptableObject, IPerkDatabase
    {
        [Tooltip("Danh sach tat ca cac Perk co trong game.")]
        [SerializeField] private List<BasePerkData> _perks = new();

        /// <summary>
        /// Danh sach read-only toan bo cac Perk.
        /// </summary>
        public IReadOnlyList<IPerkData> AllPerks => _perks.Cast<IPerkData>().ToList().AsReadOnly();

        /// <summary>
        /// Tim Perk theo ID. Tra ve null neu khong tim thay.
        /// </summary>
        public IPerkData GetById(string id)
        {
            return _perks.FirstOrDefault(p => p.Id == id);
        }
    }
}