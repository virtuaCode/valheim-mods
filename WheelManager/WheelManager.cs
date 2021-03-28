using System.Collections.Generic;
using System.Linq;

namespace WheelManager
{
    public sealed class WheelManager
    {
        private static readonly HashSet<IWheel> wheels = new HashSet<IWheel>();

        public interface IWheel
        {
            int GetKeyCount();
        }

        public static bool AddWheel(IWheel wheel)
        {
            return wheels.Add(wheel);
        }

        public static bool RemoveWheel(IWheel wheel)
        {
            return wheels.Remove(wheel);
        }

        public static bool OnTop(IWheel wheel)
        {
            if (!wheels.Contains(wheel))
                return false;

            var result = wheels.OrderByDescending(w => w.GetKeyCount()).FirstOrDefault();

            return wheel.Equals(result);
        }
    }
}
