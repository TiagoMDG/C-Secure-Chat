
namespace Projeto_TS_Chat
{
    partial class FormChatBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBoxMessage = new System.Windows.Forms.TextBox();
            this.buttonEnviar = new System.Windows.Forms.Button();
            this.buttonUpFile = new System.Windows.Forms.Button();
            this.buttonSair = new System.Windows.Forms.Button();
            this.messageChat = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // textBoxMessage
            // 
            this.textBoxMessage.Location = new System.Drawing.Point(13, 384);
            this.textBoxMessage.Multiline = true;
            this.textBoxMessage.Name = "textBoxMessage";
            this.textBoxMessage.Size = new System.Drawing.Size(413, 43);
            this.textBoxMessage.TabIndex = 1;
            // 
            // buttonEnviar
            // 
            this.buttonEnviar.Location = new System.Drawing.Point(338, 439);
            this.buttonEnviar.Name = "buttonEnviar";
            this.buttonEnviar.Size = new System.Drawing.Size(88, 43);
            this.buttonEnviar.TabIndex = 2;
            this.buttonEnviar.Text = "Enviar";
            this.buttonEnviar.UseVisualStyleBackColor = true;
            this.buttonEnviar.Click += new System.EventHandler(this.buttonEnviar_Click);
            // 
            // buttonUpFile
            // 
            this.buttonUpFile.Location = new System.Drawing.Point(244, 439);
            this.buttonUpFile.Name = "buttonUpFile";
            this.buttonUpFile.Size = new System.Drawing.Size(88, 43);
            this.buttonUpFile.TabIndex = 3;
            this.buttonUpFile.Text = "Carregar Ficheiro";
            this.buttonUpFile.UseVisualStyleBackColor = true;
            // 
            // buttonSair
            // 
            this.buttonSair.Location = new System.Drawing.Point(13, 439);
            this.buttonSair.Name = "buttonSair";
            this.buttonSair.Size = new System.Drawing.Size(88, 43);
            this.buttonSair.TabIndex = 4;
            this.buttonSair.Text = "Sair";
            this.buttonSair.UseVisualStyleBackColor = true;
            this.buttonSair.Click += new System.EventHandler(this.buttonSair_Click);
            // 
            // messageChat
            // 
            this.messageChat.FormattingEnabled = true;
            this.messageChat.Location = new System.Drawing.Point(12, 12);
            this.messageChat.Name = "messageChat";
            this.messageChat.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.messageChat.Size = new System.Drawing.Size(413, 355);
            this.messageChat.TabIndex = 5;
            // 
            // FormChatBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(439, 494);
            this.Controls.Add(this.messageChat);
            this.Controls.Add(this.buttonSair);
            this.Controls.Add(this.buttonUpFile);
            this.Controls.Add(this.buttonEnviar);
            this.Controls.Add(this.textBoxMessage);
            this.Name = "FormChatBox";
            this.Text = "Cliente Chat";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox textBoxMessage;
        private System.Windows.Forms.Button buttonEnviar;
        private System.Windows.Forms.Button buttonUpFile;
        private System.Windows.Forms.Button buttonSair;
        private System.Windows.Forms.ListBox messageChat;
    }
}

