using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    class ChaveSimetrica
    {
        AesCryptoServiceProvider aes;
        byte[] key;
        byte[] iv;

        public string GerarPrivada(string chavePublicaCliente)
        {
            //INICIALIZAR SERVIÇO DE CIFRAGEM AES
            aes = new AesCryptoServiceProvider();
            //GUARDAR A CHAVE GERADA
            key = aes.Key;
            //GUARDAR O VETOR DE INICIALIZAÇÃO GERADO
            iv = aes.IV;

            //IR BUSCAR CHAVE E IV 
            string keyB64 = GerarChavePrivada(chavePublicaCliente);
            string ivB64 = GerarIv(chavePublicaCliente);
            Console.WriteLine("A chave Privada: " + keyB64);
            Console.WriteLine("O vetor de Inicialização: " + ivB64);

            //CONVERTER DE BASE64 PARA BYTES E SUBSTITUIR NO AES
            aes.Key = Convert.FromBase64String(keyB64);
            aes.IV = Convert.FromBase64String(ivB64);
            return keyB64;
        }

        private string GerarChavePrivada(string pass)
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
        private string GerarIv(string pass)
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
