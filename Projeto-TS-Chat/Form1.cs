using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using EI.SI;

namespace Projeto_TS_Chat
{
    public partial class Form1 : Form
    {
        ProtocolSI protocolSI;
        private const int PORT = 10000;
        NetworkStream networkStream;
        TcpClient client;
        public Form1()
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

        private void buttonSair_Click(object sender, EventArgs e)
        {
            CloseClient();
            this.Close();
        }

        private void buttonEnviar_Click(object sender, EventArgs e)
        {
            // Preparar mensagem para ser enviada
            string msg = textBoxMessage.Text;
            textBoxMessage.Clear();
            byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, msg);

            // Enviar mensagem
            networkStream.Write(packet, 0, packet.Length);
            while (protocolSI.GetCmdType() != ProtocolSICmdType.ACK)
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            }
        }
        private void CloseClient()
        {
            // Preparar envio da mensagem para desligar a ligação
            byte[] eot = protocolSI.Make(ProtocolSICmdType.EOF);
            networkStream.Write(eot, 0, eot.Length);
            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            networkStream.Close();
            client.Close();
        }
    }
}
