using System.Collections.Generic;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreatureTemplate
    {
        public int entry { get; set; }
        public string name { get; set; }
        public uint faction { get; set; }
        public int minLevel { get; set; }
        public int maxLevel { get; set; }
        public long unit_flags { get; set; }
        public List<ModelCreature> Creatures { get; set; } = new List<ModelCreature>();
        public bool IsHostile => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) <= 2;
        public bool IsNeutral => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) == 3;
        public bool IsFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 4;
        public bool IsNeutralOrFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 3;

        public bool UNIT_FLAG_SERVER_CONTROLLED => (unit_flags & 1) != 0;
        public bool UNIT_FLAG_NON_ATTACKABLE => (unit_flags & 2) != 0;
        public bool UNIT_FLAG_REMOVE_CLIENT_CONTROL => (unit_flags & 4) != 0;
        public bool UNIT_FLAG_PLAYER_CONTROLLED => (unit_flags & 8) != 0;
        public bool UNIT_FLAG_RENAME => (unit_flags & 16) != 0;
        public bool UNIT_FLAG_PREPARATION => (unit_flags & 32) != 0;
        public bool UNIT_FLAG_UNK_6 => (unit_flags & 64) != 0;
        public bool UNIT_FLAG_NOT_ATTACKABLE_1 => (unit_flags & 128) != 0;
        public bool UNIT_FLAG_IMMUNE_TO_PC => (unit_flags & 256) != 0;
        public bool UNIT_FLAG_IMMUNE_TO_NPC => (unit_flags & 512) != 0;
        public bool UNIT_FLAG_LOOTING => (unit_flags & 1024) != 0;
        public bool UNIT_FLAG_PET_IN_COMBAT => (unit_flags & 2048) != 0;
        public bool UNIT_FLAG_PVP => (unit_flags & 4096) != 0;
        public bool UNIT_FLAG_SILENCED => (unit_flags & 8192) != 0;
        public bool UNIT_FLAG_CANNOT_SWIM => (unit_flags & 16384) != 0;
        public bool NIT_FLAG_SWIMMING => (unit_flags & 32768) != 0;
        public bool UNIT_FLAG_NON_ATTACKABLE_2 => (unit_flags & 65536) != 0;
        public bool UNIT_FLAG_PACIFIED => (unit_flags & 131072) != 0;
        public bool UNIT_FLAG_STUNNED => (unit_flags & 262144) != 0;
        public bool UNIT_FLAG_IN_COMBAT => (unit_flags & 524288) != 0;
        public bool UNIT_FLAG_TAXI_FLIGHT => (unit_flags & 1048576) != 0;
        public bool UNIT_FLAG_DISARMED => (unit_flags & 2097152) != 0;
        public bool UNIT_FLAG_CONFUSED => (unit_flags & 4194304) != 0;
        public bool UNIT_FLAG_FLEEING => (unit_flags & 8388608) != 0;
        public bool UNIT_FLAG_POSSESSED => (unit_flags & 16777216) != 0;
        public bool UNIT_FLAG_NOT_SELECTABLE => (unit_flags & 33554432) != 0;
        public bool UNIT_FLAG_SKINNABLE => (unit_flags & 67108864) != 0;
        public bool UNIT_FLAG_MOUNT => (unit_flags & 134217728) != 0;
        public bool UNIT_FLAG_UNK_28 => (unit_flags & 268435456) != 0;
        public bool UNIT_FLAG_UNK_29 => (unit_flags & 536870912) != 0;
        public bool UNIT_FLAG_SHEATHE => (unit_flags & 1073741824) != 0;
        public bool UNIT_FLAG_IMMUNE => (unit_flags & 2147483648) != 0;

        public Reaction GetRelationTypeTowardsMe => WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate);
    }
}
