using Unity.Collections;
using Unity.Mathematics;

namespace Tuntenfisch.Lighting2D.Common
{
    public static class NativeListExtensions
    {
        public static void EnsureMinimumCapacity<T>(this NativeList<T> nativeList, int minCapacity, float factor = 2.0f) where T : unmanaged
        {
            if (nativeList.Capacity < minCapacity)
            {
                nativeList.SetCapacity((int)math.round(factor * minCapacity));
            }
        }
    }
}