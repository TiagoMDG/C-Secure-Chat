﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EI.SI;


namespace Servidor
{    
    class ChaveSimetrica
    {
        AesCryptoServiceProvider aes=new AesCryptoServiceProvider();
        public byte[] key;
        public byte[] iv;

        public string GerarPrivada(string chavePublicaCliente)
        {
            //INICIALIZAR SERVIÇO DE CIFRAGEM AES
            
            //GUARDAR A CHAVE GERADA
            key = aes.Key;
            //GUARDAR O VETOR DE INICIALIZAÇÃO GERADO
            iv = aes.IV;

            //IR BUSCAR CHAVE E IV 
            string keyB64 = GerarChavePrivada(chavePublicaCliente);
            string ivB64 = GerarIv(chavePublicaCliente);
            //Console.WriteLine("A chave Privada: " + keyB64);
            //Console.WriteLine("O vetor de Inicialização: " + ivB64);

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

        public string CifrarPrivada(string chavePrivada)
        {
            byte[] chavePrivadaParaCifrar = Encoding.UTF8.GetBytes(chavePrivada);
            byte[] chavePrivadaCifrada;

            //reservar memoria para o processo de cifragem
            MemoryStream ms = new MemoryStream();
            //inicializar o sistema de cifragem
            CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            //cifrar os dados
            cs.Write(chavePrivadaParaCifrar, 0, chavePrivadaParaCifrar.Length);
            cs.Close();
            //guardar os dados cifrados que estão em memória
            chavePrivadaCifrada = ms.ToArray();
            //converter texto cifrado em base 64
            string strChavePrivadaCifrada = Convert.ToBase64String(chavePrivadaCifrada);
            return strChavePrivadaCifrada;
        }

        public string DecifrarTexto(string txtCifradoB64, byte[] privateKey, byte[] privateKeyIV)
        {
            //VARIÁVEL PARA GUARDAR O TEXTO CIFRADO EM BYTES
            byte[] txtCifrado = Convert.FromBase64String(txtCifradoB64);
            //RESERVAR ESPAÇO NA MEMÓRIA PARA COLOCAR O TEXTO E CIFRÁ-LO
            MemoryStream ms = new MemoryStream(txtCifrado);
            //INICIALIZAR O SISTEMA DE CIFRAGEM (READ)

            CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(privateKey, privateKeyIV), CryptoStreamMode.Read);

            //variavel para guardar texto decifrado
            byte[] textDecifrado = new byte[ms.Length];
            //variavel para numeros de bytes decifrados
            int bytesLidos = 0;
            bytesLidos = cs.Read(textDecifrado, 0, textDecifrado.Length);
            cs.Close();
            //converter para texto
            string txtConvertido = Encoding.UTF8.GetString(textDecifrado, 0, bytesLidos);
            return txtConvertido;
        }
    }
}
