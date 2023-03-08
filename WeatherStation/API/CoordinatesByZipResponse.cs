using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WeatherStation.API.CoordinatesByZip.Response
{
    [DataContract]
    public class Get
    {
        [DataMember]
        public string zip { get; set; }

        [DataMember]
        public string name { get; set; }

        [DataMember]
        public decimal lat { get; set; }

        [DataMember]
        public decimal lon { get; set; }

        [DataMember]
        public string country { get; set; }
    }
}
