using ServerProject.ContractTypes;
using ServerProject.ProxyReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using static System.Net.WebRequestMethods;
using Newtonsoft.Json.Linq;
using System.Runtime.Remoting.Messaging;


namespace ServerProject
{
    // REMARQUE : vous pouvez utiliser la commande Renommer du menu Refactoriser pour changer le nom de classe "Service1" à la fois dans le code et le fichier de configuration.
    public class Server : IServer
    {

        private string apiKey = "952e1140fe8115538347f5c5dfc84cb9acae0909" ;
        private ProxyClient client = new ProxyClient();

        public string GetData(string link, string key )
        {
            CompositeType element = new CompositeType(link ,"Contract");
            return client.LookForData(link,"Contract");
        }

        public string GetStationOfcity(string city)
        {
            string link = "https://api.jcdecaux.com/vls/v3/stations?contract="+city+"&apiKey="+apiKey;
            string type = "Station";
            List<string> names = getCityNamesWithContract();
            if(!names.Contains(city))
            {
                return "City not found in contracts.";
            }
            return client.LookForData(link,type);
        }

        private List<string> getCityNamesWithContract()
        {
            string contractsJson = GetContract();
            try
            {
                var contracts = JArray.Parse(contractsJson);
                var names = contracts
                    .Select(token => (string)token["name"])
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();
                return names;
            }
            catch (Exception ex)
            {
                return new List<string>();
            }
        }

        public string GetContract()
        {
            string link = "https://api.jcdecaux.com/vls/v3/contracts?apiKey="+apiKey;
            string type = "Contract";
            return client.LookForData(link,type);
        }

        private List<string> GetCityNameAndCorrdonate()
        {
            string allContract = GetContract();
            var parseContract = JArray.Parse(allContract);
            var city = parseContract;
            List<string> names = new List<string>();

            return names ;
        }

        public string neareastStation(double lat,double lon) 
        {
            string link = "";


        return "";
        }
    }
}
