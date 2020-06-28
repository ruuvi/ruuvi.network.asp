using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace RuuviTagApp
{
    //class for unpackin rawdata.Use CutTagToLists method to unpack data from version 3 and 5
    public class UnpackRawData
    {
        public int volt = 0;
        public string vers { get; set; }
        public string temperature { get; set; }
        public string temperatureFrac { get; set; }
        public string humidity { get; set; }
        public string pressure { get; set; }
        public string accX { get; set; }
        public string accY { get; set; }
        public string accZ { get; set; }
        public string voltage { get; set; }
        public string txPower { get; set; }
        public string moveCounter { get; set; }
        public string moveSeq { get; set; }
        public string MAC { get; set; }

        public void UnpackAllData(string data)
        {
            CutTagToLists(data);
        }

        public void CutTagToLists(string data)
        {
            // version from rawdata, unpack all necessary data
            version(data.Substring(14, 2));

            if (vers == "05")
            {
                addTemp(data.Substring(16, 4));
                addHumid(data.Substring(20, 4));
                addPress(data.Substring(24, 4));
                addAccX(data.Substring(28, 4));
                addAccy(data.Substring(32, 4));
                addAccz(data.Substring(36, 4));
                addVolt(data.Substring(40, 4));
                addTx(data);
                addMovecount(data.Substring(44, 4));
                addMoveseq(data.Substring(48, 2));
                addMac(data.Substring(50, 12));
            }
            if (vers == "03")
            {
                addHumid(data.Substring(16, 2));
                addTemp(data.Substring(18, 4));
                //addTempfrac(data.Substring(20, 2));
                addPress(data.Substring(22, 4));
                addAccX(data.Substring(26, 4));
                addAccy(data.Substring(30, 4));
                addAccz(data.Substring(34, 4));
                addVolt(data.Substring(38, 4));
                txPower = null;
                moveCounter = null;
                moveSeq = null;
                MAC = null;
            }

        }

        private void addTempfrac(string v)
        {
            throw new NotImplementedException();
        }

        public void version(string data)
        {
            if (data == "05" || data == "03")
            {
                vers = data;
            }
            else
            {
                throw new InvalidOperationException();
            }


        }
        private void addTemp(string data)
        {
            if (vers == "05")
            {
                var t = (Convert.ToInt32(data, 16) * 0.005);
                if (t >= -163.835 && t <= 163.835)
                {
                    temperature = t.ToString(CultureInfo.GetCultureInfo("en-US"));
                }
                else temperature = "NAN";

            }
            if (vers == "03")
            {

                var t = (Convert.ToInt32(data.Substring(0, 2), 16)) + (Convert.ToInt32(data.Substring(2, 2), 16) * 0.01);
                if (t >= -127.99 && t <= 127.99)
                {
                    temperature = t.ToString(CultureInfo.GetCultureInfo("en-US"));
                }
                else temperature = "NAN";
            }
        }

        private void addHumid(string data)
        {
            if (vers == "05")
            {
                var t = (Convert.ToInt32(data, 16) * 0.0025);
                if (t >= 0.000 && t <= 163.8350)
                {
                    humidity = t.ToString(CultureInfo.GetCultureInfo("en-US"));
                }
                else humidity = "NAN";
            }
            if (vers == "03")
            {
                var t = (Convert.ToInt32(data, 16) * 0.5);
                if (t >= 0.0 && t <= 127.5)
                {
                    humidity = t.ToString(CultureInfo.GetCultureInfo("en-US"));
                }
                else humidity = "NAN";
            }
        }
        private void addPress(string data)
        {
            var t = (Convert.ToInt32(data, 16) + 50000);
            if (vers == "05")
            {
                if (t >= 5000 && t <= 115534)
                {
                    t = t / 100;
                    pressure = t.ToString();
                }
                else pressure = "NAN";
            }
            if (vers == "03")
            {
                if (t >= 50000 && t <= 115535)
                {
                    t = t / 100;
                    pressure = t.ToString();
                }
                else pressure = "NAN";
            }

        }

        private void addAccX(string data)
        {
            int v = Int16.Parse(data, NumberStyles.HexNumber);
            accX = (v * 0.001).ToString();
        }

        private void addAccy(string data)
        {
            int v = Int16.Parse(data, NumberStyles.HexNumber);
            accX = (v * 0.001).ToString();
        }

        private void addAccz(string data)
        {
            int v = Int16.Parse(data, NumberStyles.HexNumber);
            accZ = (v * 0.001).ToString();
        }
        private void addTx(string data)
        {
            var tx = volt % 32;
            var tx2 = (-40 + (2 * tx));
            if (tx2 >= -40 && tx2 <= 20)
            {
                txPower = tx2.ToString();
            }
            else txPower = "NAN";
        }
        public void addVolt(string data)
        {
            var t = (Convert.ToInt32(data, 16));
            volt = t;
            float todec = ((t / 32) + 1600);
            if (vers == "05")
            {
                if (todec >= 1600 && todec <= 3646)
                {
                    voltage = (todec * 0.001).ToString();
                }
                else voltage = "NAN";
            }
            if (vers == "03")
            {
                var d = t * 0.001;
                Console.WriteLine(data);
                Console.WriteLine(t);
                if (d >= 0.000 && d <= 65535)
                {
                    voltage = d.ToString();
                }
                else voltage = "NaN";
            }

        }

        public void addMovecount(string data)
        {
            var count = 0;
            var t = (Convert.ToInt32(data, 16));
            for (int i = 0; i < t; i++)
            {
                if (count > 254)
                {
                    count = 1;
                }
                count++;
                i++;
            }
            moveCounter = count.ToString();
        }

        public void addMoveseq(string data)
        {
            var t = (Convert.ToInt32(data, 16));
            moveSeq = t.ToString();
        }
        public void addMac(string data)
        {
            MAC = data.ToString().ToUpper();
        }
    }
}
