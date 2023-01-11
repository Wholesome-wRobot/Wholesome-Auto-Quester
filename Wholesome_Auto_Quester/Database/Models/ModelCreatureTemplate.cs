using System;
using System.Collections.Generic;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreatureTemplate
    {
        public int entry { get; }
        public string name { get; }
        public uint faction { get; }
        public int KillCredit1 { get; }
        public int KillCredit2 { get; }
        //public int minLevel { get; }
        public int maxLevel { get; }
        public long unit_flags { get; }
        public long unit_flags2 { get; }
        public long type_flags { get; }
        public long dynamicflags { get; }
        public long flags_extra { get; }
        public int rank { get; }

        public List<ModelCreatureTemplate> KillCredits = new List<ModelCreatureTemplate>();
        //public bool HasKillCredit => KillCredit1 > 0 || KillCredit2 > 0;
        public List<ModelCreature> Creatures { get; set; } = new List<ModelCreature>();
        //public bool IsHostile => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) <= 2;
        //public bool IsNeutral => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) == 3;
        public bool IsFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 4;
        public bool IsNeutralOrFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 3;
        //public Reaction GetRelationTypeTowardsMe => WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate);

        public bool IsAttackable => !IsFriendly
            && !UnitFlags.Contains("UNIT_FLAG_NOT_ATTACKABLE_1")
            && !UnitFlags.Contains("UNIT_FLAG_NON_ATTACKABLE");

        private List<string> _unitFlags;
        public List<string> UnitFlags
        {
            get
            {
                if (_unitFlags == null) _unitFlags = GetMatchingUnitFlags(unit_flags);
                return _unitFlags;
            }
        }

        private List<string> _unitFlags2;
        public List<string> UnitFlags2
        {
            get
            {
                if (_unitFlags2 == null) _unitFlags2 = GetMatchingUnitFlags2(unit_flags2);
                return _unitFlags2;
            }
        }

        private List<string> _unitDynamicFLags;
        public List<string> UnitDynamicFlags
        {
            get
            {
                if (_unitDynamicFLags == null) _unitDynamicFLags = GetMatchingUnitDynamicFlags(dynamicflags);
                return _unitDynamicFLags;
            }
        }

        private List<string> _unitTypeFLags;
        public List<string> UnitTypeFlags
        {
            get
            {
                if (_unitTypeFLags == null) _unitTypeFLags = GetMatchingUnitTypeFlags(type_flags);
                return _unitTypeFLags;
            }
        }

        private List<string> _unitFLagsExtra;
        public List<string> UnitFlagsExtra
        {
            get
            {
                if (_unitFLagsExtra == null) _unitFLagsExtra = GetMatchingUnitFlagsExtra(flags_extra);
                return _unitFLagsExtra;
            }
        }

        public List<string> GetMatchingUnitFlags(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(UNIT_FLAGS)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(UNIT_FLAGS), i));
            }
            return result;
        }

        public List<string> GetMatchingUnitFlags2(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(UNIT_FLAGS2)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(UNIT_FLAGS2), i));
            }
            return result;
        }

        public List<string> GetMatchingUnitDynamicFlags(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(UNIT_DYNAMIC_FLAGS)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(UNIT_DYNAMIC_FLAGS), i));
            }
            return result;
        }

        public List<string> GetMatchingUnitTypeFlags(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(UNIT_TYPE_FLAGS)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(UNIT_TYPE_FLAGS), i));
            }
            return result;
        }

        public List<string> GetMatchingUnitFlagsExtra(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(UNIT_FLAGS_EXTRA)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(UNIT_FLAGS_EXTRA), i));
            }
            return result;
        }

        public bool IsValidForKill => !IsFriendly && (rank <= 0 || rank == 4);
    }
}
public enum UNIT_TYPE_FLAGS : long
{
    CREATURE_TYPE_FLAG_TAMEABLE = 1,
    CREATURE_TYPE_FLAG_VISIBLE_TO_GHOSTS = 2,
    CREATURE_TYPE_FLAG_BOSS_MOB = 4,
    CREATURE_TYPE_FLAG_DO_NOT_PLAY_WOUND_ANIM = 8,
    CREATURE_TYPE_FLAG_NO_FACTION_TOOLTIP = 16,
    CREATURE_TYPE_FLAG_MORE_AUDIBLE = 32,
    CREATURE_TYPE_FLAG_SPELL_ATTACKABLE = 64,
    CREATURE_TYPE_FLAG_INTERACT_WHILE_DEAD = 128,
    CREATURE_TYPE_FLAG_SKIN_WITH_HERBALISM = 256,
    CREATURE_TYPE_FLAG_SKIN_WITH_MINING = 512,
    CREATURE_TYPE_FLAG_NO_DEATH_MESSAGE = 1024,
    CREATURE_TYPE_FLAG_ALLOW_MOUNTED_COMBAT = 2048,
    CREATURE_TYPE_FLAG_CAN_ASSIST = 4096,
    CREATURE_TYPE_FLAG_NO_PET_BAR = 8192,
    CREATURE_TYPE_FLAG_MASK_UID = 16384,
    CREATURE_TYPE_FLAG_SKIN_WITH_ENGINEERING = 32768,
    CREATURE_TYPE_FLAG_EXOTIC_PET = 65536,
    CREATURE_TYPE_FLAG_USE_MODEL_COLLISION_SIZE = 131072,
    CREATURE_TYPE_FLAG_ALLOW_INTERACTION_WHILE_IN_COMBAT = 262144,
    CREATURE_TYPE_FLAG_COLLIDE_WITH_MISSILES = 524288,
    CREATURE_TYPE_FLAG_NO_NAME_PLATE = 1048576,
    CREATURE_TYPE_FLAG_DO_NOT_PLAY_MOUNTED_ANIMATIONS = 2097152,
    CREATURE_TYPE_FLAG_LINK_ALL = 4194304,
    CREATURE_TYPE_FLAG_INTERACT_ONLY_WITH_CREATOR = 8388608,
    CREATURE_TYPE_FLAG_DO_NOT_PLAY_UNIT_EVENT_SOUNDS = 16777216,
    CREATURE_TYPE_FLAG_HAS_NO_SHADOW_BLOB = 33554432,
    CREATURE_TYPE_FLAG_TREAT_AS_RAID_UNIT = 67108864,
    CREATURE_TYPE_FLAG_FORCE_GOSSIP = 134217728,
    CREATURE_TYPE_FLAG_DO_NOT_SHEATHE = 268435456,
    CREATURE_TYPE_FLAG_DO_NOT_TARGET_ON_INTERACTION = 536870912,
    CREATURE_TYPE_FLAG_DO_NOT_RENDER_OBJECT_NAME = 1073741824,
    CREATURE_TYPE_FLAG_QUEST_BOSS = 2147483648,
}

public enum UNIT_FLAGS_EXTRA : long
{
    CREATURE_FLAG_EXTRA_INSTANCE_BIND = 1,
    CREATURE_FLAG_EXTRA_CIVILIAN = 2,
    CREATURE_FLAG_EXTRA_NO_PARRY = 4,
    CREATURE_FLAG_EXTRA_NO_PARRY_HASTEN = 8,
    CREATURE_FLAG_EXTRA_NO_BLOCK = 16,
    CREATURE_FLAG_EXTRA_NO_CRUSHING_BLOWS = 32,
    CREATURE_FLAG_EXTRA_NO_XP = 64,
    CREATURE_FLAG_EXTRA_TRIGGER = 128,
    CREATURE_FLAG_EXTRA_NO_TAUNT = 256,
    CREATURE_FLAG_EXTRA_NO_MOVE_FLAGS_UPDATE = 512,
    CREATURE_FLAG_EXTRA_GHOST_VISIBILITY = 1024,
    CREATURE_FLAG_EXTRA_USE_OFFHAND_ATTACK = 2048,
    CREATURE_FLAG_EXTRA_NO_SELL_VENDOR = 4096,
    CREATURE_FLAG_EXTRA_IGNORE_COMBAT = 8192,
    CREATURE_FLAG_EXTRA_WORLDEVENT = 16384,
    CREATURE_FLAG_EXTRA_GUARD = 32768,
    CREATURE_FLAG_EXTRA_IGNORE_FEIGN_DEATH = 65536,
    CREATURE_FLAG_EXTRA_NO_CRIT = 131072,
    CREATURE_FLAG_EXTRA_NO_SKILL_GAINS = 262144,
    CREATURE_FLAG_EXTRA_OBEYS_TAUNT_DIMINISHING_RETURNS = 524288,
    CREATURE_FLAG_EXTRA_ALL_DIMINISH = 1048576,
    CREATURE_FLAG_EXTRA_NO_PLAYER_DAMAGE_REQ = 2097152,
    CREATURE_FLAG_EXTRA_DUNGEON_BOSS = 268435456,
    CREATURE_FLAG_EXTRA_IGNORE_PATHFINDING = 536870912,
    CREATURE_FLAG_EXTRA_IMMUNITY_KNOCKBACK = 1073741824,
}

public enum UNIT_DYNAMIC_FLAGS : long
{
    UNIT_DYNFLAG_NONE = 0,
    UNIT_DYNFLAG_LOOTABLE = 1,
    UNIT_DYNFLAG_TRACK_UNIT = 2,
    UNIT_DYNFLAG_TAPPED = 4,
    UNIT_DYNFLAG_TAPPED_BY_PLAYER = 8,
    UNIT_DYNFLAG_SPECIALINFO = 16,
    UNIT_DYNFLAG_DEAD = 32,
    UNIT_DYNFLAG_REFER_A_FRIEND = 64,
    UNIT_DYNFLAG_TAPPED_BY_ALL_THREAT_LIST = 128,
}

public enum UNIT_FLAGS2 : long
{
    UNIT_FLAG2_FEIGN_DEATH = 1,
    UNIT_FLAG2_HIDE_BODY = 2,
    UNIT_FLAG2_IGNORE_REPUTATION = 4,
    UNIT_FLAG2_COMPREHEND_LANG = 8,
    UNIT_FLAG2_MIRROR_IMAGE = 16,
    UNIT_FLAG2_DO_NOT_FADE_IN = 32,
    UNIT_FLAG2_FORCE_MOVEMENT = 64,
    UNIT_FLAG2_DISARM_OFFHAND = 128,
    UNIT_FLAG2_DISABLE_PRED_STATS = 256,
    UNIT_FLAG2_DISARM_RANGED = 1024,
    UNIT_FLAG2_REGENERATE_POWER = 2048,
    UNIT_FLAG2_RESTRICT_PARTY_INTERACTION = 4096,
    UNIT_FLAG2_PREVENT_SPELL_CLICK = 8192,
    UNIT_FLAG2_ALLOW_ENEMY_INTERACT = 16384,
    UNIT_FLAG2_CANNOT_TURN = 32768,
    UNIT_FLAG2_UNK2 = 65536,
    UNIT_FLAG2_PLAY_DEATH_ANIM = 131072,
    UNIT_FLAG2_ALLOW_CHEAT_SPELLS = 262144,
}

public enum UNIT_FLAGS : long
{
    UNIT_FLAG_SERVER_CONTROLLED = 1,
    UNIT_FLAG_NON_ATTACKABLE = 2,
    UNIT_FLAG_REMOVE_CLIENT_CONTROL = 4,
    UNIT_FLAG_PLAYER_CONTROLLED = 8,
    UNIT_FLAG_RENAME = 16,
    UNIT_FLAG_PREPARATION = 32,
    UNIT_FLAG_UNK_6 = 64,
    UNIT_FLAG_NOT_ATTACKABLE_1 = 128,
    UNIT_FLAG_IMMUNE_TO_PC = 256,
    UNIT_FLAG_IMMUNE_TO_NPC = 512,
    UNIT_FLAG_LOOTING = 1024,
    UNIT_FLAG_PET_IN_COMBAT = 2048,
    UNIT_FLAG_PVP = 4096,
    UNIT_FLAG_SILENCED = 8192,
    UNIT_FLAG_CANNOT_SWIM = 16384,
    UNIT_FLAG_SWIMMING = 32768,
    UNIT_FLAG_NON_ATTACKABLE_2 = 65536,
    UNIT_FLAG_PACIFIED = 131072,
    UNIT_FLAG_STUNNED = 262144,
    UNIT_FLAG_IN_COMBAT = 524288,
    UNIT_FLAG_TAXI_FLIGHT = 1048576,
    UNIT_FLAG_DISARMED = 2097152,
    UNIT_FLAG_CONFUSED = 4194304,
    UNIT_FLAG_FLEEING = 8388608,
    UNIT_FLAG_POSSESSED = 16777216,
    UNIT_FLAG_NOT_SELECTABLE = 33554432,
    UNIT_FLAG_SKINNABLE = 67108864,
    UNIT_FLAG_MOUNT = 134217728,
    UNIT_FLAG_UNK_28 = 268435456,
    UNIT_FLAG_UNK_29 = 536870912,
    UNIT_FLAG_SHEATHE = 1073741824,
    UNIT_FLAG_IMMUNE = 2147483648
}