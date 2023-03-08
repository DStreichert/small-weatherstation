using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WeatherStation.API.CurrentWeatherData.Response
{
    [DataContract]
    public class Get
    {
        [DataMember]
        public MainData main { get; set; }
    }

    public class MainData
    {
        public double temp { get; set; }
        public double pressure { get; set; }
        public double humidity { get; set; }
    }
}
