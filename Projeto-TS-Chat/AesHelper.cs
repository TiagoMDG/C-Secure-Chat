using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace Projeto_TS_Chat
{
    class AesHelper
    {
        AesCryptoServiceProvider aes;

        public string GerarChavePrivada(string pass)
        {
            byte[] salt = new byte[] { 0, 1, 0, 8, 2, 9, 9, 7 };
            Rfc2898DeriveBytes pwdGen = new Rfc2898DeriveBytes(pass, salt, 1000);
            //GERAR KEY
            byte[] key = pwdGen.GetBytes(16);
            //CONVERTER A PASS PARA BASE64
            string passB64 = Convert.ToBase64String(key);
            //DEVOLVER A PASS EM BYTES
            return passB64;
        }

        //GERAR UM VETOR DE INICIALIZAÇÃO A PARTIR DE UMA STRING
        public string GerarIv(string pass)
        {
            byte[] salt = new byte[] { 7, 8, 7, 8, 2, 5, 9, 5 };
            Rfc2898DeriveBytes pwdGen = new Rfc2898DeriveBytes(pass, salt, 1000);
            //GERAR UMA KEY
            byte[] iv = pwdGen.GetBytes(16);
            //CONVERTER PARA BASE64
            string ivB64 = Convert.ToBase64String(iv);
            //DEVOLVER EM BYTES
            return ivB64;
        }
    }
}
