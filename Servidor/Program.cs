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
                //aceitar ligaçoes
                TcpClient client = listener.AcceptTcpClient();
                clientCounter++;
                Console.WriteLine("Clientes conectados: {0}", clientCounter);
                //utilizar threads
                ClienteHandler clientHandler = new ClienteHandler(client, clientCounter);
                clientHandler.Handle();
            }
        }
    }
    public class Globals //armazena variaveis glibais para serem usadas nas varias funções do servidor
    {
        public Byte[] privateKey;
        public Byte[] privateKeyIV;
        public string chavepublica;
        public string username;
    }

    class ClienteHandler //classe que trata de todas as operações de interação com o cliente
    {
        private TcpClient client;
        private int clientID;
        

        AesCryptoServiceProvider aes;
        RSACryptoServiceProvider rsa;

        string username, password, stringSaltedPasswordHash, salt, chave, nomeFicheiro; //variaveis que iram ser usadas nas várias cominicações
        
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

            StreamWriter sw = new StreamWriter("ConsoleLog.txt");//inicia o processo de escrita para o ficheiro ConsoleLog.txt

            verificarLoginRegisto loginRegisto = new verificarLoginRegisto(); //inicialização da classe que contem as funçoes de login e registo

            //enquanto a tread nao receber ordem de termino
            while (protocoloSI.GetCmdType() != ProtocolSICmdType.EOT)
            {
                int bytesRead = networkStream.Read(protocoloSI.Buffer, 0, protocoloSI.Buffer.Length);
                byte[] ack;

                //faz a diferenciação entre os varios tipos de pacotes enviados atraves do protocolo SI do cliente para o servidor
                switch (protocoloSI.GetCmdType())
                {
                    case ProtocolSICmdType.PUBLIC_KEY: //recebe e trata a chave publica do cliente

                        string chavePublica = protocoloSI.GetStringFromData();
                        global.chavepublica = chavePublica;

                        string chavePrivada = GerarChavePrivada(chavePublica);// gera a chave privada com base na chave publica enviada pelo cliente
                        global.privateKey = Convert.FromBase64String(chavePrivada);//converte a chave privada para uma variável tipo byte

                        global.privateKeyIV = Convert.FromBase64String(GerarIv(chavePublica));//gera o vetor de inicialização a ser usado nos processos de decifragem
                        
                        aes.Key = global.privateKey;//atribui, ao aes, a chave privada 
                        aes.IV = global.privateKeyIV;//atribui, ao aes, o vetor de inicialização

                        //envia a chave privada encriptada para o cliente
                        string chavePrivadaCifrada = CifrarTexto(chavePrivada);
                        byte[] msg = protocoloSI.Make(ProtocolSICmdType.SECRET_KEY, chavePrivadaCifrada);
                        networkStream.Write(msg, 0, msg.Length);

                        //escreve para o ficheiro ConsoleLog.txt as chaves 
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Chave Publica: " + chavePublica);
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Chave Privada: " + chavePrivada);

                        break;

                    case ProtocolSICmdType.USER_OPTION_1: //Login

                        //recupera os dados enviados pelo user no pacote
                        string msgLogin = DecifrarTexto(protocoloSI.GetStringFromData());

                        //divide os dados em strings, usando um caracter pré-definido, Nome de utilizador e password
                        string[] SplitLogin = msgLogin.Split(new Char[] { '|' });

                        username = Convert.ToString(SplitLogin[0]);
                        password = Convert.ToString(SplitLogin[1]);

                        //chama a funçao de verificação de login,
                        //se bem sucedido deixa o utilizador entrar no sistema se não termina a comunicação
                        if (loginRegisto.VerifyLogin(username, password, global.chavepublica))
                        {
                            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Utilizador " + username + " autorizado!");
                            sw.WriteLine("\n" + DateTime.Now.ToString("HH:mm:ss") + " Utilizador " + username + " autorizado!");//escreve para log o resultado da tentativa de login

                            //envia mensagem para o cliente a acusar login bem sucedido
                            ack = protocoloSI.Make(ProtocolSICmdType.ACK);                            
                            networkStream.Write(ack, 0, ack.Length);
                        }
                        else
                        {
                            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " ERRO! Utilizador " + username + " invalido ou nao existente!");
                            sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " ERRO! Utilizador " + username + " invalido ou nao existente!");//escreve para log o resultado da tentativa de login

                            //envia mensagem para o cliente a terminar a ligação
                            ack = protocoloSI.Make(ProtocolSICmdType.EOT);
                            networkStream.Write(ack, 0, ack.Length);
                        }
                        break;

                    case ProtocolSICmdType.USER_OPTION_2: //Registo

                        //recupera e decifra a nensagem enviada pelo cliente com as credenciais para registo
                        string msgRegister = DecifrarTexto(protocoloSI.GetStringFromData());

                        //divide os dados em strings, usando um caracter pré-definido,
                        //Nome de utilizador, password, salt e chave publica
                        string[] SplitRegister = { };                        
                        for (int i = 0; i < msgRegister.Length; i++)
                        {
                            SplitRegister = msgRegister.Split(new Char[] { '|' });
                        }
                        username = Convert.ToString(SplitRegister[0]);
                        stringSaltedPasswordHash = Convert.ToString(SplitRegister[1]);
                        salt = Convert.ToString(SplitRegister[2]);
                        chave = Convert.ToString(SplitRegister[3]);

                        //chama a função de registo de utilizadores na base de dados
                        loginRegisto.Register(username, Convert.FromBase64String(stringSaltedPasswordHash), Convert.FromBase64String(salt), global.chavepublica);

                        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + username + " registado com sucesso!");
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + username + " registado com sucesso!");

                        //envia mensagem para o cliente em como o registo foi bem sucedido
                        ack = protocoloSI.Make(ProtocolSICmdType.ACK);
                        networkStream.Write(ack, 0, ack.Length);
                        
                        break;

                    case ProtocolSICmdType.DATA: //mensagem de texto

                        //recupera e decifra a nensagem de texto enviada pelo cliente
                        string msgRecebida = DecifrarTexto(protocoloSI.GetStringFromData());

                        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + username +"("+ clientID +")" + " enviou a seguinte mensagem: " + msgRecebida);
                        //Mensagem a enviar para o cliente apos recepção de mensagem de texto
                        string msgResposta = "Mensagem recebida pelos nossos servidores, obrigado por nos escolher.";

                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + username + ": " + msgRecebida);
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Server: " + msgResposta);

                        //Envia mensagem de confirmaçao de recepçao, cifrada, para o cliente
                        byte[] packet = protocoloSI.Make(ProtocolSICmdType.DATA, CifrarTexto(msgResposta));                        
                        networkStream.Write(packet, 0, packet.Length);
                        break;

                    case ProtocolSICmdType.USER_OPTION_4://recebe nome do ficheiro

                        //recebe o nome do ficheiro
                        nomeFicheiro = protocoloSI.GetStringFromData();
                        //esccreve na consola do servidor o nome do ficheiro e a informção de quem o enviou
                        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + username + "(" + clientID + ")" + " enviou a seguinte mensagem: " + DecifrarTexto(nomeFicheiro));
                        
                        //Enviar mensagem de confirmaçao de recepçao do nome do fichiero para o cliente
                        ack = protocoloSI.Make(ProtocolSICmdType.ACK);
                        networkStream.Write(ack, 0, ack.Length);
                        break;

                    case ProtocolSICmdType.USER_OPTION_3: //recebe o ficheiro

                        //recupera o ficheiro enviado pelo cliente 
                        byte[] fileReceived = protocoloSI.GetData();

                        //escreve na pasta do servidor o ficheiro com o nome enviado pelo cliente e recebido na USER_OPTION_4
                        File.WriteAllBytes(DecifrarTexto(nomeFicheiro), fileReceived);
                        //Mensagem a enviar para o cliente apos recepção de ficheiros
                        msgResposta = "Ficheiro recebido pelos nossos servidores, obrigado por nos escolher.";

                        //escreve no ficheiro de log o nome de ficheiro enviado pelo cliente e a resposta que o servidor enviou
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + username + ": " + DecifrarTexto(nomeFicheiro));
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Server: " + msgResposta);

                        //Enviar mensagem de confirmaçao de recepçao para o cliente
                        packet = protocoloSI.Make(ProtocolSICmdType.DATA, CifrarTexto(msgResposta));
                        networkStream.Write(packet, 0, packet.Length);
                        break;
                        
                    case ProtocolSICmdType.EOT:// recebe este protcolo quando o clinte encerra comunicações
                        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Ending thread from client {0}({1})", username,clientID);
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Ending thread from client {0}({1})", username, clientID);
                        ack = protocoloSI.Make(ProtocolSICmdType.ACK);
                        //envia mensagem para stream
                        networkStream.Write(ack, 0, ack.Length);
                        break;
                }
            }
            //fecha todas as comunicaçoes com o cliente e encerra o processo de escrita para o ficheiro de log do servidor
            networkStream.Close();
            client.Close();
            sw.Close();
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

        //funçao responsavel por cifrar mensagens enviadas para o cliente
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

        //funçao responsavel por decifrar mensagens de texto enviadas pelo cliente
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
