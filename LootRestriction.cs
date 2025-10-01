using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using Harmony;

namespace Erenshor_LootRestriction
{
    [BepInPlugin(ModGUID, ModDescription, ModVersion)]
    public class LootRestriction : BaseUnityPlugin
    {
        internal const string ModName = "LootRestriction";
        internal const string ModVersion = "1.0.0";
        internal const string ModDescription = "Loot Restriction";
        internal const string Author = "Brad522";
        private const string ModGUID = Author + "." + ModName;
    }
}
