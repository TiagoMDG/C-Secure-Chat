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
    }
    class ClienteHandler
    {
        private TcpClient client;
        private int clientID;
        //construtor da classe

      
        AesCryptoServiceProvider aes;

        

        string username, stringSaltedPasswordHash, salt, chave, hash, assinatura;

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
            ChaveSimetrica cs = new ChaveSimetrica();
            Globals global = new Globals();
            aes = new AesCryptoServiceProvider();
            

            string username,password, stringSaltedPasswordHash, salt, chave, hash, assinatura;

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
                        string chavePrivada = cs.GerarPrivada(chavePublica);
                        
                        global.privateKey = Convert.FromBase64String(chavePrivada);

                        global.privateKeyIV = Convert.FromBase64String(cs.GerarIv(chavePublica));
                        aes.Key = global.privateKey;
                        aes.IV = global.privateKeyIV;
                        string chavePrivadaCifrada = cs.CifrarPrivada(chavePrivada);
                        byte[] msg = protocoloSI.Make(ProtocolSICmdType.SECRET_KEY, chavePrivadaCifrada);
                        networkStream.Write(msg, 0, msg.Length);

                        Console.WriteLine("A chave Privada: " + Convert.ToBase64String(global.privateKey)+ " bits convertidos key: "+ global.privateKey.Length + " bits convertidos IV: " + global.privateKeyIV.Length);
                        break;

                    case ProtocolSICmdType.USER_OPTION_1: //Login
                        //recupera os dados enviados pelo user no pacote
                        string msgLogin = DecifrarTexto(protocoloSI.GetStringFromData());
                        Console.WriteLine("mensagem para login: " + msgLogin);

                        //divide os dados em strings de Nome de utilizador e password
                        string[] SplitLogin = msgLogin.Split(new Char[] { '|' });

                        username = Convert.ToString(SplitLogin[0]);
                        password = Convert.ToString(SplitLogin[1]);

                        if (loginRegisto.VerifyLogin(username, password))
                        {
                            Console.WriteLine("Utilizador: " + username + " autorizado!");
                        }
                        else
                        {
                            Console.WriteLine("ERRO!\nUtilizador: " + username + " invalido ou nao existente!");
                        }
                        ack = protocoloSI.Make(ProtocolSICmdType.ACK);
                        //envia mensagem para stream
                        networkStream.Write(ack, 0, ack.Length);
                        break;

                    case ProtocolSICmdType.USER_OPTION_2: //registo
                        
                        string msgRegister = DecifrarTexto(protocoloSI.GetStringFromData());
                        Console.WriteLine(msgRegister);
                        string[] SplitRegister = { };

                        for (int i = 0; i < msgRegister.Length; i++)
                        {
                            SplitRegister = msgRegister.Split(new Char[] { '|' });
                        }
                        

                        //fazer ciclo split

                        username = Convert.ToString(SplitRegister[0]);
                        stringSaltedPasswordHash = Convert.ToString(SplitRegister[1]);
                        salt = Convert.ToString(SplitRegister[2]);
                        chave = Convert.ToString(SplitRegister[3]);

                        loginRegisto.Register(username, Convert.FromBase64String(stringSaltedPasswordHash), Convert.FromBase64String(salt));

                        ack = protocoloSI.Make(ProtocolSICmdType.ACK);
                        //envia mensagem para stream
                        networkStream.Write(ack, 0, ack.Length);
                        
                        break;

                    case ProtocolSICmdType.DATA: //mensagem normal

                        string mensagemCliente = protocoloSI.GetStringFromData();


                        Console.WriteLine("Recebi a seguinte mensagem do cliente (username e password cifrada): " + clientID + ": " + protocoloSI.GetStringFromData());
                        string msgRecebida = protocoloSI.GetStringFromData();

                        //username + password
                        Console.WriteLine("Username+Password decifrada: " + clientID + ": " + DecifrarTexto(msgRecebida));
                        string username_password = DecifrarTexto(msgRecebida);

                        //string[] words = username_password.Split('+');

                        //foreach (var word in words)
                        //{
                        //    System.Console.WriteLine($"<{word}>");
                        //}
                        //String username = words[0];
                        //String password = words[1];

                        //Console.WriteLine("Username: " + username);
                        //Console.WriteLine("Password: " + password);

                        ////Registar em BD
                        //byte[] salt = GenerateSalt(SALTSIZE);
                        //byte[] hash = GenerateSaltedHash(password, salt);
                        //Register(username, hash, salt);

                        ack = Encoding.UTF8.GetBytes(DecifrarTexto(msgRecebida));
                        //envia a mensagem para o stream
                        networkStream.Write(ack, 0, ack.Length);
                        
                        break;



                    case ProtocolSICmdType.EOT:
                        Console.WriteLine("Ending thread from client {0}" + clientID);
                        ack = protocoloSI.Make(ProtocolSICmdType.ACK);
                        //envia mensagem para stream
                        networkStream.Write(ack, 0, ack.Length);
                        break;
                }
            }            
            networkStream.Close();
            client.Close();            
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
    }
}
