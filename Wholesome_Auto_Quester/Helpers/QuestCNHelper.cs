using Newtonsoft.Json;
using robotManager.Helpful;
using System;
using System.IO;
using System.Collections.Generic;
//using Wholesome_Auto_Quester.Database.Models;


namespace Wholesome_Auto_Quester.Helpers
{
    public class JSONModelQuestTemplateCN
    {

        public int Id { get; set; }

        public string LogTitle { get; set; }
    }

    public class QuestCNHelper
    {
        public static string GetQuestCNName(int id)
        {

            string jsonFilePath = Others.GetCurrentDirectory + @"Data\questCN.json";

            if (!File.Exists(jsonFilePath))
            {
                Logger.Log(jsonFilePath + "Not exists");
                return "";
            }
            string jsonContent = File.ReadAllText(jsonFilePath);
            if(jsonContent.Equals(""))
            {
                Logger.Log(jsonFilePath + "have no content");
                return "";
            }

            List<JSONModelQuestTemplateCN> _jSONModelQuestTemplateCNEntries = JsonConvert.DeserializeObject<List<JSONModelQuestTemplateCN>>(jsonContent);

            foreach (var tmp in _jSONModelQuestTemplateCNEntries)
            {

                if (tmp.Id == id)
                {
                    return tmp.LogTitle;
                }

            }

            return "";
        }


    }
}
