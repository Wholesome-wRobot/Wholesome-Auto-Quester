namespace Wholesome_Auto_Quester.Helpers
{
    public enum Factions
    {
        Unknown = 0,
        Human = 1,
        Orc = 2,
        Dwarf = 4,
        NightElf = 8,
        Undead = 16,
        Tauren = 32,
        Gnome = 64,
        Troll = 128,
        Goblin = 256,
        BloodElf = 512,
        Draenei = 1024,
        Worgen = 2097152
    }

    public enum Classes
    {
        Unknown = 0,
        Warrior = 1,
        Paladin = 2,
        Hunter = 4,
        Rogue = 8,
        Priest = 16,
        DeathKnight = 32,
        Shaman = 64,
        Mage = 128,
        Warlock = 256,
        Druid = 1024
    }

    public enum QuestStatus
    {
        ToTurnIn,
        InProgress,
        ToPickup,
        Failed,
        None,
        Completed,
    }

    public enum TaskType
    {
        TurnInQuest,
        PickupQuest,
        Kill,
        KillAndLoot,
        PickupObject
    }
}
