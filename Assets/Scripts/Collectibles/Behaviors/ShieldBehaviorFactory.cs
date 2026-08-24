using System;
using System.Collections.Generic;
using Collectibles.Data;
using Core.Interfaces;
using UnityEngine;

namespace Collectibles.Behaviors
{
    /// <summary>
    /// Factory để tạo ra các instance IShieldBehavior dựa trên loại BaseShieldData.
    /// </summary>
    public static class ShieldBehaviorFactory
    {
        private static readonly Dictionary<Type, Func<IBaseShieldData, IShieldBehavior>> _behaviorCreators = new()
        {
            { typeof(FireShieldData), (data) => new FireShieldBehavior(data as FireShieldData) },
            { typeof(CrystalShieldData), (data) => new CrystalShieldBehavior(data as CrystalShieldData) },
            { typeof(MagicShieldData), (data) => new MagicShieldBehavior(data as MagicShieldData) },
            // Thêm các loại khiên khác vào đây
        };

        public static IShieldBehavior CreateBehavior(IBaseShieldData data)
        {
            if (data == null) return null;
            if (_behaviorCreators.TryGetValue(data.GetType(), out var creator))
            {
                return creator(data);
            }
            Debug.LogError($"Không tìm thấy ShieldBehavior cho loại dữ liệu: {data.GetType().Name}");
            return null;
        }
    }
}