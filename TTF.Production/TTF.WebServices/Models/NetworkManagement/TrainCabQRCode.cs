using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models.NetworkManagement
{
    public class TrainCabQRCode
    {

        private string trainType = "";

        public string TrainType
        {
            get { return trainType; }
            set { trainType = value; }
        }
        

        private string emuCode = "";

        public string EMUCode
        {
            get { return emuCode; }
            set { emuCode = value; }
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