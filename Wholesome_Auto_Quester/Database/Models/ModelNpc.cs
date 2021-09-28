using robotManager.Helpful;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelNpc
    {
        public string Name { get; set; }
        public string ItemName { get; set; } // for loot lookup
        public int Id { get; set; }
        public int Guid { get; set; }
        public int Map { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int SpawnTimeMin { get; set; }
        public int SpawnTimeMax { get; set; }

        // Faction related
        public uint FactionTemplateID { get; set; }
        public int IdFaction { get; set; }
        public int FactionType { get; set; }
        public int OurMask { get; set; }
        public int FriendlyMask { get; set; }
        public int HostileMask { get; set; }
        public int EnemyFaction1 { get; set; }
        public int EnemyFaction2 { get; set; }
        public int EnemyFaction3 { get; set; }
        public int EnemyFaction4 { get; set; }
        public int FriendFaction1 { get; set; }
        public int FriendFaction2 { get; set; }
        public int FriendFaction3 { get; set; }
        public int FriendFaction4 { get; set; }

        public Vector3 GetSpawnPosition => new Vector3(PositionX, PositionY, PositionZ);

        public bool IsHostile => (int)WoWFactionTemplate.FromId(FactionTemplateID).GetReactionTowards(ObjectManager.Me.FactionTemplate) <= 2;
        public bool IsNeutral => (int)WoWFactionTemplate.FromId(FactionTemplateID).GetReactionTowards(ObjectManager.Me.FactionTemplate) == 3;
        public bool IsFriendly => (int)WoWFactionTemplate.FromId(FactionTemplateID).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 4;
        public bool IsNeutralOrFriendly => (int)WoWFactionTemplate.FromId(FactionTemplateID).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 3;

        public Reaction GetRelationTypeTowardsMe => WoWFactionTemplate.FromId(FactionTemplateID).GetReactionTowards(ObjectManager.Me.FactionTemplate);
    }
}

public enum RelationType
{
    Unknown,
    Neutral,
    Friendly,
    Hostile,
}