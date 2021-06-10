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
        //private RSACryptoServiceProvider rsaSign;
        AesCryptoServiceProvider aes;
        private RSACryptoServiceProvider rsa;

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

            ////Obter um fluxo de clientes para leitura e escrita
            networkStream = client.GetStream();

            //// Preparação da comunicação utilizando a classe desenvolvida pelo DEI
            protocolSI = new ProtocolSI();

            ////inicializar o serviço AES
            aes = new AesCryptoServiceProvider();
            ////guardar a chave gerada
            key = aes.Key;
            ////guardar o vetro de de inicialização IV
            iv = aes.IV;

            enviarReceberChaves();

        }
        private string GerarChavePublica()
        {
            rsa = new RSACryptoServiceProvider();
            //CRIAR E DEVOLVER UMA STRING QUE CONTÉM A CHAVE PÚBLICA
            publicKey = rsa.ToXmlString(false);
            //CRIAR E DEVOLVER UMA STRING COM CHAVE PÚBLICA E PRIVADA
            string bothkeys = rsa.ToXmlString(true);
           
            return publicKey;
        }
        private void enviarReceberChaves()
        {
            publicKey = GerarChavePublica();

            byte[] packet = protocolSI.Make(ProtocolSICmdType.PUBLIC_KEY, publicKey);
            networkStream.Write(packet, 0, packet.Length);

            //receber chave privada
            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

            string chavePrivadaCifrada;
            chavePrivadaCifrada = protocolSI.GetStringFromData();

            //INICIALIZAR SERVIÇO DE CIFRAGEM AES
            aes = new AesCryptoServiceProvider();
            //GUARDAR A CHAVE GERADA
            key = aes.Key;
            //GUARDAR O VETOR DE INICIALIZAÇÃO GERADO
            iv = aes.IV;
            //IR BUSCAR CHAVE E IV 
            string keyB64 = GerarChavePrivada(publicKey);
            string ivB64 = GerarIv(publicKey);

            //CONVERTER DE BASE64 PARA BYTES E SUBSTITUIR NO AES
            aes.Key = Convert.FromBase64String(keyB64);
            aes.IV = Convert.FromBase64String(ivB64);
            //IR BUSCAR O TEXTO DA TEXTBOX TEXTOCIFRADO (BASE64)
            //string textoCifrado = tb_TextoCifrado.Text;
            //CHAMAR A FUNÇÃO DECIFRARTEXTO E ENVIAR TEXTO GUARDADO ANTES E GUARDÁ-LO NA VARÍAVEL TEXTODECIFRADO
            chavePrivadaDecifrada = DecifrarTexto(chavePrivadaCifrada);

            Console.WriteLine(chavePrivadaDecifrada);
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            String pass = textBoxPwLogin.Text;
            String username = textBoxUserLogin.Text;

            Login(username, pass);

            
        }
        private void buttonRegistar_Click(object sender, EventArgs e)
        {
            String pass = textBoxPwRegistar.Text;
            String username = textBoxUserRegistar.Text;
            byte[] salt = GenerateSalt(SALTSIZE);
            byte[] hash = GenerateSaltedHash(pass, salt);

            Register(username, hash, salt, publicKey);

        }

        private void Login(string user, string password)
        {
            //Login opçao SI USER_OPTION_1

            //mensagem a enviar
            string msg = user + '|' + password;

            //pacote a enviar pelo protocolo SI
            byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, CifrarTexto(msg));

            // Enviar mensagem
            networkStream.Write(packet, 0, packet.Length);
            //recebe confirmaçao do servidor que o login foi um sucesso!
            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            if (protocolSI.GetCmdType() == ProtocolSICmdType.ACK)
            {
                this.Hide();
                FormChatBox f1 = new FormChatBox();

                f1.ShowDialog();
                this.Close();
            }
            else
            {
                MessageBox.Show("O seu username ou password estão incorretos ou não existem", "Erro de autenticação!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            }
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
            byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2, CifrarTexto(msg));

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

        private string CifrarTexto(string txt)
        {
            //VARIÁVEL PARA GUARDAR O TEXTO DECIFRADO EM BYTES
            byte[] txtDecifrado = Encoding.UTF8.GetBytes(txt);
            //VARIÁVEL PARA GUARDAR O TEXTO CIFRADO EM BYTES
            byte[] txtCifrado;
            //RESERVAR ESPAÇO NA MEMÓRIA PARA COLOCAR O TEXTO E CIFRÁ-LO
            MemoryStream ms = new MemoryStream();
            //INICIALIZAR O SISTEMA DE CIFRAGEM (WRITE)
            CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            //CRIFRAR OS DADOS
            cs.Write(txtDecifrado, 0, txtDecifrado.Length);
            cs.Close();
            //GUARDAR OS DADOS CRIFRADO QUE ESTÃO NA MEMÓRIA
            txtCifrado = ms.ToArray();
            //CONVERTER OS BYTES PARA BASE64 (TEXTO)
            string txtCifradoB64 = Convert.ToBase64String(txtCifrado);
            //DEVOLVER OS BYTES CRIADOS EM BASE64
            return txtCifradoB64;
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

        public string DecifrarTexto(string txtCifradoB64)
        {
            //VARIÁVEL PARA GUARDAR O TEXTO CIFRADO EM BYTES
            byte[] txtCifrado = Convert.FromBase64String(txtCifradoB64);
            //RESERVAR ESPAÇO NA MEMÓRIA PARA COLOCAR O TEXTO E CIFRÁ-LO
            MemoryStream ms = new MemoryStream(txtCifrado);
            //INICIALIZAR O SISTEMA DE CIFRAGEM (READ)
            CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);

            //VARIÁVEL PARA GUARDO O TEXTO DECIFRADO
            byte[] txtDecifrado = new byte[ms.Length];
            //VARIÁVEL PARA TER O NÚMERO DE BYTES DECIFRADOS
            int bytesLidos = 0;
            //DECIFRAR OS DADOS
            bytesLidos = cs.Read(txtDecifrado, 0, txtDecifrado.Length);
            cs.Close();
            //CONVERTER PARA TEXTO
            string textoDecifrado = Encoding.UTF8.GetString(txtDecifrado, 0, bytesLidos);
            //DEVOLVER TEXTO DECRIFRADO
            return textoDecifrado;
        }
    }
}
