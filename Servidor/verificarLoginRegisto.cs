using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

// Esta classe contem todas as funções que interagem com a base de dados

namespace Servidor
{
    class verificarLoginRegisto
    {
        private const int NUMBER_OF_ITERATIONS = 1000;

        // esta função faz a verificação das credencias do utilizador e palavra passe 

        public bool VerifyLogin(string username, string password, string publicKey)
        {
            SqlConnection conn = null;
            try
            {
                // Configurar ligação à Base de Dados
                conn = new SqlConnection();
                conn.ConnectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename='C:\Users\Pedro Lourenço\Documents\Aulas\PSI_TS_PL1\Servidor\Users.mdf';Integrated Security=True");

                // Abrir ligação à Base de Dados
                conn.Open();

                // Declaração do comando SQL
                String sql = "SELECT * FROM Users WHERE Username = @username";

                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = sql;
                
                // Declaração dos parâmetros do comando SQL
                SqlParameter param = new SqlParameter("@username", username);
                SqlParameter paramUpdate = new SqlParameter("@publicKey", publicKey);

                // Introduzir valor ao parâmentro registado no comando SQL
                cmd.Parameters.Add(param);
                cmd.Parameters.Add(paramUpdate);
                // Associar ligação à Base de Dados ao comando a ser executado
                cmd.Connection = conn;

                // Executar comando SQL
                SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    throw new Exception("Error while trying to access an user");
                }

                // Ler resultado da pesquisa
                reader.Read();

                // Obter Hash (password + salt)
                byte[] saltedPasswordHashStored = (byte[])reader["SaltedPasswordHash"];

                // Obter salt
                byte[] saltStored = (byte[])reader["Salt"];

                conn.Close();
                updatePublicKey(username, publicKey);
                //Verificar se a password na base de dados 
                byte[] hash = GenerateSaltedHash(password, saltStored);
                return saltedPasswordHashStored.SequenceEqual(hash);
                //throw new NotImplementedException();
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred: " + e.Message);
                return false;
            }
        }

        // atualiza a chave publica armazenada na base de dados apois login do utilizador confirmado 
        private void updatePublicKey(string username, string publicKey)
        {
            SqlConnection conn = null;
            try
            {
                // Configurar ligação à Base de Dados
                conn = new SqlConnection();
                conn.ConnectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename='C:\Users\Pedro Lourenço\Documents\Aulas\PSI_TS_PL1\Servidor\Users.mdf';Integrated Security=True");

                // Abrir ligação à Base de Dados
                conn.Open();

                // Declaração dos parâmetros do comando SQL
                SqlParameter paramUsername = new SqlParameter("@username", username);
                SqlParameter paramPublicKey = new SqlParameter("@publicKey", publicKey);

                // Declaração do comando SQL
                String sql = "UPDATE Users SET PublicKey=@publicKey WHERE Username = @username";

                // Prepara comando SQL para ser executado na Base de Dados
                SqlCommand cmd = new SqlCommand(sql, conn);

                // Introduzir valores aos parâmentros registados no comando SQL
                cmd.Parameters.Add(paramUsername);
                cmd.Parameters.Add(paramPublicKey);

                // Executar comando SQL
                int lines = cmd.ExecuteNonQuery();

                // Fechar ligação
                conn.Close();
                if (lines == 0)
                {
                    // Se forem devolvidas 0 linhas alteradas então o não foi executado com sucesso
                    throw new Exception("Error while updating a key");
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error while inserting an user:" + e.Message);
            }
        
        }

        // cria o salt para armazendar a palavra passe na base de dados
        private static byte[] GenerateSaltedHash(string plainText, byte[] salt)
        {
            Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(plainText, salt, NUMBER_OF_ITERATIONS);
            return rfc2898.GetBytes(32);
        }


        // faz o registo do utilizador na base de dados para que este consiga efetuar o login,
        // funçao armazena nome do utilizador, palavra passe, salt usado e chave publica
        public void Register(string username, byte[] saltedPasswordHash, byte[] salt, string publicKey)
        {   

            SqlConnection conn = null;
            try
            {
                // Configurar ligação à Base de Dados
                conn = new SqlConnection();
                conn.ConnectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename='C:\Users\Pedro Lourenço\Documents\Aulas\PSI_TS_PL1\Servidor\Users.mdf';Integrated Security=True");

                // Abrir ligação à Base de Dados
                conn.Open();

                // Declaração dos parâmetros do comando SQL
                SqlParameter paramUsername = new SqlParameter("@username", username);
                SqlParameter paramPassHash = new SqlParameter("@saltedPasswordHash", saltedPasswordHash);
                SqlParameter paramSalt = new SqlParameter("@salt", salt);
                SqlParameter paramPublicKey = new SqlParameter("@publicKey", publicKey);

                // Declaração do comando SQL
                String sql = "INSERT INTO Users (Username, SaltedPasswordHash, Salt, PublicKey) VALUES (@username,@saltedPasswordHash,@salt,@publicKey)";

                // Prepara comando SQL para ser executado na Base de Dados
                SqlCommand cmd = new SqlCommand(sql, conn);

                // Introduzir valores aos parâmentros registados no comando SQL
                cmd.Parameters.Add(paramUsername);
                cmd.Parameters.Add(paramPassHash);
                cmd.Parameters.Add(paramSalt);
                cmd.Parameters.Add(paramPublicKey);

                // Executar comando SQL
                int lines = cmd.ExecuteNonQuery();

                // Fechar ligação
                conn.Close();
                if (lines == 0)
                {
                    // Se forem devolvidas 0 linhas alteradas então o não foi executado com sucesso
                    throw new Exception("Error while inserting an user");
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error while inserting an user:" + e.Message);
            }
        }

        //verifica a existencia de um nome de utizador repetido e devolve "true" ou "false" caso encontre ou não
        public bool UserCheckUp(string username)
        {          

            SqlConnection conn = null;
            try
            {
                // Configurar ligação à Base de Dados
                conn = new SqlConnection();
                conn.ConnectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename='C:\Users\Pedro Lourenço\Documents\Aulas\PSI_TS_PL1\Servidor\Users.mdf';Integrated Security=True");

                // Abrir ligação à Base de Dados
                conn.Open();

                // Declaração dos parâmetros do comando SQL
                SqlParameter paramUsername = new SqlParameter("@username", username);

                // Declaração do comando SQL
                String sql = "SELECT * FROM Users WHERE Username = @username";

                // Prepara comando SQL para ser executado na Base de Dados
                SqlCommand cmd = new SqlCommand(sql, conn);

                // Introduzir valores aos parâmentros registados no comando SQL
                cmd.Parameters.Add(paramUsername);

                // Executar comando SQL
                int lines = cmd.ExecuteNonQuery();

                // Fechar ligação
                conn.Close();
                return true;

            }
            catch 
            {
                return false;

            }

        }
    }
}
