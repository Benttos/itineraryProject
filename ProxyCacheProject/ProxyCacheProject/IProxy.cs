using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace ProxyCacheProject
{
    // REMARQUE : vous pouvez utiliser la commande Renommer du menu Refactoriser pour changer le nom d'interface "IService1" à la fois dans le code et le fichier de configuration.
    [ServiceContract]
    public interface IProxy
    {
        [OperationContract]
        string LookForData(string link,string type);


        // TODO: ajoutez vos opérations de service ici
    }

    // Utilisez un contrat de données comme indiqué dans l'exemple ci-après pour ajouter les types composites aux opérations de service.
    // Vous pouvez ajouter des fichiers XSD au projet. Une fois le projet généré, vous pouvez utiliser directement les types de données qui y sont définis, avec l'espace de noms "ProxyCacheProject.ContractType".
    [DataContract]
    public class CompositeType
    {
        string type = "Contract";
        string link = "http";

        
        public CompositeType(string link,string type) { 
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
            { get { return type; } set { type = value; } }
    }
}
