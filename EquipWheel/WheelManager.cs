using System.Collections.Generic;
using System.Linq;

namespace EquipWheel
{
    public sealed class WheelManager
    { 

        private static readonly HashSet<IWheel> wheels = new HashSet<IWheel>();
        private static IWheel activeWheel;
        public static bool inventoryVisible = false;
        

        public static bool AnyVisible
        {
            get
            {
                return wheels.Any(w => w.IsVisible());
            }
        }

        public interface IWheel
        {
            int GetKeyCountDown();
            int GetKeyCountPressed();
            bool IsVisible();
            void Hide();
            string GetName();
            float JoyStickIgnoreTime();
        }

        public static void Activate(IWheel wheel)
        {
            if (!wheel.IsVisible())
                return;

            foreach (var w in wheels)
            {
                if (!wheel.Equals(w))
                {
                    w.Hide();
                }
            }

            activeWheel = wheel;
        }
        
        public static bool IsActive(IWheel wheel)
        {
            return wheel.Equals(activeWheel);
        }

        public static bool AddWheel(IWheel wheel)
        {
            return wheels.Add(wheel);
        }

        public static bool RemoveWheel(IWheel wheel)
        {
            return wheels.Remove(wheel);
        }

        public static bool BestMatchDown(IWheel wheel)
        {
            if (!wheels.Contains(wheel))
                return false;

            var result = wheels.OrderByDescending(w => w.GetKeyCountDown()).FirstOrDefault();

            return wheel.Equals(result);
        }

        public static bool BestMatchPressed(IWheel wheel)
        {
            if (!wheels.Contains(wheel))
                return false;

            var result = wheels.OrderByDescending(w => w.GetKeyCountPressed()).FirstOrDefault();

            return wheel.Equals(result);
        }

        public static float GetJoyStickIgnoreTime ()
        {
            float time = 0;

            foreach (var w in wheels)
            {
                time += w.JoyStickIgnoreTime();
            }

            return time;
        }
    }
}
