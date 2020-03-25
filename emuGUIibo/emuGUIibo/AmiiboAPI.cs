using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace emuGUIibo
{

    public class Rootobject
    {
        public Amiibo[] amiibo { get; set; }
    }

    public class Amiibo : IComparable<Amiibo>
    {
        public string amiiboSeries { get; set; }
        public string character { get; set; }
        public string gameSeries { get; set; }
        public string head { get; set; }
        public string image { get; set; }
        public string name { get; set; }
        public Release release { get; set; }
        public string tail { get; set; }
        public string type { get; set; }
        public string ID { get; set; }
        public int CompareTo(Amiibo other)
        {
            return this.name.CompareTo(other.name);
        }
    }

    public class Release
    {
        public string au { get; set; }
        public string eu { get; set; }
        public string jp { get; set; }
        public string na { get; set; }
    }

    public class AmiiboSeries : IComparable<AmiiboSeries>
    {
        public string amiiboSeries { get; set; }
        public List<Amiibo> Amiibos { get; set; }
        public int CompareTo(AmiiboSeries other)
        {
            return this.amiiboSeries.CompareTo(other.amiiboSeries);
        }
        public override string ToString()
        {
            return amiiboSeries;
        }
    }

    static class AmiiboAPI
    {
        private const string AMIIBO_API_URL = "https://www.amiiboapi.com/api/amiibo/";

        public static List<AmiiboSeries> AmiiboSeries = new List<AmiiboSeries>();

        public static bool GetAllAmiibos()
        {
            try
            {
                using (WebClient wb = new WebClient())
                {
                    Amiibo[] json = JsonConvert.DeserializeObject<Rootobject>(wb.DownloadString(AMIIBO_API_URL)).amiibo;
                    Console.WriteLine(json);
                    foreach (Amiibo amiibo in json)
                    {
                        amiibo.ID = Convert.ToString(UInt64.Parse(amiibo.head + amiibo.tail, System.Globalization.NumberStyles.HexNumber));
                        if(!AmiiboSeries.Any(series => series.amiiboSeries == amiibo.amiiboSeries))
                        {
                            AmiiboSeries.Add(new AmiiboSeries
                            {
                                amiiboSeries = amiibo.amiiboSeries,
                                Amiibos = new List<Amiibo>()
                            });
                        }
                        AmiiboSeries.Find(series => series.amiiboSeries == amiibo.amiiboSeries).Amiibos.Add(amiibo);
                    }
                    AmiiboSeries.Sort();
                    foreach(AmiiboSeries series in AmiiboSeries)
                    {
                        series.Amiibos.Sort();
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static char MakeRandomHexChar(Random random)
        {
            string hexChars = "0123456789ABCDEF";
            int randomIndex = random.Next(0, hexChars.Length - 1);

            return hexChars[randomIndex];
        }

        public static string MakeRandomHexString(int length)
        {
            string hexString = "";
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                hexString += MakeRandomHexChar(random);
            }

            return hexString;
        }
    }
}