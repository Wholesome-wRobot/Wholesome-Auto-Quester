using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelItemTemplate
    {
        public int Entry { get; set; }
        public int Class { get; set; }
        public string Name { get; set; }
        public List<ModelCreatureTemplate> DroppedBy { get; set; }
        public List<ModelGameObjectTemplate> GatheredOn { get; set; }
    }
}
