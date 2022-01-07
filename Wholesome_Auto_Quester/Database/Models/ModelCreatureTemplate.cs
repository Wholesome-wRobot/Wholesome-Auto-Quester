using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models.Flags;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreatureTemplate
    {

        private DBUnitFlags _unitFlags;
        public DBUnitFlags DBUnitFlags
        {
            get
            {
                if (_unitFlags == null) _unitFlags = new DBUnitFlags(unit_flags);
                return _unitFlags;
            }
        }

        private DBUnitFlags2 _unitFlags2;
        public DBUnitFlags2 DBUnitFlags2
        {
            get
            {
                if (_unitFlags2 == null) _unitFlags2 = new DBUnitFlags2(unit_flags2);
                return _unitFlags2;
            }
        }

        private DBUnitDynamicFlags _unitDynamicFlags;
        public DBUnitDynamicFlags DBUnitDynamicFlags
        {
            get
            {
                if (_unitDynamicFlags == null) _unitDynamicFlags = new DBUnitDynamicFlags(dynamicflags);
                return _unitDynamicFlags;
            }
        }

        private DBUnitTypeFlags _unitTypeFlags;
        public DBUnitTypeFlags DBUnitTypeFlags
        {
            get
            {
                if (_unitTypeFlags == null) _unitTypeFlags = new DBUnitTypeFlags(type_flags);
                return _unitTypeFlags;
            }
        }

        private DBUnitFlagsExtra _unitFlagsExtra;
        public DBUnitFlagsExtra DBUnitFlagsExtra
        {
            get
            {
                if (_unitFlagsExtra == null) _unitFlagsExtra = new DBUnitFlagsExtra(flags_extra);
                return _unitFlagsExtra;
            }
        }

        public int entry { get; set; }
        public string name { get; set; }
        public uint faction { get; set; }
        public int minLevel { get; set; }
        public int maxLevel { get; set; }
        public long unit_flags { get; set; }
        public long unit_flags2 { get; set; }
        public long type_flags { get; set; }
        public long dynamicflags { get; set; }
        public long flags_extra { get; set; }
        public List<ModelCreature> Creatures { get; set; } = new List<ModelCreature>();
        public bool IsHostile => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) <= 2;
        public bool IsNeutral => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) == 3;
        public bool IsFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 4;
        public bool IsNeutralOrFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 3;

        public Reaction GetRelationTypeTowardsMe => WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate);
    }
}
