using System.Collections.Generic;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreatureLootTemplate
    {
        public ModelCreatureLootTemplate(
            JSONModelCreatureLootTemplate jmclt,
            Dictionary<int, JSONModelCreatureTemplate> creatureTemplatesDic)
        {
            Entry = jmclt.Entry;
            Chance = jmclt.Chance;

            if (creatureTemplatesDic.TryGetValue(jmclt.Entry, out JSONModelCreatureTemplate creatureTemplate))
                CreatureTemplate = new ModelCreatureTemplate(creatureTemplate, creatureTemplatesDic);
            else
                Logger.LogDevDebug($"WARNING: CreatureTemplate with entry {jmclt.Entry} couldn't be found in dictionary");
        }

        public int Entry { get; }
        //public int Item { get; }
        //public int Reference { get; }
        public float Chance { get; set; }
        //public int QuestRequired { get; }
        //public int LootMode { get; }
        //public int GroupId { get; }
        //public int MinCount { get; }
        //public int MaxCount { get; }
        //public string Comment { get; }

        public ModelCreatureTemplate CreatureTemplate { get; set; }
    }
}
