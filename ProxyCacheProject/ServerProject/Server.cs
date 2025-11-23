using Newtonsoft.Json.Linq;
using ServerProject.ContractTypes;
using ServerProject.ProxyReference;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;

namespace ServerProject
{
    // REMARQUE : vous pouvez utiliser la commande Renommer du menu Refactoriser pour changer le nom de classe "Service1" à la fois dans le code et le fichier de configuration.
    public class Server : IServer
    {

        private string apiKey = "952e1140fe8115538347f5c5dfc84cb9acae0909";
        private string osrApiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6ImY3NTFlNTFhN2NhYjQyZjZiYTc5ZmIxNjRhYzI2Mzg2IiwiaCI6Im11cm11cjY0In0=";
        private ProxyClient client = new ProxyClient();

        // vitesses de repli (m/s)
        private const double WALKING_SPEED_MPS = 5.0 / 3.6;   // 5 km/h
        private const double BIKING_SPEED_MPS = 15.0 / 3.6;   // 15 km/h
        private const string jsonPath = @"C:\Users\karat\OneDrive\Bureau\Ecole_inge\middleware\itineraryProject\ProxyCacheProject\JsonUser.txt";

        private string GetData(string link, string key)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            CompositeType element = new CompositeType(link, "Contract");
            return client.LookForData(link, "Contract");
        }

        private string GetStationOfcity(string city)
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

        private string GetContract()
        {
            AddCorsheader();
            string link = "https://api.jcdecaux.com/vls/v3/contracts?apiKey=" + apiKey;
            string type = "Contract";
            return client.LookForData(link, type);
        }



        public string neareastCityWithContract2(string lat, string lon)
        {
            AddCorsheader();
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

            foreach (var contract in contracts)
            {
                if (contract == null) continue;
                var contractName = (string)contract["name"];
                (double contractLat, double contractLon) = getCoordinateOfCity(contractName);
                if (contractLat == -1 || contractLon == -1) continue;
                double distance = HaversineDistanceMeters(parsedLat, parsedLon, contractLat, contractLon);
                //to do on veut ameliorer de maniere a ce que on prenne une station sur la route et pas forcément la ville la plus proche
                if (distance < nearestDistance && validateCity(contractName))
                {
                    nearestDistance = distance;
                    nearestCity = contractName;
                }
            }
            return nearestCity;
        }

        private bool validateCity(string cityName)
        {
            try
            {
                string stationJson = GetStationOfcity(cityName);
                if (string.IsNullOrWhiteSpace(stationJson)) return false;

                var token = JToken.Parse(stationJson);

                // Si la réponse est un tableau (liste de stations)
                if (token.Type == JTokenType.Array)
                {
                    return ((JArray)token).HasValues;
                }

                // Si la réponse est un objet, chercher des champs courants contenant des stations
                if (token.Type == JTokenType.Object)
                {
                    var obj = (JObject)token;
                    if (obj["stations"] is JArray stationsArr && stationsArr.HasValues) return true;
                    if (obj["features"] is JArray featuresArr && featuresArr.HasValues) return true;
                    // cas d'un seul objet station avec "position"
                    if (obj["position"] != null) return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public string GetBikeItineray(string startLat, string startLon, string endLat, string endLon)
        {
            // Appel OpenRouteService (OSR) — renvoie la réponse OSR brute (JSON)
            AddCorsheader();
            string profile = "cycling-regular";
            string link = "https://api.openrouteservice.org/v2/directions/" + profile
                          + "?api_key=" + Uri.EscapeDataString(osrApiKey)
                          + "&start=" + startLon + "," + startLat
                          + "&end=" + endLon + "," + endLat
                          + "&geometry_format=geojson&instructions=true";

            return client.LookForData(link, "Itinerary");
        }

        public string GetPedestrianItinerary(string startLat, string startLon, string endLat, string endLon)
        {
            // Appel OpenRouteService (OSR) — renvoie la réponse OSR brute (JSON)
            AddCorsheader();
            string profile = "foot-walking";
            string link = "https://api.openrouteservice.org/v2/directions/" + profile
                          + "?api_key=" + Uri.EscapeDataString(osrApiKey)
                          + "&start=" + startLon + "," + startLat
                          + "&end=" + endLon + "," + endLat
                          + "&geometry_format=geojson&instructions=true";

            return client.LookForData(link, "Itinerary");
        }

        public string neareastStationInCity(string city,string Lat,string Lon)
        {
            try
            {
                string stationJson = GetStationOfcity(city);
                //dans cette ville on cherche la station la plus proche
                var stations = JArray.Parse(stationJson);

                // parse les coo
                if (!double.TryParse(Lat, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double userLat) ||
                    !double.TryParse(Lon, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double userLon))
                {
                    return "impossible de convertir et d'obtenier les element souhaiter ";
                }

                double minDistance = double.MaxValue;
                double bestLat = 0.0;
                double bestLon = 0.0;
                string BestStationName = "";
                foreach (var station in stations)
                {
                    var pos = station["position"];
                    if (pos == null) continue;

                    double stationLat = pos["latitude"].Value<double>();
                    double stationLon = pos["longitude"].Value<double>();
                    string stationName = station["name"].Value<string>();

                    double distance = HaversineDistanceMeters(userLat, userLon, stationLat, stationLon);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestLat = stationLat;
                        bestLon = stationLon;
                        BestStationName = stationName;
                    }
                }

                var result = new JObject
                {
                    ["name"] = BestStationName,
                    ["position"] = new JObject
                    {
                        ["lat"] = bestLat,
                        ["lon"] = bestLon
                    },
                    ["distance"] = minDistance
                };
                return result.ToString();
            }
            catch
            {
                return "failure dans neareste station ";
            }
        }

        private (double,double) getCoordinateOfCity(string city)
        {
            string link = "https://api-adresse.data.gouv.fr/search/?q="+city+"&limit=1";
            CompositeType element = new CompositeType(link, "City");
            string result = client.LookForData(link, "City");
            try
            {
                var parsedResult = result != null ? JObject.Parse(result) : null;
                (double lat, double lon) = parsedResult["features"]?[0]?["geometry"]?["coordinates"] != null ?
                    (parsedResult["features"][0]["geometry"]["coordinates"][1].Value<double>(),
                     parsedResult["features"][0]["geometry"]["coordinates"][0].Value<double>()) : (-1, -1);
                return (lat, lon);

            }
            catch 
            {
                return (-1, -1);
            }
        }

        // Helper : extrait la durée en secondes depuis JSON OSR (features[0].properties.summary.duration)
        // Retourne null si non trouvée. Supporte aussi le format OSRM ("routes[0].duration") si présent.
        private double? ExtractDurationFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var obj = JObject.Parse(json);

                // OpenRouteService
                var durOrs = obj["features"]?[0]?["properties"]?["summary"]?["duration"];
                if (durOrs != null && durOrs.Type == JTokenType.Float || durOrs != null && durOrs.Type == JTokenType.Integer)
                {
                    return durOrs.Value<double>();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public string BestItinerary(string startLat, string startLon, string endLat, string endLon)
        {
            AddCorsheader();
            try
            {
                //on recup le trajet total a pied
                string pedestrianItinerary = GetPedestrianItinerary(startLat, startLon, endLat, endLon);
                //on recupe la ville avec station la plus proche 
                string nearestCityNameStart = neareastCityWithContract2(startLat, startLon);
                //on recup station dans ville  
                string stationInfo = neareastStationInCity(nearestCityNameStart, startLat, startLon);
                var stationToken = JObject.Parse(stationInfo);
                double bestStationLat = stationToken["position"]?["lat"]?.Value<double>() ?? -1;
                double bestStationLon = stationToken["position"]?["lon"]?.Value<double>() ?? -1;

                //on recupere mtn la station la plus proche du point d'arrivée
                string nearestCityNameEnd = neareastCityWithContract2(endLat, endLon);
                //dans cett ville on veux la station la plus proche de nous
                string endStationInfo = neareastStationInCity(nearestCityNameEnd, endLat, endLon);
                var endStationInfoToken = JObject.Parse(endStationInfo);
                double bestStationEndLat = endStationInfoToken["position"]?["lat"]?.Value<double>() ?? -1;
                double bestStationEndLon = endStationInfoToken["position"]?["lon"]?.Value<double>() ?? -1;

                if(checkStation(bestStationLat,bestStationLon) && checkStation(bestStationEndLat, bestStationEndLon) && pedestrianItinerary!=null){
                    string itineratyTobikeStartStation = GetPedestrianItinerary(startLat, startLon, bestStationLat.ToString(CultureInfo.InvariantCulture), bestStationLon.ToString(CultureInfo.InvariantCulture));
                    string itineratyBike = GetBikeItineray(bestStationLat.ToString(CultureInfo.InvariantCulture), bestStationLon.ToString(CultureInfo.InvariantCulture), bestStationEndLat.ToString(CultureInfo.InvariantCulture), bestStationEndLon.ToString(CultureInfo.InvariantCulture));
                    string itineraryFromEndStationToEnd = GetPedestrianItinerary(bestStationEndLat.ToString(CultureInfo.InvariantCulture), bestStationEndLon.ToString(CultureInfo.InvariantCulture), endLat, endLon);

                    // extraire durées (OSR ou OSRM supporté)
                    double pedestrianDuration = ExtractDurationFromJson(pedestrianItinerary) ?? double.MaxValue;
                    double toBikeDuration = ExtractDurationFromJson(itineratyTobikeStartStation) ?? double.MaxValue;
                    double bikeDuration = ExtractDurationFromJson(itineratyBike) ?? double.MaxValue;
                    double fromBikeDuration = ExtractDurationFromJson(itineraryFromEndStationToEnd) ?? double.MaxValue;

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
                            // renvoyer les JSONs OSR (parsés) dans le tableau — logique inchangée côté décision
                            ["itineraries"] = new JArray
                            {
                                JToken.Parse(itineratyTobikeStartStation),
                                JToken.Parse(itineratyBike),
                                JToken.Parse(itineraryFromEndStationToEnd)
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
                                JToken.Parse(pedestrianItinerary)
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
            if (lat == -1 || lon == -1 || (lat == 0 && lon==0))
            {
                return false;
            }
            return true;
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

        public string GetUserInfo(string username)
        {
            AddCorsheader();
            try
            {
                string filePath = jsonPath;
                if (!File.Exists(filePath))
                {
                    var errNotFound = new JObject { ["error"] = "file_not_found", ["message"] = $"JsonUser.txt introuvable ({filePath})" };
                    return errNotFound.ToString();
                }

                string json = File.ReadAllText(filePath);

                JObject root;
                try
                {
                    root = JObject.Parse(json);
                }
                catch
                {
                    // Tentative de correction simple pour virgules finales invalides
                    string fixedJson = Regex.Replace(json, ",\\s*([}\\]])", "$1");
                    root = JObject.Parse(fixedJson);
                }

                var users = root["users"] as JArray;
                if (users == null)
                {
                    var errFormat = new JObject { ["error"] = "invalid_format", ["message"] = "Champ 'users' absent ou non valide dans le fichier JSON." };
                    return errFormat.ToString();
                }

                var userToken = users.FirstOrDefault(t => string.Equals((string)t["username"], username, StringComparison.OrdinalIgnoreCase));
                if (userToken == null)
                {
                    var errUser = new JObject { ["error"] = "not_found", ["message"] = $"Utilisateur '{username}' introuvable." };
                    return errUser.ToString();
                }

                return ((JObject)userToken).ToString();
            }
            catch (Exception ex)
            {
                var err = new JObject { ["error"] = "internal_exception", ["message"] = ex.Message };
                return err.ToString();
            }
        }








    }

}
