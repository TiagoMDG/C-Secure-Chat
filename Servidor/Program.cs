using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using EI.SI;
using System.Security.Cryptography;
using System.IO;

namespace Servidor
{

    class Program
    {
        private const int port = 10000;
        

        static void Main(string[] args)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            TcpListener listener = new TcpListener(endPoint);
            

            //iniciar o servidor
            listener.Start();
            Console.WriteLine("Server is ready! Super Ready!");

            int clientCounter = 0;

            while (true)
            {
                //aceitar lligaçoes
                TcpClient client = listener.AcceptTcpClient();
                clientCounter++;
                Console.WriteLine("Clientes conectados: {0}", clientCounter);
                //utilizar threads
                ClienteHandler clientHandler = new ClienteHandler(client, clientCounter);
                clientHandler.Handle();
            }
        }
    }
    public class Globals
    {
        public Byte[] privateKey;
        public Byte[] privateKeyIV;
        public string chavepublica;
        public string username;
    }
    class ClienteHandler
    {
        private TcpClient client;
        private int clientID;
        //construtor da classe

        AesCryptoServiceProvider aes;
        RSACryptoServiceProvider rsa;

        string username, password, stringSaltedPasswordHash, salt, chave, nomeFicheiro;
        
        public ClienteHandler(TcpClient client, int clientID)
        {
            this.client = client;
            this.clientID = clientID;
        }

        //funçao para tratamento da tread
        public void Handle()
        {
            Thread thread = new Thread(threadHandler);
            thread.Start();
        }
                
        private void threadHandler()
        {
            NetworkStream networkStream = this.client.GetStream();
            ProtocolSI protocoloSI = new ProtocolSI();
            Globals global = new Globals();
            aes = new AesCryptoServiceProvider();
            StreamWriter sw = new StreamWriter("ConsoleLog.txt");


            

            verificarLoginRegisto loginRegisto = new verificarLoginRegisto();

            //enquanto a tread nao receber ordem de termino
            while (protocoloSI.GetCmdType() != ProtocolSICmdType.EOT)
            {
                int bytesRead = networkStream.Read(protocoloSI.Buffer, 0, protocoloSI.Buffer.Length);
                byte[] ack;

                switch (protocoloSI.GetCmdType())
                {
                    case ProtocolSICmdType.PUBLIC_KEY:

                        string chavePublica = protocoloSI.GetStringFromData();
                        global.chavepublica = chavePublica;
                        string chavePrivada = GerarChavePrivada(chavePublica);
                        global.privateKey = Convert.FromBase64String(chavePrivada);

                        global.privateKeyIV = Convert.FromBase64String(GerarIv(chavePublica));
                        aes.Key = global.privateKey;
                        aes.IV = global.privateKeyIV;
                        string chavePrivadaCifrada = CifrarTexto(chavePrivada);
                        byte[] msg = protocoloSI.Make(ProtocolSICmdType.SECRET_KEY, chavePrivadaCifrada);
                        networkStream.Write(msg, 0, msg.Length);

                        sw.WriteLine("Chave Publica: " + chavePublica);
                        sw.WriteLine("Chave Privada: " + chavePrivada);

                        break;

                    case ProtocolSICmdType.USER_OPTION_1: //Login
                        //recupera os dados enviados pelo user no pacote
                        string msgLogin = DecifrarTexto(protocoloSI.GetStringFromData());

                        //divide os dados em strings de Nome de utilizador e password
                        string[] SplitLogin = msgLogin.Split(new Char[] { '|' });

                        username = Convert.ToString(SplitLogin[0]);
                        password = Convert.ToString(SplitLogin[1]);

                        if (loginRegisto.VerifyLogin(username, password, global.chavepublica))
                        {
                            Console.WriteLine("Utilizador " + username + " autorizado!");
                            sw.WriteLine("\nUtilizador " + username + " autorizado!");

                            ack = protocoloSI.Make(ProtocolSICmdType.ACK);
                            //envia mensagem para stream
                            networkStream.Write(ack, 0, ack.Length);
                        }
                        else
                        {
                            Console.WriteLine("ERRO!\nUtilizador: " + username + " invalido ou nao existente!");
                            sw.WriteLine("ERRO!\nUtilizador: " + username + " invalido ou nao existente!");

                            ack = protocoloSI.Make(ProtocolSICmdType.EOT);
                            //envia mensagem para stream
                            networkStream.Write(ack, 0, ack.Length);
                        }
                        break;

                    case ProtocolSICmdType.USER_OPTION_2: //registo
                        
                        string msgRegister = DecifrarTexto(protocoloSI.GetStringFromData());
                        Console.WriteLine(msgRegister);
                        string[] SplitRegister = { };
                        //fazer ciclo split
                        for (int i = 0; i < msgRegister.Length; i++)
                        {
                            SplitRegister = msgRegister.Split(new Char[] { '|' });
                        }

                        username = Convert.ToString(SplitRegister[0]);
                        stringSaltedPasswordHash = Convert.ToString(SplitRegister[1]);
                        salt = Convert.ToString(SplitRegister[2]);
                        chave = Convert.ToString(SplitRegister[3]);

                        //verifica se ja existe utilizador registado com o mesmo nome
                        if (!loginRegisto.UserCheckUp(username))
                        {
                            loginRegisto.Register(username, Convert.FromBase64String(stringSaltedPasswordHash), Convert.FromBase64String(salt), global.chavepublica);
                        }
                        else
                        {
                            Console.WriteLine("Ja existe um utilizador com esse nome");
                        }

                        ack = protocoloSI.Make(ProtocolSICmdType.ACK);
                        //envia mensagem para stream
                        networkStream.Write(ack, 0, ack.Length);
                        
                        break;

                    case ProtocolSICmdType.DATA: //mensagem normal

                        string msgRecebida = protocoloSI.GetStringFromData();

                        Console.WriteLine(username +"("+ clientID +")" + " enviou a seguinte mensagem: " + DecifrarTexto(msgRecebida));
                        string msgResposta = "Mensagem recebida pelos nossos servidores, obrigado por nos escolher.";

                        sw.WriteLine(username + ": " + DecifrarTexto(msgRecebida));
                        sw.WriteLine("Server: " + msgResposta);

                        //Enviar mensagem de confirmaçao de recepçao para o cliente
                        byte[] packet = protocoloSI.Make(ProtocolSICmdType.DATA, CifrarTexto(msgResposta));
                        
                        networkStream.Write(packet, 0, packet.Length);
                        break;

                    case ProtocolSICmdType.USER_OPTION_4://recebe nome do ficheiro

                        nomeFicheiro = protocoloSI.GetStringFromData();
                        //nomeFicheiro = DecifrarTexto(nomeFicheiroRecebido);
                        Console.WriteLine(username + "(" + clientID + ")" + " enviou a seguinte mensagem: " + DecifrarTexto(nomeFicheiro));
                        msgResposta = "Mensagem recebida pelos nossos servidores, obrigado por nos escolher.";

                        sw.WriteLine(username + ": " + DecifrarTexto(nomeFicheiro));
                        sw.WriteLine("Server: " + msgResposta);

                        //Enviar mensagem de confirmaçao de recepçao para o cliente
                        packet = protocoloSI.Make(ProtocolSICmdType.DATA, CifrarTexto(msgResposta));

                        networkStream.Write(packet, 0, packet.Length);
                        break;

                    case ProtocolSICmdType.USER_OPTION_3: //mensagem normal

                        byte[] fileReceived = protocoloSI.GetData();
                        //File.WriteAllBytes("teste", fileReceived);

                        File.WriteAllBytes(DecifrarTexto(nomeFicheiro), fileReceived);
                        Console.WriteLine(fileReceived.Length);
                        DecryptFile(DecifrarTexto(nomeFicheiro), aes.Key, aes.IV);

                        Console.WriteLine(username + "(" + clientID + ")" + " enviou um ficheiro");
                        string msgFicheiro = "Ficheiro recebido pelos nossos servidores, obrigado por nos escolher.";

                        sw.WriteLine(username + "(" + clientID + ")" + " enviou um ficheiro");

                        //Enviar mensagem de confirmaçao de recepçao para o cliente
                        byte[] packetfile = protocoloSI.Make(ProtocolSICmdType.DATA, CifrarTexto(msgFicheiro));

                        networkStream.Write(packetfile, 0, packetfile.Length);
                        break;
                        
                    case ProtocolSICmdType.EOT:
                        Console.WriteLine("Ending thread from client {0}({1})", username,clientID);
                        sw.WriteLine("Ending thread from client {0}({1})", username, clientID);
                        ack = protocoloSI.Make(ProtocolSICmdType.ACK);
                        //envia mensagem para stream
                        networkStream.Write(ack, 0, ack.Length);
                        break;
                }
            }            
            networkStream.Close();
            client.Close();
            sw.Close();

            //Environment.Exit(0);
        }


        //funções 

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

        private void DecryptFile(string inFile, byte[] privatekey, byte[] iv)
        {
            // Create instance of Aes for
            // symetric decryption of the data.
            Aes aes = Aes.Create();

            // Create byte arrays to get the length of
            // the encrypted key and IV.
            // These values were stored as 4 bytes each
            // at the beginning of the encrypted package.
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            string outFile = inFile.Substring(0, inFile.LastIndexOf(".")) + "";

            // Use FileStream objects to read the encrypted
            // file (inFs) and save the decrypted file (outFs).
            using (FileStream inFs = new FileStream(inFile, FileMode.Open))
            {

                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Read(LenK, 0, 3);
                inFs.Seek(4, SeekOrigin.Begin);
                inFs.Read(LenIV, 0, 3);

                // Convert the lengths to integer values.
                int lenK = BitConverter.ToInt32(LenK, 0);
                int lenIV = BitConverter.ToInt32(LenIV, 0);

                // Determine the start postition of
                // the ciphter text (startC)
                // and its length(lenC).
                int startC = lenK + lenIV + 8;
                int lenC = (int)inFs.Length - startC;

                // Create the byte arrays for
                // the encrypted Aes key,
                // the IV, and the cipher text.
                byte[] KeyEncrypted = new byte[lenK];
                byte[] IV = new byte[lenIV];

                // Extract the key and IV
                // starting from index 8
                // after the length values.
                inFs.Seek(8, SeekOrigin.Begin);
                inFs.Read(KeyEncrypted, 0, lenK);
                inFs.Seek(8 + lenK, SeekOrigin.Begin);
                inFs.Read(IV, 0, lenIV);
                //Directory.CreateDirectory(DecrFolder);
                // Use RSACryptoServiceProvider
                // to decrypt the AES key.
                rsa = new RSACryptoServiceProvider();
                byte[] KeyDecrypted = rsa.Decrypt(KeyEncrypted, false);

                // Decrypt the key.
                ICryptoTransform transform = aes.CreateDecryptor(KeyDecrypted, IV);

                // Decrypt the cipher text from
                // from the FileSteam of the encrypted
                // file (inFs) into the FileStream
                // for the decrypted file (outFs).
                using (FileStream outFs = new FileStream(outFile, FileMode.Create))
                {

                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size.
                    int blockSizeBytes = aes.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];

                    // By decrypting a chunk a time,
                    // you can save memory and
                    // accommodate large files.

                    // Start at the beginning
                    // of the cipher text.
                    inFs.Seek(startC, SeekOrigin.Begin);
                    using (CryptoStream outStreamDecrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamDecrypted.Write(data, 0, count);
                        }
                        while (count > 0);

                        outStreamDecrypted.FlushFinalBlock();
                        outStreamDecrypted.Close();
                    }
                    outFs.Close();
                }
                inFs.Close();
            }
        }



    }
}
