using WeatherStation.ViewModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

using Xunit;

namespace WeatherStationTest
{
    [TestClass]
    public class MainViewModelTest
    {
        private MainViewModel viewmodel;

        public MainViewModelTest()
        {
            this.viewmodel = new MainViewModel();
        }

        [TestMethod]
        public void BtnGetCoordinates_Click_should_able_to_get_coordinates_with_plz_and_country()
        {
            // arrange
            var testdata = new object[] { "20255", "DE", "", "" };
            this.viewmodel.Latitude = 0;
            //if (this.viewmodel.Plz == testdata[0].ToString() && this.viewmodel.Country == testdata[1].ToString())
            //{
            //    return;
            //}

            // act
            this.viewmodel.BtnGetCoordinates_Click(testdata);

            // assert
            Assert.IsTrue(this.viewmodel.Latitude > 0);
        }

        [TestMethod]
        public void GetWeatherData_should_able_to_get_weather_with_plz_and_country()
        {
            // arrange
            this.viewmodel.Temperature = -500;

            // act
            var result = this.viewmodel.GetWeatherData();

            // assert
            Assert.IsTrue(result && this.viewmodel.Temperature > -500);
        }
    }
}
