using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models.NetworkManagement
{
    public class PlatformQRCode
    {
        private short platformId = 0;

        public short PlatformId
        {
            get { return platformId; }
            set { platformId = value; }
        }
        private string platformCode = "";

        public string PlatformCode
        {
            get { return platformCode; }
            set { platformCode = value; }
        }
        private string stationCode = "";

        public string StationCode
        {
            get { return stationCode; }
            set { stationCode = value; }
        }
        private short wall = 0;

        public short Wall
        {
            get { return wall; }
            set { wall = value; }
        }


        private string qrText = "";

        public string QrText
        {
            get { return qrText; }
            set { qrText = value; }
        }

        private byte[] qrImage;

        public byte[] QrImage
        {
            get { return qrImage; }
            set { qrImage = value; }
        }
    }
}