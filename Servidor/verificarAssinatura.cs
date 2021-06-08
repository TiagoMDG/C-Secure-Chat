using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    class verificarAssinatura
    {
        private RSACryptoServiceProvider rsaVerify;

        public bool Verificar(string hash, string assinatura, string publicKey)
        {
            rsaVerify.FromXmlString(publicKey);

            byte[] hashUser = Convert.FromBase64String(hash);
            byte[] assUser = Convert.FromBase64String(assinatura);
            bool verify = rsaVerify.VerifyHash(hashUser, CryptoConfig.MapNameToOID("SHA256"), assUser);
            return verify;
        }        

    }
}
