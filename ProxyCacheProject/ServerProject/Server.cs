using Newtonsoft.Json.Linq;
using ServerProject.ContractTypes;
using ServerProject.ProxyReference;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel;
using System.ServiceModel.Web;

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
            AddCorsheader();
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
            var contractsJson = GetContract();
            var contracts = JArray.Parse(contractsJson);
            string nearestCity = "";
            double nearestDistance = double.MaxValue;

            // on parse avec InvariantCulture afin d'éviter les problèmes de format cf doc car pas tous compris mais ca peux casser 
            if (!double.TryParse(lat, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsedLat) ||
                !double.TryParse(lon, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsedLon))
            {
                return "pas de ville la plus proche"; //pas de ville la plus proche
            }

            foreach (var contract in contracts) {
                if (contract == null) continue;
                var contractName = (string)contract["name"];
                (double contractLat,double contractLon) = getCoordinateOfCity(contractName) ;
                if (contractLat == null || contractLon == null) continue;
                double distance = HaversineDistanceMeters(parsedLat, parsedLon, contractLat, contractLon);
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearestCity = contractName;
                }
            }

            return nearestCity;
        }

        public string GetBikeItineray(string startLat, string startLon, string endLat, string endLon)
        {
            AddCorsheader();
            string link = "https://router.project-osrm.org/route/v1/bicycle/" + startLon + "," + startLat + ";" + endLon + "," + endLat + "?steps=true&geometries=geojson&overview=full";
            return client.LookForData(link, "Itinerary");
        }

        public string GetPedestrianItinerary(string startLat, string startLon, string endLat, string endLon)
        {
            AddCorsheader();
            string link = "https://router.project-osrm.org/route/v1/foot/" + startLon + "," + startLat + ";" + endLon + "," + endLat + "?steps=true&geometries=geojson&overview=full";
            return client.LookForData(link, "Itinerary");
        }

        private (double,double) neareastStationInCity(string city,string Lat,string Lon)
        {
            try
            {
                string stationJson = GetStationOfcity(city);
                //dans cette ville on cherche la station la plus proche
                var stations = JArray.Parse(stationJson);

                // Parse user coords using InvariantCulture
                if (!double.TryParse(Lat, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double userLat) ||
                    !double.TryParse(Lon, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double userLon))
                {
                    return (-1, -1);
                }

                double minDistance = double.MaxValue;
                double bestLat = 0.0;
                double bestLon = 0.0;

                foreach (var station in stations)
                {
                    var pos = station["position"];
                    if (pos == null) continue;

                    // changement de nom des clés possibles pour invariant au cas ou 
                    double stationLat = pos["lat"].Value<double>();
                    double stationLon = pos["lng"].Value<double>();

                    double distance = HaversineDistanceMeters(userLat, userLon, stationLat, stationLon);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestLat = stationLat;
                        bestLon = stationLon;
                    }
                }
                return (bestLat, bestLon);
            }
            catch
            {
                return (-1,-1);
            }
        }

        private (double,double) getCoordinateOfCity(string city)
        {
            string link = "https://api-adresse.data.gouv.fr/search/?q=$"+city+"&limit=1";
            CompositeType element = new CompositeType(link, "City");
            string result = client.LookForData(link, "City");
            var parsedResult = JObject.Parse(result);
            (double lat,double lon ) = parsedResult["features"]?[0]?["geometry"]?["coordinates"] != null ?
                (parsedResult["features"][0]["geometry"]["coordinates"][1].Value<double>(),
                 parsedResult["features"][0]["geometry"]["coordinates"][0].Value<double>()) : (-1, -1);
            return (lat,lon);
        }

        public string BestItinerary(string startLat, string startLon, string endLat, string endLon)
        {
            //on ajoute en-tete CORS car sinon marche pas 
            AddCorsheader();
            try
            {
                //on calcule le temps a pied
                string pedestrianItinerary = GetPedestrianItinerary(startLat, startLon, endLat, endLon);
                //on cherche la ville la plus proche du point de départ
                string nearestCityNameStart = neareastCityWithContract2(startLat, startLon);
                //dans cett ville on veux la station la plus proche de nous 
                (double bestStationLat,double bestStationLon) = neareastStationInCity(nearestCityNameStart, startLat, startLon);
                //on recupere mtn la station la plus proche du point d'arrivée
                string nearestCityNameEnd = neareastCityWithContract2(endLat, endLon);
                //dans cett ville on veux la station la plus proche de nous
                (double bestStationEndLat, double bestStationEndLon) = neareastStationInCity(nearestCityNameEnd, endLat, endLon);


                //on passe mtn a la logique on calcule le temps total avec les trois itinéraires
                if(checkStation(bestStationLat,bestStationLon) && checkStation(bestStationEndLat, bestStationEndLon) && pedestrianItinerary!=null){
                    // utiliser InvariantCulture pour convertir en chaîne (séparateur décimal = '.')
                    string itineratyTobikeStartStation = GetPedestrianItinerary(startLat, startLon, bestStationLat.ToString(CultureInfo.InvariantCulture), bestStationLon.ToString(CultureInfo.InvariantCulture));
                    string itineratyBike = GetBikeItineray(bestStationLat.ToString(CultureInfo.InvariantCulture), bestStationLon.ToString(CultureInfo.InvariantCulture), bestStationEndLat.ToString(CultureInfo.InvariantCulture), bestStationEndLon.ToString(CultureInfo.InvariantCulture));
                    string itineraryFromEndStationToEnd = GetPedestrianItinerary(bestStationEndLat.ToString(CultureInfo.InvariantCulture), bestStationEndLon.ToString(CultureInfo.InvariantCulture), endLat, endLon);
                    //on calcule le temps total
                    var pedestrianRoute = JObject.Parse(pedestrianItinerary);
                    var toBikeRoute = JObject.Parse(itineratyTobikeStartStation);
                    var bikeRoute = JObject.Parse(itineratyBike);
                    var fromBikeRoute = JObject.Parse(itineraryFromEndStationToEnd);
                    //temps de la route à pied total
                    double pedestrianDuration = pedestrianRoute["routes"]?[0]?["duration"]?.Value<double>() ?? double.MaxValue;
                    //temps pour aller à la station de vélo
                    double toBikeDuration = toBikeRoute["routes"]?[0]?["duration"]?.Value<double>() ?? double.MaxValue;
                    //temps pour aller de la station de vélo du début à la station de vélo d'arrivée
                    double bikeDuration = bikeRoute["routes"]?[0]?["duration"]?.Value<double>() ?? double.MaxValue;
                    //temps pour aller de la station de vélo d'arrivée au point d'arrivée
                    double fromBikeDuration = fromBikeRoute["routes"]?[0]?["duration"]?.Value<double>() ?? double.MaxValue;
                    //temps total
                    double totalDuration = toBikeDuration + bikeDuration + fromBikeDuration;
                    if(totalDuration < pedestrianDuration)
                    {
                        var result = new JObject
                        {
                            ["methode"] = "withBike",
                            ["numberOfItinerary"] = 3,
                            ["stationCoordinate"] = new JObject
                            {
                                ["startStation"] = new JObject
                                {
                                    ["lat"] = bestStationLat,
                                    ["lon"] = bestStationLon
                                },
                                ["endStation"] = new JObject
                                {
                                    ["lat"] = bestStationEndLat,
                                    ["lon"] = bestStationEndLon
                                }
                            },
                            ["itineraries"] = new JArray
                            {
                                toBikeRoute,
                                bikeRoute,
                                fromBikeRoute
                            }
                        };
                        return result.ToString();
                    }else
                    {
                        var result = new JObject
                        {
                            ["methode"] = "onFoot",
                            ["numberOfItinerary"] = 1,
                            ["itineraries"] = new JArray
                            {
                                pedestrianRoute
                            }
                        };
                        return result.ToString();
                    }
                }else
                {
                    return "there a no walk and bike itinireraries available ";
                }
            }
            catch (Exception ex)
            {
                // Retourne une erreur JSON lisible pour le client (facilite le debug)
                var err = new JObject
                {
                    ["error"] = "internal_exception",
                    ["message"] = ex.Message
                };
                return err.ToString();
            }
        }
      

        private Boolean checkStation(double lat,double lon)
        {
            if (lat == -1 || lon == -1)
            {
                return false;
            }
            return true;
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
            var headers = WebOperationContext.Current.OutgoingResponse.Headers;
            headers.Set("Access-Control-Allow-Origin", "*");
            headers.Set("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");
            headers.Set("Access-Control-Allow-Headers", "Content-Type,Accept");
        }

        private void AddCorsheader()
        {
            var headers = WebOperationContext.Current.OutgoingResponse.Headers;
            headers.Set("Access-Control-Allow-Origin", "*");
            headers.Set("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");
            headers.Set("Access-Control-Allow-Headers", "Content-Type,Accept");
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


    }
}
