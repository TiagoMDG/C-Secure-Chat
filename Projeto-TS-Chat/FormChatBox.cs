using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;
using EI.SI;

namespace Projeto_TS_Chat
{
    public partial class FormChatBox : Form
    {
        ProtocolSI protocolSI;
        private const int PORT = 10000;
        NetworkStream networkStream;
        TcpClient client;
        CspParameters cspp = new CspParameters();
        private RSACryptoServiceProvider rsa;

        const string EncrFolder = @"c:\z_TS\Encrypt\";
        const string SrcFolder = @"c:\z_TS\Encrypt\";
        const string PubKeyFile = @"c:\z_TS\encrypt\rsaPublicKey.txt";
        const string keyName = "Key01";

        byte[] chavePrivadaDecifrada;

        string publickey, keyB64, ivB64;
        int bytesRead = 0;

        private byte[] key;
        private byte[] iv;
        AesCryptoServiceProvider aes;

        public FormChatBox()
        {
            InitializeComponent();

            // Criar um cliente TcpCliente
            //Nota, para que este cliente funcione, é preciso ter um TcpServer
            //ligado ao mesmo endereço especificado pelo servidor e porta

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, PORT);
            client = new TcpClient();
            client.Connect(endPoint);

            //Obter um fluxo de clientes para leitura e escrita
            networkStream = client.GetStream();

            // Preparação da comunicação utilizando a classe desenvolvida pelo DEI
            protocolSI = new ProtocolSI();


        }

        private void buttonEnviar_Click(object sender, EventArgs e)
        {
            // Preparar mensagem para ser enviada
            string msg = textBoxMessage.Text;
            textBoxMessage.Clear();
            messageChat.Items.Add("Utilizador: "+msg);
            
            //preparar a mensagem para ser enviada
            byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, CifrarTexto(msg));

            //enviar mensagem
            networkStream.Write(packet, 0, packet.Length);
            bytesRead = networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

            string msgRecebida = protocolSI.GetStringFromData();
            messageChat.Items.Add(" enviou a seguinte mensagem: " + DecifrarTexto(msgRecebida));
            Console.WriteLine(" enviou a seguinte mensagem: " + DecifrarTexto(msgRecebida));
        }

        private void buttonSair_Click(object sender, EventArgs e)
        {
            CloseClient();
            this.Close();
        }

        private void CloseClient()
        {
            // Preparar envio da mensagem para desligar a ligação
            byte[] eot = protocolSI.Make(ProtocolSICmdType.EOT);
            networkStream.Write(eot, 0, eot.Length);
            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            networkStream.Close();
            client.Close();
        }

        private string GerarChavePublica()
        {
            rsa = new RSACryptoServiceProvider();
            //CRIAR E DEVOLVER UMA STRING QUE CONTÉM A CHAVE PÚBLICA
            publickey = rsa.ToXmlString(false);
            //CRIAR E DEVOLVER UMA STRING COM CHAVE PÚBLICA E PRIVADA
            string bothkeys = rsa.ToXmlString(true);
            return publickey;
        }

        //GERAR UMA CHAVE SIMÉTRICA A PARTIR DE UMA STRING
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


        //FUNÇÃO PARA CIFRAR O TEXTO
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

        private string DecifrarTexto(string txtCifradoB64)
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



        private void EncryptFile(string inFile)
        {

            // Create instance of Aes for
            // symmetric encryption of the data.
            Aes aes = Aes.Create();
            ICryptoTransform transform = aes.CreateEncryptor();

            // Use RSACryptoServiceProvider to
            // encrypt the AES key.
            // rsa is previously instantiated:
            //    rsa = new RSACryptoServiceProvider(cspp);
            byte[] keyEncrypted = rsa.Encrypt(aes.Key, false);

            // Create byte arrays to contain
            // the length values of the key and IV.
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            int lKey = keyEncrypted.Length;
            LenK = BitConverter.GetBytes(lKey);
            int lIV = aes.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);

            // Write the following to the FileStream
            // for the encrypted file (outFs):
            // - length of the key
            // - length of the IV
            // - ecrypted key
            // - the IV
            // - the encrypted cipher content

            int startFileName = inFile.LastIndexOf("\\") + 1;
            // Change the file's extension to ".enc"
            string outFile = EncrFolder + inFile.Substring(startFileName, inFile.LastIndexOf(".") - startFileName) + ".enc";

            using (FileStream outFs = new FileStream(outFile, FileMode.Create))
            {

                outFs.Write(LenK, 0, 4);
                outFs.Write(LenIV, 0, 4);
                outFs.Write(keyEncrypted, 0, lKey);
                outFs.Write(aes.IV, 0, lIV);

                // Now write the cipher text using
                // a CryptoStream for encrypting.
                using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                {

                    // By encrypting a chunk at
                    // a time, you can save memory
                    // and accommodate large files.
                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size.
                    int blockSizeBytes = aes.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];
                    int bytesRead = 0;

                    using (FileStream inFs = new FileStream(inFile, FileMode.Open))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamEncrypted.Write(data, 0, count);
                            bytesRead += blockSizeBytes;
                        }
                        while (count > 0);
                        inFs.Close();
                    }
                    outStreamEncrypted.FlushFinalBlock();
                    outStreamEncrypted.Close();
                }
                outFs.Close();
            }
        }

        private void buttonUpFile_Click(object sender, EventArgs e)
        {
            rsakey();
            openFileDialog1.InitialDirectory = SrcFolder;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fName = openFileDialog1.FileName;
                if (fName != null)
                {
                    FileInfo fInfo = new FileInfo(fName);
                    // Pass the file name without the path.
                    string name = fInfo.FullName;
                    EncryptFile(name);
                }
            }
        }

        private void rsakey()
        {
            StreamReader sr = new StreamReader(PubKeyFile);
            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            string keytxt = sr.ReadToEnd();
            rsa.FromXmlString(keytxt);
            rsa.PersistKeyInCsp = true;
            sr.Close();
        }

        private void FormChatBox_Load(object sender, EventArgs e)
        {
            //INICIALIZAR SERVIÇO DE CIFRAGEM AES
            aes = new AesCryptoServiceProvider();
            //GUARDAR A CHAVE GERADA
            key = aes.Key;
            //GUARDAR O VETOR DE INICIALIZAÇÃO GERADO
            iv = aes.IV;
            //IR BUSCAR CHAVE E IV

            publickey = GerarChavePublica();
            keyB64 = GerarChavePrivada(publickey);
            ivB64 = GerarIv(publickey);


            //CONVERTER DE BASE64 PARA BYTES E SUBSTITUIR NO AES
            aes.Key = Convert.FromBase64String(keyB64);
            aes.IV = Convert.FromBase64String(ivB64);

            byte[] packet = protocolSI.Make(ProtocolSICmdType.PUBLIC_KEY, publickey);
            networkStream.Write(packet, 0, packet.Length);
        }
    }
}
