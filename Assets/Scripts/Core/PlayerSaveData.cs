using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Model du lieu nguoi choi de luu tru bang file JSON.
    /// Chua cac thong tin can luu tru vinh vien: credits, danh sach skill va perk so huu.
    /// Dung chung boi PlayerDataManager (luu/load) va cac he thong khac.
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        /// <summary>
        /// So credits hien tai cua nguoi choi.
        /// </summary>
        public int Credits;

        /// <summary>
        /// Danh sach ID cac skill nguoi choi dang so huu.
        /// </summary>
        public List<string> OwnedSkillIds = new();

        /// <summary>
        /// Danh sach ID cac perk nguoi choi dang so huu.
        /// </summary>
        public List<string> OwnedPerkIds = new();

        /// <summary>
        /// Tong so lan quay thuong (mua skill) da thuc hien tren tat ca cac nhom.
        /// Dung de tinh gia hien tai cua cac nhom skill o Shop.
        /// </summary>
        public int SkillSpinCount;

        /// <summary>
        /// ID cua skill dang duoc trang bi (null/rong neu chua trang bi skill nao).
        /// Duoc luu de khoi phuc lai trang thai trang bi khi vao game.
        /// </summary>
        public string EquippedSkillId;

        /// <summary>
        /// ID cua perk dang duoc trang bi (null/rong neu chua trang bi perk nao).
        /// Duoc luu de khoi phuc lai trang thai trang bi khi vao game.
        /// </summary>
        public string EquippedPerkId;

        /// <summary>
        /// Trang thai Extreme Mode cua nguoi choi (bat/tat).
        /// Duoc luu de khoi phuc logic (max HP, vô hiệu perk) va UI khi vao game.
        /// Khi bat: player chi co max HP = 35, perk bi vô hiệu nhưng van equip/ hien thi duoc.
        /// </summary>
        public bool ExtremeModeEnabled;
    }
}