using UnityEngine;
using Core.Interfaces;

namespace Player
{
    /// <summary>
    /// Component danh dau (marker) cho collider than chinh cua Player.
    /// Trien khai interface IPrimaryExplosionTarget trong Core.Interfaces de cac assembly khac (Core, Collectibles, Bombs)
    /// co the nhan dien qua interface ma khong bi phu thuoc vong tron vao assembly Player.
    /// </summary>
    public class PrimaryTarget : MonoBehaviour, IPrimaryExplosionTarget
    {
    }
}
