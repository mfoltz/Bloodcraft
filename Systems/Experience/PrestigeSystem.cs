using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cobalt.Systems.Experience
{
    public class PrestigeSystem
    {
        public class PrestigeStatManager
        {
            public enum PrestigeStatType
            {
                PhysicalResistance,
                SpellResistance,
                MovementSpeed
            }

            public static readonly Dictionary<int, PrestigeStatType> PrestigeStatMap = new()
            {
                { 0, PrestigeStatType.PhysicalResistance },
                { 1, PrestigeStatType.SpellResistance },
                { 2, PrestigeStatType.MovementSpeed }
            };
        }
    }
}
