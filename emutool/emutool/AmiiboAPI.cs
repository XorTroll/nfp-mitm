using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace emutool
{
    public class Amiibo : IComparable<Amiibo>
    {
        public string AmiiboName    { get; set; }
        public string SeriesName    { get; set; }
        public string CharacterName { get; set; }
        public string ImageURL      { get; set; }
        public string AmiiboId      { get; set; }

        public int CompareTo(Amiibo other)
        {
            return this.AmiiboName.CompareTo(other.AmiiboName);
        }
    }

    static class AmiiboAPI
    {
        private const string AMIIBO_API_URL = "https://www.amiiboapi.com/api/amiibo/";

        public static List<string> AmiiboSeries = new List<string>();
        public static List<Amiibo> AllAmiibo    = new List<Amiibo>();

        public static bool GetAllAmiibos()
        {
            try
            {
                JObject json = JObject.Parse(new WebClient().DownloadString(AMIIBO_API_URL));

                foreach (JToken amiibo in json["amiibo"])
                {
                    if (AmiiboSeries.Where(serie => serie == amiibo["amiiboSeries"].ToString()).Count() == 0)
                    {
                        AmiiboSeries.Add(amiibo["amiiboSeries"].ToString());
                    }

                    AllAmiibo.Add(new Amiibo { AmiiboName    = amiibo["name"].ToString(),
                                               SeriesName    = amiibo["amiiboSeries"].ToString(),
                                               CharacterName = amiibo["character"].ToString(),
                                               ImageURL      = amiibo["image"].ToString(),
                                               AmiiboId      = amiibo["head"].ToString()
                                                             + amiibo["tail"].ToString()
                    });
                }
                AmiiboSeries.Sort();
                AllAmiibo.Sort();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static char MakeRandomHexChar(Random random)
        {
            string hexChars    = "0123456789ABCDEF";
            int    randomIndex = random.Next(0, hexChars.Length - 1);

            return hexChars[randomIndex];
        }

        public static string MakeRandomHexString(int length)
        {
            string hexString = "";
            Random random    = new Random();

            for (int i = 0; i < length; i++)
            {
                hexString += MakeRandomHexChar(random);
            }

            return hexString;
        }
    }
}