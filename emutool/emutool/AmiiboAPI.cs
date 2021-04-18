using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace emutool
{
    public static class AmiiboAPI
    {
        public class Amiibo
        {
            public string AmiiboName { get; set; }

            public string SeriesName { get; set; }

            public string CharacterName { get; set; }

            public string ImageURL { get; set; }

            public string AmiiboId { get; set; }
        }

        public class AmiiboList
        {
            public List<Amiibo> Amiibos { get; set; }

            public AmiiboList()
            {
                Amiibos = new List<Amiibo>();
            }

            public int GetAmiiboCount()
            {
                return Amiibos.Count;
            }

            public List<Amiibo> GetAmiibosBySeries(string series)
            {
                var list = new List<Amiibo>();
                if(Amiibos.Any())
                {
                    list.AddRange(Amiibos.Where(amiibo => amiibo.SeriesName == series).OrderBy(amiibo => amiibo.AmiiboName));
                }
                return list;
            }

            public List<string> GetAmiiboSeries()
            {
                var list = new List<string>();
                if(Amiibos.Any())
                {
                    foreach(var amiibo in Amiibos)
                    {
                        var series = amiibo.SeriesName;
                        if(!list.Contains(series))
                        {
                            list.Add(series);
                        }
                    }
                }
                return list;
            }
        };

        private const string AmiiboAPIURL = "https://www.amiiboapi.com/api/amiibo/";

        public static AmiiboList GetAllAmiibos()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var list = new AmiiboList();
            try
            {
                var json = JObject.Parse(new WebClient().DownloadString(AmiiboAPIURL));
                foreach(var entry in json["amiibo"])
                {
                    var amiibo = new Amiibo
                    {
                        AmiiboName = entry["name"].ToString(),
                        SeriesName = entry["amiiboSeries"].ToString(),
                        CharacterName = entry["character"].ToString(),
                        ImageURL = entry["image"].ToString(),
                        AmiiboId = entry["head"].ToString() + entry["tail"].ToString(),
                    };
                    list.Amiibos.Add(amiibo);
                }
            }
            catch(Exception ex)
            {
                ExceptionUtils.LogExceptionMessage(ex);
            }
            return list;
        }
    }
}