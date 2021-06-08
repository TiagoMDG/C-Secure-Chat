using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Projeto_TS_Chat
{
    class KeyGenerator
    {
        private RSACryptoServiceProvider rsaSign;
        
        public string generator()
        {
            rsaSign = new RSACryptoServiceProvider();
            string publicKey = rsaSign.ToXmlString(false);

            return publicKey;

        }

    }
}
