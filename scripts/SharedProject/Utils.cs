using System;
using System.Linq;

namespace IngameScript {
    public class Utils {

        public static double RadiansToDegrees(double radians) {
            return radians * (180 / Math.PI);
        }

        public static string ToMmasterLcd(string text) {
            return String.Join("\n", text.Split('\n').Select(line => $"Echo {line}"));
        }
    }
}
