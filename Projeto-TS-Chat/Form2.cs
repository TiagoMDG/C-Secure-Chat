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

namespace Projeto_TS_Chat
{
    public partial class Form2 : Form
    {
        //protoclo Si e socket de comunicaçao com o servidor
        ProtocolSI protocolSI;
        private const int PORT = 10000;
        NetworkStream networkStream;
        TcpClient client;

        //constantes par geral as pass's com salt
        private const int SALTSIZE = 8;
        private const int NUMBER_OF_ITERATIONS = 1000;
        
        public Form2()
        {
            InitializeComponent();

            //iniciar o componente de cominicaçao de cliente
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, PORT);
            client = new TcpClient();
            client.Connect(endPoint);

            //Obter um fluxo de clientes para leitura e escrita
            networkStream = client.GetStream();

            // Preparação da comunicação utilizando a classe desenvolvida pelo DEI
            protocolSI = new ProtocolSI();
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            String password = textBoxPwLogin.Text;
            String username = textBoxUserLogin.Text;

            /* if (VerifyLogin(username, password))
            {
                MessageBox.Show("O utilizador está válido");
            }
            else
            {
                MessageBox.Show("Login inválido");
            } */

            this.Hide();
            Form1 f1 = new Form1();
            f1.ShowDialog();
            this.Close();
        }
        private void buttonEnviar_Click(object sender, EventArgs e)
        {
            // Preparar mensagem para ser enviada
            //string msg = textBoxMsg.Text;
            //textBoxMsg.Clear();
            

           
        }
        private void buttonRegistar_Click(object sender, EventArgs e)
        {
            String pass = textBoxPwRegistar.Text;
            String username = textBoxUserRegistar.Text;

            byte[] salt = GenerateSalt(SALTSIZE);
            byte[] hash = GenerateSaltedHash(pass, salt);

            Register(username, hash, salt);

            this.Hide();
            Form1 f1 = new Form1();
            f1.ShowDialog();
            this.Close();
        }

        private void Register(string user, byte[] passHash, byte[] passSalt)
        {
            //registar opçao SI USER_OPTION_2
            //converter encriptaçao para string para ser enviado
            string passwordHash = Convert.ToBase64String(passHash);
            string passwordSalt = Convert.ToBase64String(passSalt);

            //mensagem a enviar
            string[] msg = new string[3] { user, passwordHash, passwordSalt };

            //pacote a enviar pelo protocolo SI
            byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2, user);

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
    }
}
