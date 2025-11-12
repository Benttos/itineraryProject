using ServerProject.ContractTypes;
using ServerProject.ProxyReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Web;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace ServerProject
{
    // REMARQUE : vous pouvez utiliser la commande Renommer du menu Refactoriser pour changer le nom de classe "Service1" à la fois dans le code et le fichier de configuration.
    public class Server : IServer
    {

        private string apiKey = "952e1140fe8115538347f5c5dfc84cb9acae0909";
        private ProxyClient client = new ProxyClient();

        // vitesses de repli (m/s)
        private const double WALKING_SPEED_MPS = 5.0 / 3.6;   // 5 km/h
        private const double BIKING_SPEED_MPS = 15.0 / 3.6;   // 15 km/h

        public string GetData(string link, string key)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            CompositeType element = new CompositeType(link, "Contract");
            return client.LookForData(link, "Contract");
        }

        public string GetStationOfcity(string city)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            string link = "https://api.jcdecaux.com/vls/v3/stations?contract=" + city + "&apiKey=" + apiKey;
            string type = "Station";
            List<string> names = getCityNamesWithContract();
            if (!names.Contains(city))
            {
                return "City not found in contracts.";
            }
            return client.LookForData(link, type);
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
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public string GetContract()
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            string link = "https://api.jcdecaux.com/vls/v3/contracts?apiKey=" + apiKey;
            string type = "Contract";
            return client.LookForData(link, type);
        }

        private List<string> GetCityNameAndCorrdonate()
        {
            string allContract = GetContract();
            var parseContract = JArray.Parse(allContract);
            var city = parseContract;
            List<string> names = new List<string>();

            return names;
        }

        public string neareastCityWithContract(string lat, string lon)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");

            if (!double.TryParse(lat, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var targetLat) ||
                !double.TryParse(lon, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var targetLon))
            {
                throw new WebFaultException<string>("Invalid coordinates format. Use dot as decimal separator (ex: 48.859).", System.Net.HttpStatusCode.BadRequest);
            }

            string contractsJson = GetContract();
            JArray contracts;
            try
            {
                contracts = JArray.Parse(contractsJson);
            }
            catch
            {
                throw new WebFaultException<string>("Failed to parse contracts from JCDecaux API.", System.Net.HttpStatusCode.InternalServerError);
            }

            string bestContract = null;
            double bestDistanceMeters = double.MaxValue;
            JObject bestStation = null;

            foreach (var contractToken in contracts)
            {
                var contractName = (string)contractToken["name"];
                if (string.IsNullOrWhiteSpace(contractName))
                    continue;

                // Récupère toutes les stations pour le contract
                string stationsLink = "https://api.jcdecaux.com/vls/v3/stations?contract=" + contractName + "&apiKey=" + apiKey;
                string stationsJson;
                try
                {
                    stationsJson = client.LookForData(stationsLink, "Station");
                }
                catch
                {
                    continue;
                }

                JArray stations;
                try
                {
                    stations = JArray.Parse(stationsJson);
                }
                catch
                {
                    continue;
                }

                foreach (var station in stations)
                {
                    var pos = station["position"];
                    if (pos == null)
                        continue;

                    double sLat = 0.0, sLon = 0.0;
                    bool parsed = false;

                    var latToken = pos["lat"] ?? pos["latitude"];
                    var lonToken = pos["lng"] ?? pos["lon"] ?? pos["longitude"];
                    if (latToken != null && lonToken != null)
                    {
                        parsed = double.TryParse(latToken.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out sLat)
                                 && double.TryParse(lonToken.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out sLon);
                    }

                    if (!parsed)
                        continue;

                    double dist = HaversineDistanceMeters(targetLat, targetLon, sLat, sLon);
                    if (dist < bestDistanceMeters)
                    {
                        bestDistanceMeters = dist;
                        bestContract = contractName;
                        bestStation = station as JObject;
                    }
                }
            }

            if (bestContract == null)
            {
                return new JObject { ["error"] = "No stations found for any contract." }.ToString();
            }

            var result = new JObject
            {
                ["contract"] = bestContract,
                ["distanceMeters"] = Math.Round(bestDistanceMeters, 1),
                ["station"] = bestStation ?? new JObject()
            };

            return result.ToString();
        }


        public string neareastCityWithContract2(string lat, string lon)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            var contractsJson = GetContract();
            var contracts = JArray.Parse(contractsJson);
            string nearestCity = "";
            double nearestDistance = double.MaxValue;
            foreach (var contract in contracts) {
                if (contract == null) continue;
                var contractName = (string)contract["name"];
                var contractLat = (double?)contract["latitude"];
                var contractLon = (double?)contract["longitude"];
                if (contractLat == null || contractLon == null) continue;
                double distance = HaversineDistanceMeters(double.Parse(lat), double.Parse(lon), contractLat.Value, contractLon.Value);
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearestCity = contractName;
                }
            }

            return nearestCity;
        }

        public string GetBikeItineray(string startLat, string startLon, string endLat, string endLon)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "http://localhost:8080");
            string link = "https://router.project-osrm.org/route/v1/bicycle/" + startLon + "," + startLat + ";" + endLon + "," + endLat + "?steps=true&geometries=geojson&overview=full";
            return client.LookForData(link, "Itinerary");
        }

        public string GetPedestrianItinerary(string startLat, string startLon, string endLat, string endLon)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            string link = "https://router.project-osrm.org/route/v1/foot/" + startLon + "," + startLat + ";" + endLon + "," + endLat + "?steps=true&geometries=geojson&overview=full";
            return client.LookForData(link, "Itinerary");
        }

        private string neareastStationInCity(string city,string Lat,string Lon)
        {
            try
            {
                string stationJson = GetStationOfcity(city);
                //dans cette ville on cherche la station la plus proche
                var stations = JArray.Parse(stationJson);
                double userLat = double.Parse(Lat);
                double userLon = double.Parse(Lon);
                double minDistance = double.MaxValue;
                foreach (var station in stations)
                {
                    var pos = station["position"];
                    if (pos == null) continue;
                    double stationLat = pos["lat"].Value<double>();
                    double stationLon = pos["lng"].Value<double>();
                    double distance = HaversineDistanceMeters(userLat, userLon, stationLat, stationLon);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }
            catch
            {
                return "pas de station dans la ville ";
            }
            return "";
        }

        public string BestItinerary(string startLat, string startLon, string endLat, string endLon)
        {
            //on calcule le temps a pied
            string pedestrianItinerary = GetPedestrianItinerary(startLat, startLon, endLat, endLon);
            //on cherche la ville la plus proche du point de départ
            string nearestCityName = neareastCityWithContract2(startLat, startLon);
            //dans cett ville on veux la station la plus proche de nous 
            

            return "";
        }
       


        // Réponse au préflight OPTIONS (pour getItinerary)
        public void OptionsGetItinerary()
        {
            var resp = WebOperationContext.Current.OutgoingResponse;
            resp.StatusCode = System.Net.HttpStatusCode.OK;
            resp.Headers.Add("Access-Control-Allow-Origin", "*");
            resp.Headers.Add("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");
            resp.Headers.Add("Access-Control-Allow-Headers", "Content-Type,Accept");
        }

        // Réponse au préflight OPTIONS (pour bestItinerary)
        public void OptionsBestItinerary()
        {
            var resp = WebOperationContext.Current.OutgoingResponse;
            resp.StatusCode = System.Net.HttpStatusCode.OK;
            resp.Headers.Add("Access-Control-Allow-Origin", "*");
            resp.Headers.Add("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");
            resp.Headers.Add("Access-Control-Allow-Headers", "Content-Type,Accept");
        }

        // Haversine en mètres
        private static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0; // rayon terre en mètres
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double deg)
        {
            return deg * (Math.PI / 180.0);
        }

        // --- Helpers OSRM / stations ---

        // Extrait lat/lon depuis l'objet station JCDecaux
        private bool TryGetStationPosition(JObject station, out double lat, out double lon)
        {
            lat = lon = double.NaN;
            var pos = station?["position"] as JObject;
            if (pos == null) return false;

            // Correct tokens order : lat first, puis latitude ; lon tries lng/son/longitude
            var latToken = pos["lat"] ?? pos["latitude"];
            var lonToken = pos["lng"] ?? pos["lon"] ?? pos["longitude"];
            if (latToken == null || lonToken == null) return false;

            return double.TryParse(latToken.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out lat)
                   && double.TryParse(lonToken.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out lon);
        }

        // Appel OSRM pour un profile donné — retourne l'objet JSON reçu ou null
        private JObject TryGetOsrmRoute(string profile, double sLat, double sLon, double eLat, double eLon)
        {
            var sLonStr = sLon.ToString(CultureInfo.InvariantCulture);
            var sLatStr = sLat.ToString(CultureInfo.InvariantCulture);
            var eLonStr = eLon.ToString(CultureInfo.InvariantCulture);
            var eLatStr = eLat.ToString(CultureInfo.InvariantCulture);

            string link = $"https://router.project-osrm.org/route/v1/{profile}/{sLonStr},{sLatStr};{eLonStr},{eLatStr}?steps=false&geometries=geojson&overview=full";
            try
            {
                string json = client.LookForData(link, "Itinerary");
                if (string.IsNullOrWhiteSpace(json)) return null;
                var obj = JObject.Parse(json);
                if (obj["routes"] != null && obj["routes"].HasValues) return obj;
            }
            catch
            {
                // ignore and return null -> fallback handled by caller
            }
            return null;
        }

        // Essaie plusieurs profils OSRM (retourne true + route si trouvé)
        private bool TryGetAnyOsrmRoute(string[] profiles, double sLat, double sLon, double eLat, double eLon, out JObject route)
        {
            route = null;
            foreach (var p in profiles)
            {
                try
                {
                    var r = TryGetOsrmRoute(p, sLat, sLon, eLat, eLon);
                    if (r != null)
                    {
                        route = r;
                        return true;
                    }
                }
                catch
                {
                    // try next profile
                }
            }
            return false;
        }

        private double GetDurationFromOsrm(JObject route)
        {
            try
            {
                var token = route.SelectToken("routes[0].duration");
                if (token == null) return double.NaN;
                return token.Value<double>();
            }
            catch
            {
                return double.NaN;
            }
        }

        private double GetDistanceFromOsrm(JObject route)
        {
            try
            {
                var token = route.SelectToken("routes[0].distance");
                if (token == null) return double.NaN;
                return token.Value<double>();
            }
            catch
            {
                return double.NaN;
            }
        }
    }
}
