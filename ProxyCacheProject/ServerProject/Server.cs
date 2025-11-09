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

        public string GetItineray(string startLat, string startLon, string endLat, string endLon)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "http://localhost:8080");
            string link = "https://router.project-osrm.org/route/v1/bicycle/" + startLon + "," + startLat + ";" + endLon + "," + endLat + "?steps=true&geometries=geojson&overview=full";
            Console.Write(link);
            return client.LookForData(link, "Itinerary");
        }


        public string BestItinerary(string fromLat, string fromLon, string toLat, string toLon)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");

            // Parse input coords
            if (!double.TryParse(fromLat, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var startLat) ||
                !double.TryParse(fromLon, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var startLon) ||
                !double.TryParse(toLat, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var endLat) ||
                !double.TryParse(toLon, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var endLon))
            {
                throw new WebFaultException<string>("Invalid coordinates format. Use dot as decimal separator (ex: 48.859).", System.Net.HttpStatusCode.BadRequest);
            }

            // profils OSRM à tester
            var walkProfiles = new[] { "walking", "foot" };
            var bikeProfiles = new[] { "bicycle", "cycling" };

            // 1) durée marche directe (OSRM ou fallback)
            JObject routeWalkStartToDest = null;
            bool hasWalkRoute = TryGetAnyOsrmRoute(walkProfiles, startLat, startLon, endLat, endLon, out routeWalkStartToDest);
            double walkStartToDestSec = hasWalkRoute ? GetDurationFromOsrm(routeWalkStartToDest) :
                                         HaversineDistanceMeters(startLat, startLon, endLat, endLon) / WALKING_SPEED_MPS;

            // 2) station la plus proche du départ (ville/contract + station)
            string startNearestRaw = neareastCityWithContract(fromLat, fromLon);
            JObject startNearestJson;
            try
            {
                startNearestJson = JObject.Parse(startNearestRaw);
            }
            catch
            {
                // fallback: marche directe
                if (hasWalkRoute) return routeWalkStartToDest.ToString();
                var sOnly = new JObject
                {
                    ["mode"] = "walk",
                    ["durationSeconds"] = Math.Round(walkStartToDestSec, 1),
                    ["distanceMeters"] = Math.Round(HaversineDistanceMeters(startLat, startLon, endLat, endLon), 1)
                };
                return sOnly.ToString();
            }

            if (startNearestJson["error"] != null || startNearestJson["station"] == null)
            {
                if (hasWalkRoute) return routeWalkStartToDest.ToString();
                var sOnly = new JObject
                {
                    ["mode"] = "walk",
                    ["durationSeconds"] = Math.Round(walkStartToDestSec, 1),
                    ["distanceMeters"] = Math.Round(HaversineDistanceMeters(startLat, startLon, endLat, endLon), 1)
                };
                return sOnly.ToString();
            }

            var startStation = startNearestJson["station"] as JObject;
            if (!TryGetStationPosition(startStation, out double stLat, out double stLon))
            {
                if (hasWalkRoute) return routeWalkStartToDest.ToString();
                var sOnly = new JObject
                {
                    ["mode"] = "walk",
                    ["durationSeconds"] = Math.Round(walkStartToDestSec, 1),
                    ["distanceMeters"] = Math.Round(HaversineDistanceMeters(startLat, startLon, endLat, endLon), 1)
                };
                return sOnly.ToString();
            }

            // 3) durée marche départ -> station A (OSRM ou fallback)
            JObject routeWalkStartToStation = null;
            bool hasWalkStartToStation = TryGetAnyOsrmRoute(walkProfiles, startLat, startLon, stLat, stLon, out routeWalkStartToStation);
            double walkStartToStationSec = hasWalkStartToStation ? GetDurationFromOsrm(routeWalkStartToStation) :
                                            HaversineDistanceMeters(startLat, startLon, stLat, stLon) / WALKING_SPEED_MPS;

            // Règle initiale : si aller à la station la plus proche prend plus de temps que marcher directement -> marcher direct
            if (walkStartToStationSec > walkStartToDestSec)
            {
                if (hasWalkRoute) return routeWalkStartToDest.ToString();
                var sOnly = new JObject
                {
                    ["mode"] = "walk",
                    ["durationSeconds"] = Math.Round(walkStartToDestSec, 1),
                    ["distanceMeters"] = Math.Round(HaversineDistanceMeters(startLat, startLon, endLat, endLon), 1)
                };
                return sOnly.ToString();
            }

            // 4) station la plus proche de l'arrivée
            string endNearestRaw = neareastCityWithContract(toLat, toLon);
            JObject endNearestJson;
            try
            {
                endNearestJson = JObject.Parse(endNearestRaw);
            }
            catch
            {
                // fallback: marche
                if (hasWalkRoute) return routeWalkStartToDest.ToString();
                var sOnly = new JObject
                {
                    ["mode"] = "walk",
                    ["durationSeconds"] = Math.Round(walkStartToDestSec, 1),
                    ["distanceMeters"] = Math.Round(HaversineDistanceMeters(startLat, startLon, endLat, endLon), 1)
                };
                return sOnly.ToString();
            }

            if (endNearestJson["error"] != null || endNearestJson["station"] == null)
            {
                if (hasWalkRoute) return routeWalkStartToDest.ToString();
                var sOnly = new JObject
                {
                    ["mode"] = "walk",
                    ["durationSeconds"] = Math.Round(walkStartToDestSec, 1),
                    ["distanceMeters"] = Math.Round(HaversineDistanceMeters(startLat, startLon, endLat, endLon), 1)
                };
                return sOnly.ToString();
            }

            var endStation = endNearestJson["station"] as JObject;
            if (!TryGetStationPosition(endStation, out double edLat, out double edLon))
            {
                if (hasWalkRoute) return routeWalkStartToDest.ToString();
                var sOnly = new JObject
                {
                    ["mode"] = "walk",
                    ["durationSeconds"] = Math.Round(walkStartToDestSec, 1),
                    ["distanceMeters"] = Math.Round(HaversineDistanceMeters(startLat, startLon, endLat, endLon), 1)
                };
                return sOnly.ToString();
            }

            // 5) durée vélo stationA -> stationB (OSRM ou fallback)
            JObject routeBikeStations = null;
            bool hasBikeBetween = TryGetAnyOsrmRoute(bikeProfiles, stLat, stLon, edLat, edLon, out routeBikeStations);
            double bikeBetweenSec = hasBikeBetween ? GetDurationFromOsrm(routeBikeStations) :
                                     HaversineDistanceMeters(stLat, stLon, edLat, edLon) / BIKING_SPEED_MPS;

            // 6) durée marche stationB -> destination (OSRM ou fallback)
            JObject routeWalkEndStationToDest = null;
            bool hasWalkEndStationToDest = TryGetAnyOsrmRoute(walkProfiles, edLat, edLon, endLat, endLon, out routeWalkEndStationToDest);
            double walkEndStationToDestSec = hasWalkEndStationToDest ? GetDurationFromOsrm(routeWalkEndStationToDest) :
                                               HaversineDistanceMeters(edLat, edLon, endLat, endLon) / WALKING_SPEED_MPS;

            double combinedSec = walkStartToStationSec + bikeBetweenSec + walkEndStationToDestSec;

            // Si la combinaison walk+bike+walk est plus rapide => renvoyer plan composite
            if (combinedSec < walkStartToDestSec)
            {
                var segments = new JArray();

                segments.Add(new JObject
                {
                    ["type"] = "walk",
                    ["from"] = new JObject { ["lat"] = startLat, ["lon"] = startLon },
                    ["to"] = new JObject { ["lat"] = stLat, ["lon"] = stLon },
                    ["durationSeconds"] = Math.Round(walkStartToStationSec, 1),
                    ["distanceMeters"] = Math.Round(hasWalkStartToStation ? GetDistanceFromOsrm(routeWalkStartToStation) : HaversineDistanceMeters(startLat, startLon, stLat, stLon), 1),
                    ["osrm"] = hasWalkStartToStation ? routeWalkStartToStation : null
                });

                segments.Add(new JObject
                {
                    ["type"] = "bike",
                    ["from"] = new JObject { ["lat"] = stLat, ["lon"] = stLon },
                    ["to"] = new JObject { ["lat"] = edLat, ["lon"] = edLon },
                    ["durationSeconds"] = Math.Round(bikeBetweenSec, 1),
                    ["distanceMeters"] = Math.Round(hasBikeBetween ? GetDistanceFromOsrm(routeBikeStations) : HaversineDistanceMeters(stLat, stLon, edLat, edLon), 1),
                    ["osrm"] = hasBikeBetween ? routeBikeStations : null
                });

                segments.Add(new JObject
                {
                    ["type"] = "walk",
                    ["from"] = new JObject { ["lat"] = edLat, ["lon"] = edLon },
                    ["to"] = new JObject { ["lat"] = endLat, ["lon"] = endLon },
                    ["durationSeconds"] = Math.Round(walkEndStationToDestSec, 1),
                    ["distanceMeters"] = Math.Round(hasWalkEndStationToDest ? GetDistanceFromOsrm(routeWalkEndStationToDest) : HaversineDistanceMeters(edLat, edLon, endLat, endLon), 1),
                    ["osrm"] = hasWalkEndStationToDest ? routeWalkEndStationToDest : null
                });

                var result = new JObject
                {
                    ["mode"] = "walk+bike",
                    ["totalDurationSeconds"] = Math.Round(combinedSec, 1),
                    ["segments"] = segments
                };

                return result.ToString();
            }
            else
            {
                // marche directe est plus courte (ou égal)
                if (hasWalkRoute) return routeWalkStartToDest.ToString();

                var sOnly = new JObject
                {
                    ["mode"] = "walk",
                    ["durationSeconds"] = Math.Round(walkStartToDestSec, 1),
                    ["distanceMeters"] = Math.Round(HaversineDistanceMeters(startLat, startLon, endLat, endLon), 1)
                };
                return sOnly.ToString();
            }
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
