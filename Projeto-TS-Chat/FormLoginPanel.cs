using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using EI.SI;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Projeto_TS_Chat
{
    public partial class FormLoginPanel : Form
    {
        //protoclo Si e socket de comunicaçao com o servidor
        ProtocolSI protocolSI;
        private const int PORT = 10000;
        NetworkStream networkStream;
        TcpClient client;
        private RSACryptoServiceProvider rsaSign;
        AesCryptoServiceProvider aes;
        
        //constantes par geral as pass's com salt
        private const int SALTSIZE = 8;
        private const int NUMBER_OF_ITERATIONS = 1000;
        byte[] key;
        byte[] iv;
        string publicKey;
        string chavePrivadaDecifrada;

        public FormLoginPanel()
        {
            InitializeComponent();

            //iniciar o componente de cominicaçao de cliente
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, PORT);
            client = new TcpClient();
            client.Connect(endPoint);

            rsaSign = new RSACryptoServiceProvider();
            string publicKey = rsaSign.ToXmlString(false);

            //Obter um fluxo de clientes para leitura e escrita
            networkStream = client.GetStream();

            // Preparação da comunicação utilizando a classe desenvolvida pelo DEI
            protocolSI = new ProtocolSI();

            //inicializar o serviço AES
            aes = new AesCryptoServiceProvider();
            //guardar a chave gerada
            key = aes.Key;
            //guardar o vetro de de inicialização IV
            iv = aes.IV;

            enviarReceberChaves();
         
        }
        private void enviarReceberChaves()
        {
            KeyGenerator k = new KeyGenerator();

            publicKey = k.generator();

            byte[] packet = protocolSI.Make(ProtocolSICmdType.PUBLIC_KEY, publicKey);
            networkStream.Write(packet, 0, packet.Length);

            GerarChavePrivada(publicKey);

            //receber chave privada
            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

            string chavePrivadaCifrada;
            chavePrivadaCifrada = protocolSI.GetStringFromData();
            Console.WriteLine(chavePrivadaCifrada);
            /*
            byte[] txtcifrado = Convert.FromBase64String(chavePrivadaCifrada);
            MemoryStream ms = new MemoryStream(txtcifrado);
            CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            //variavel para guardar texto decifrado
            byte[] textDecifrado = new byte[ms.Length];
            //variavel para numeros de bytes decifrados
            int bytesLidos = 0;
            bytesLidos = cs.Read(textDecifrado, 0, textDecifrado.Length);
            cs.Close();
            //converter para texto
            chavePrivadaDecifrada = Encoding.UTF8.GetString(textDecifrado, 0, textDecifrado.Length);

            Console.WriteLine(chavePrivadaDecifrada);
            */
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            String password = textBoxPwLogin.Text;
            String username = textBoxUserLogin.Text;

            this.Hide();
            FormChatBox f1 = new FormChatBox();
            f1.ShowDialog();
            this.Close();
        }
        private void buttonRegistar_Click(object sender, EventArgs e)
        {
            String pass = textBoxPwRegistar.Text;
            String username = textBoxUserRegistar.Text;
            byte[] salt = GenerateSalt(SALTSIZE);
            byte[] hash = GenerateSaltedHash(pass, salt);

            Register(username, hash, salt, publicKey);

            this.Hide();
            FormChatBox f1 = new FormChatBox();
            f1.ShowDialog();
            this.Close();
        }

        private void Register(string user, byte[] passHash, byte[] passSalt, string publicKey)
        {
            //registar opçao SI USER_OPTION_2
            //converter encriptaçao para string para ser enviado
            string passwordHash = Convert.ToBase64String(passHash);
            string passwordSalt = Convert.ToBase64String(passSalt);

            //mensagem a enviar
            string msg =  user+'|'+ passwordHash + '|' + passwordSalt + '|' + publicKey;

            //pacote a enviar pelo protocolo SI
            byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2, msg);

            // Enviar mensagem
            networkStream.Write(packet, 0, packet.Length);
            while (protocolSI.GetCmdType() != ProtocolSICmdType.ACK)
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            }
        }

        private static byte[] GenerateSalt(int size)
        {
            //Generate a cryptographic random number.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[size];
            rng.GetBytes(buff);
            return buff;
        }

        private static byte[] GenerateSaltedHash(string plainText, byte[] salt)
        {
            Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(plainText, salt, NUMBER_OF_ITERATIONS);
            return rfc2898.GetBytes(32);
        }

        private string GerarPrivada(string chavePublicaCliente)
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
