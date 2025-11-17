using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace ServerProject
{
    // REMARQUE : vous pouvez utiliser la commande Renommer du menu Refactoriser pour changer le nom d'interface "IService1" à la fois dans le code et le fichier de configuration.
    [ServiceContract]
    public interface IServer
    {

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/getBikeItineray?fromLat={fromLat}&fromLon={fromLon}&toLat={toLat}&toLon={toLon}", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        string GetBikeItineray(string fromLat, string fromLon, string toLat, string toLon);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/getPedestrianItinerary?fromLat={fromLat}&fromLon={fromLon}&toLat={toLat}&toLon={toLon}", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        string GetPedestrianItinerary(string fromLat, string fromLon, string toLat, string toLon);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/bestItinerary?fromLat={fromLat}&fromLon={fromLon}&toLat={toLat}&toLon={toLon}", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        string BestItinerary(string fromLat, string fromLon, string toLat, string toLon);




    }

    // Utilisez un contrat de données comme indiqué dans l'exemple ci-après pour ajouter les types composites aux opérations de service.
    // Vous pouvez ajouter des fichiers XSD au projet. Une fois le projet généré, vous pouvez utiliser directement les types de données qui y sont définis, avec l'espace de noms "ServerProject.ContractType".
    [DataContract]
    public class CompositeType
    {
        string link = "http";
        string type = "Contract";

        // Constructeur sans paramètres requis pour la désérialisation WCF
        public CompositeType() { }

        public CompositeType(string link,string type)
        {
            this.link = link;
            this.type = type;
        }



        [DataMember]
        public string Link
        {
            get { return link; }
            set { link = value; }
        }

        [DataMember] 
        public string Type
        {
            get { return type; }
            set { type = value; }
        }
    }
}
