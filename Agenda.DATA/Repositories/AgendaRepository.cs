using Agenda.DATA.Models;
using JWT.Controllers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;

namespace Agenda.DATA.Repositories
{
    public class AgendaRepository
    {
        private string _ConnAgenda = "";
        private string _bdAgenda = "";

        private IConfiguration Configuration;

        #region CONSTRUTOR
        public AgendaRepository(IConfiguration Configuration)
        {
            this.Configuration = Configuration;
            _ConnAgenda = Configuration.GetValue<string>("Conn_AgendaServicos");
            _bdAgenda = Configuration.GetValue<string>("AgendaServicos");
        }
        #endregion

        public string InsereLog(object valores, int usuCod, int tipLogCod, DateTime dataHoraTransacao)
        {
            string ret = "";
            try
            {
                StringWriter sw = new StringWriter();
                XmlTextWriter xw = new XmlTextWriter(sw);
                XmlDocument xmlDocument = JsonConvert.DeserializeXmlNode(JsonConvert.SerializeObject(valores), "Valores");
                xmlDocument.WriteTo(xw);
                string objXML = sw.ToString() != "<Valores />" ? sw.ToString() : null;

                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        //DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@logDesc", objXML);
                        command.Parameters.AddWithValue("@logDataHora", dataHoraTransacao);
                        command.Parameters.AddWithValue("@usuCod", usuCod);
                        command.Parameters.AddWithValue("@tipLogCod", tipLogCod);

                        command.CommandText = @"
                                                    INSERT INTO " + _bdAgenda + @".dbo.LogSis
                                                    (logDesc, logDataHora, usuCod, tipLogCod)
                                                    VALUES(
                                                    @logDesc,
                                                    @logDataHora,
                                                    @usuCod,
                                                    @tipLogCod
                                                    );
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string Teste(string teste)
        {
            return teste += "...";
        }

        public UsuarioModel Login(LoginModel objLogin)
        {
            UsuarioModel objUsuarioRetorno = new UsuarioModel();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;

                        //-> Validando Login
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@usuLogin", objLogin.usuLogin);
                        command.Parameters.AddWithValue("@usuSenha", objLogin.usuSenha);
                        command.CommandText = @"
                                                SELECT usuCod, usuNome, usuLogin, usuSenha, usuStatus, perfCod
                                                FROM " + _bdAgenda + @".dbo.Usuario WITH(NOLOCK)
                                                WHERE usuLogin = @usuLogin 
                                                AND usuSenha = @usuSenha;
                                            ";
                        DataTable dt = new DataTable();
                        SqlDataReader reader;
                        reader = command.ExecuteReader();
                        dt.Load(reader);
                        if (dt.Rows.Count > 0)
                        {
                            //-> Se existir, faz insere na tabela de Log.
                            object obj = JObject.Parse("{\n    \"Login\":\"" + objLogin.usuLogin + "\"\n}");
                            InsereLog(obj, int.Parse(dt.Rows[0].ItemArray[0].ToString()), 1, dataHoraTransacao); //-> Tipo de Log de Acesso = 1.
                        }
                        else
                        {
                            objUsuarioRetorno.MensagemErro = "Usuário ou senha inválidos.";
                            return objUsuarioRetorno;
                        }

                        objUsuarioRetorno.usuCod = int.Parse(dt.Rows[0].ItemArray[0].ToString());
                        objUsuarioRetorno.usuNome = dt.Rows[0].ItemArray[1].ToString();
                        objUsuarioRetorno.usuLogin = dt.Rows[0].ItemArray[2].ToString();
                        objUsuarioRetorno.usuSenha = "";
                        objUsuarioRetorno.usuStatus = dt.Rows[0].ItemArray[4].ToString() == "True" ? true : false;
                        objUsuarioRetorno.perfCod = int.Parse(dt.Rows[0].ItemArray[5].ToString());
                        objUsuarioRetorno.MensagemErro = "OK";

                        //-> Se tudo der certo até aqui, gera um token de acesso.
                        AutenticacaoController jwtAutenticacao = new AutenticacaoController();
                        var Token = jwtAutenticacao.GerarToken(JsonConvert.SerializeObject(objUsuarioRetorno));
                        objUsuarioRetorno.tokenAcesso = Token.ToString().Substring(10);
                        objUsuarioRetorno.tokenAcesso = objUsuarioRetorno.tokenAcesso.Substring(0, objUsuarioRetorno.tokenAcesso.Length - 2);

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        objUsuarioRetorno.MensagemErro = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                objUsuarioRetorno.MensagemErro = ex.Message;
            }

            return objUsuarioRetorno;
        }

        #region MANTER INFORMAÇÕES
        public string ManterVeiculo(VeiculoModel objVeiculo, int usuCod)
        {
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@veicCod", objVeiculo.veicCod);
                        command.Parameters.AddWithValue("@veicMarca", objVeiculo.veicMarca);
                        command.Parameters.AddWithValue("@veicModelo", objVeiculo.veicModelo);
                        command.Parameters.AddWithValue("@veicAno", objVeiculo.veicAno);
                        command.Parameters.AddWithValue("@veicPlaca", objVeiculo.veicPlaca.ToUpper());
                        command.Parameters.AddWithValue("@veicObse", objVeiculo.veicObse);
                        command.Parameters.AddWithValue("@veicStatus", objVeiculo.veicStatus);
                        command.Parameters.AddWithValue("@tipVeicCod", objVeiculo.tipVeicCod);

                        //-> Se tiver id, faz Update:
                        if (objVeiculo.veicCod > 0)
                        {
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.Veiculo
                                                        SET 
                                                            veicMarca=@veicMarca, 
                                                            veicModelo=@veicModelo, 
                                                            veicAno=@veicAno, 
                                                            veicPlaca=@veicPlaca, 
                                                            veicObse=@veicObse, 
                                                            veicStatus=@veicStatus, 
                                                            tipVeicCod=@tipVeicCod
                                                        WHERE veicCod=@veicCod;
                                                    ";
                            command.ExecuteNonQuery();
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.Veiculo
                                                        (veicMarca, veicModelo, veicAno, veicPlaca, veicObse, veicStatus, tipVeicCod)
                                                        VALUES(
                                                            @veicMarca,
                                                            @veicModelo,
                                                            @veicAno,
                                                            @veicPlaca,
                                                            @veicObse,
                                                            @veicStatus,
                                                            @tipVeicCod
                                                        );
                                                    ";
                            command.ExecuteNonQuery();
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        InsereLog(objVeiculo, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string AlteraStatusVeiculo(int veicCod, bool veicStatus, int usuCod)
        {
            veicStatus = !veicStatus;
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@veicCod", veicCod);
                        command.Parameters.AddWithValue("@veicStatus", veicStatus);

                        command.CommandText = @"
                                                    UPDATE " + _bdAgenda + @".dbo.Veiculo
                                                    SET 
                                                        veicStatus=@veicStatus
                                                    WHERE veicCod=@veicCod;
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        object obj = JObject.Parse("{\n    \"veicCod\":\"" + veicCod.ToString() + "\",\n    \"veicStatus\": \"" + veicStatus.ToString() + "\"\n}");
                        InsereLog(obj, usuCod, 2, dataHoraTransacao); //-> Tipo de Log de Transação = 2.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string AlteraStatusMaquina(int maqCod, bool maqStatus, int usuCod)
        {
            maqStatus = !maqStatus;
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@maqCod", maqCod);
                        command.Parameters.AddWithValue("@maqStatus", maqStatus);

                        command.CommandText = @"
                                                    UPDATE " + _bdAgenda + @".dbo.Maquina
                                                    SET 
                                                        maqStatus=@maqStatus
                                                    WHERE maqCod=@maqCod;
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        object obj = JObject.Parse("{\n    \"maqCod\":\"" + maqCod.ToString() + "\",\n    \"maqStatus\": \"" + maqStatus.ToString() + "\"\n}");
                        InsereLog(obj, usuCod, 2, dataHoraTransacao); //-> Tipo de Log de Transação = 2.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string AlteraStatusEquipe(int equipCod, bool equipStatus, int usuCod)
        {
            equipStatus = !equipStatus;
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@equipCod", equipCod);
                        command.Parameters.AddWithValue("@equipStatus", equipStatus);

                        command.CommandText = @"
                                                    UPDATE " + _bdAgenda + @".dbo.Equipe
                                                    SET 
                                                        equipStatus=@equipStatus
                                                    WHERE equipCod=@equipCod;
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        object obj = JObject.Parse("{\n    \"equipCod\":\"" + equipCod.ToString() + "\",\n    \"equipStatus\": \"" + equipStatus.ToString() + "\"\n}");
                        InsereLog(obj, usuCod, 2, dataHoraTransacao); //-> Tipo de Log de Transação = 2.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string AlteraStatusItemCheckList(int itmChLsCod, bool itmChLsStatus, int usuCod)
        {
            itmChLsStatus = !itmChLsStatus;
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@itmChLsCod", itmChLsCod);
                        command.Parameters.AddWithValue("@itmChLsStatus", itmChLsStatus);

                        command.CommandText = @"
                                                    UPDATE " + _bdAgenda + @".dbo.ItemCheckList
                                                    SET 
                                                    itmChLsStatus=@itmChLsStatus
                                                    WHERE itmChLsCod=@itmChLsCod;
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        object obj = JObject.Parse("{\n    \"itmChLsCod\":\"" + itmChLsCod.ToString() + "\",\n    \"itmChLsStatus\": \"" + itmChLsStatus.ToString() + "\"\n}");
                        InsereLog(obj, usuCod, 2, dataHoraTransacao); //-> Tipo de Log de Transação = 2.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string AlteraStatusUsuario(int usuCod, bool usuStatus, int usuCodEnv)
        {
            usuStatus = !usuStatus;
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@usuCod", usuCod);
                        command.Parameters.AddWithValue("@usuStatus", usuStatus);

                        command.CommandText = @"
                                                    UPDATE " + _bdAgenda + @".dbo.Usuario
                                                    SET 
                                                        usuStatus=@usuStatus
                                                    WHERE usuCod=@usuCod;
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        object obj = JObject.Parse("{\n    \"usuCod\":\"" + usuCod.ToString() + "\",\n    \"usuStatus\": \"" + usuStatus.ToString() + "\"\n}");
                        InsereLog(obj, usuCodEnv, 2, dataHoraTransacao); //-> Tipo de Log de Transação = 2.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string AlteraStatusCheckList(int chLsCod, bool chLsStatus, int usuCod)
        {
            chLsStatus = !chLsStatus;
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@chLsCod", chLsCod);
                        command.Parameters.AddWithValue("@chLsStatus", chLsStatus);

                        command.CommandText = @"
                                                    UPDATE " + _bdAgenda + @".dbo.CheckList
                                                    SET 
                                                        chLsStatus=@chLsStatus
                                                    WHERE chLsCod=@chLsCod;
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        object obj = JObject.Parse("{\n    \"chLsCod\":\"" + chLsCod.ToString() + "\",\n    \"chLsStatus\": \"" + chLsStatus.ToString() + "\"\n}");
                        InsereLog(obj, usuCod, 2, dataHoraTransacao); //-> Tipo de Log de Transação = 2.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string ManterMaquina(MaquinaModel objMaquina, int usuCod)
        {
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@maqCod", objMaquina.maqCod);
                        command.Parameters.AddWithValue("@maqMarca", objMaquina.maqMarca);
                        command.Parameters.AddWithValue("@maqModelo", objMaquina.maqModelo);
                        command.Parameters.AddWithValue("@maqObse", objMaquina.maqObse);
                        command.Parameters.AddWithValue("@maqStatus", objMaquina.maqStatus);
                        command.Parameters.AddWithValue("@diamCod", objMaquina.diamCod);
                        command.Parameters.AddWithValue("@veicCod", objMaquina.veicCod);

                        //-> Se tiver id, faz Update:
                        if (objMaquina.maqCod > 0)
                        {
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.Maquina
                                                        SET 
                                                        maqMarca=@maqMarca, 
                                                        maqModelo=@maqModelo, 
                                                        maqObse=@maqObse, 
                                                        maqStatus=@maqStatus, 
                                                        diamCod=@diamCod, 
                                                        veicCod=@veicCod
                                                        WHERE maqCod=@maqCod;
                                                    ";
                            command.ExecuteNonQuery();
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.Maquina
                                                        (maqMarca, maqModelo, maqObse, maqStatus, diamCod, veicCod)
                                                        VALUES(
                                                        @maqMarca, 
                                                        @maqModelo, 
                                                        @maqObse, 
                                                        @maqStatus, 
                                                        @diamCod, 
                                                        @veicCod
                                                        );
                                                    ";
                            command.ExecuteNonQuery();
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        InsereLog(objMaquina, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string ManterEquipe(UsuarioEnvioModel objEquipe, int usuCod)
        {
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@equipCod", objEquipe.objEnvioEquipe.equipCod);
                        command.Parameters.AddWithValue("@equipDesc", objEquipe.objEnvioEquipe.equipDesc);
                        command.Parameters.AddWithValue("@maqCod", objEquipe.objEnvioEquipe.maqCod);
                        command.Parameters.AddWithValue("@apNavCod", objEquipe.objEnvioEquipe.apNavCod);
                        command.Parameters.AddWithValue("@equipStatus", objEquipe.objEnvioEquipe.equipStatus);

                        //-> Se tiver id, faz Update:
                        if (objEquipe.objEnvioEquipe.equipCod > 0)
                        {
                            //-> Altera primeiro a equipe (Equipe)
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.Equipe
                                                        SET equipDesc=@equipDesc, 
                                                        equipStatus=@equipStatus, 
                                                        apNavCod=@apNavCod, 
                                                        maqCod=@maqCod
                                                        WHERE equipCod=@equipCod;
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Apaga os integrantes que estavam relacionados (UsuarioEquipe)
                            command.CommandText = @"
                                                        DELETE FROM " + _bdAgenda + @".dbo.UsuarioEquipe
                                                        WHERE equipCod=@equipCod;
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Insere os novos usuários na tabela de relacionamento (UsuarioEquipe)
                            foreach (var itmUsr in objEquipe.objEnvioListaUsuario)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@equipCod", objEquipe.objEnvioEquipe.equipCod);
                                command.Parameters.AddWithValue("@usuCod", itmUsr.usuCod);
                                command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.UsuarioEquipe
                                                        (equipCod, usuCod)
                                                        VALUES(
                                                        @equipCod, 
                                                        @usuCod
                                                        );
                                                    ";
                                command.ExecuteNonQuery();
                            }
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.Equipe
                                                        (equipDesc, equipStatus, apNavCod, maqCod)
                                                        VALUES(
                                                        @equipDesc, 
                                                        @equipStatus, 
                                                        @apNavCod, 
                                                        @maqCod
                                                        );
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Retornando o Id que foi inserido da equipe.
                            command.CommandText = @"
                                                        SELECT MAX(equipCod) FROM " + _bdAgenda + @".dbo.Equipe WITH(NOLOCK);
                                                    ";
                            int lastEquipCod = 0;
                            DataTable dt = new DataTable();
                            SqlDataReader reader;
                            reader = command.ExecuteReader();
                            dt.Load(reader);
                            if (dt.Rows.Count > 0)
                            {
                                lastEquipCod = int.Parse(dt.Rows[0].ItemArray[0].ToString());
                            }

                            if (lastEquipCod > 0)
                            {
                                //-> Insere os novos usuários na tabela de relacionamento (UsuarioEquipe)
                                foreach (var itmUsr in objEquipe.objEnvioListaUsuario)
                                {
                                    command.Parameters.Clear();
                                    command.Parameters.AddWithValue("@equipCod", lastEquipCod);
                                    command.Parameters.AddWithValue("@usuCod", itmUsr.usuCod);
                                    command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.UsuarioEquipe
                                                        (equipCod, usuCod)
                                                        VALUES(
                                                        @equipCod, 
                                                        @usuCod
                                                        );
                                                    ";
                                    command.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                ret = "falha ao realizar a operação.";
                                transaction.Rollback();
                                connection.Close();
                            }
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        InsereLog(objEquipe, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string ManterUsuario(UsuarioTbModel objUsuario, int usuCod)
        {
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@usuCod", objUsuario.usuCod);
                        command.Parameters.AddWithValue("@usuNome", objUsuario.usuNome);
                        command.Parameters.AddWithValue("@usuLogin", objUsuario.usuLogin);
                        command.Parameters.AddWithValue("@usuSenha", objUsuario.usuSenha);
                        command.Parameters.AddWithValue("@usuStatus", objUsuario.usuStatus);
                        command.Parameters.AddWithValue("@perfCod", objUsuario.perfCod);

                        //-> Se tiver id, faz Update:
                        if (objUsuario.usuCod > 0)
                        {
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.Usuario
                                                        SET 
                                                        usuNome=@usuNome, 
                                                        usuLogin=@usuLogin, 
                                                    ";
                            //-> Se a senha estiver preenchida, atualiza também.
                            if (objUsuario.usuSenha.Length > 0)
                            {
                                command.CommandText += @"
                                                            usuSenha=@usuSenha, 
                                                    ";
                            }

                            command.CommandText += @"
                                                        usuStatus=@usuStatus, 
                                                        perfCod=@perfCod
                                                        WHERE usuCod=@usuCod;
                                                    ";

                            command.ExecuteNonQuery();
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.Usuario
                                                        (usuNome, usuLogin, usuSenha, usuStatus, perfCod)
                                                        VALUES(
                                                        @usuNome, 
                                                        @usuLogin, 
                                                        @usuSenha, 
                                                        @usuStatus, 
                                                        @perfCod
                                                        );
                                                    ";
                            command.ExecuteNonQuery();
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        InsereLog(objUsuario, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string ManterItemChecklist(ItemCheckListModel objChecklist, int usuCod)
        {
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@itmChLsCod", objChecklist.itmChLsCod);
                        command.Parameters.AddWithValue("@itmChLsDesc", objChecklist.itmChLsDesc);
                        command.Parameters.AddWithValue("@itmChLsObrig", objChecklist.itmChLsObrig);
                        command.Parameters.AddWithValue("@itmChLsStatus", objChecklist.itmChLsStatus);

                        //-> Se tiver id, faz Update:
                        if (objChecklist.itmChLsCod > 0)
                        {
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.ItemCheckList
                                                        SET 
                                                        itmChLsDesc=@itmChLsDesc, 
                                                        itmChLsObrig=itmChLsObrig, 
                                                        itmChLsStatus=@itmChLsStatus
                                                        WHERE itmChLsCod=@itmChLsCod;
                                                    ";
                            command.ExecuteNonQuery();
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.ItemCheckList
                                                        (itmChLsDesc, itmChLsObrig, itmChLsStatus)
                                                        VALUES(
                                                        @itmChLsDesc, 
                                                        @itmChLsObrig, 
                                                        @itmChLsStatus
                                                        );
                                                    ";
                            command.ExecuteNonQuery();
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        InsereLog(objChecklist, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string ManterCheckList(ChecklistEnvioModel objChecklist, int usuCod)
        {
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@chLsCod", objChecklist.objChecklist.chLsCod);
                        command.Parameters.AddWithValue("@chLsDesc", objChecklist.objChecklist.chLsDesc);
                        command.Parameters.AddWithValue("@chLsStatus", objChecklist.objChecklist.chLsStatus);
                        command.Parameters.AddWithValue("@tipChLiCod", objChecklist.objChecklist.tipChLiCod);

                        //-> Se tiver id, faz Update:
                        if (objChecklist.objChecklist.chLsCod > 0)
                        {
                            //-> Altera primeiro o checklist
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.CheckList
                                                        SET 
                                                        chLsDesc=@chLsDesc, 
                                                        chLsStatus=@chLsStatus, 
                                                        tipChLiCod=@tipChLiCod
                                                        WHERE chLsCod=@chLsCod;
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Apaga os itens que estavam relacionados (ChkLstItmChkLst)
                            command.CommandText = @"
                                                        DELETE FROM " + _bdAgenda + @".dbo.ChkLstItmChkLst
                                                        WHERE chLsCod=@chLsCod;
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Insere os novos itens na tabela de relacionamento (ChkLstItmChkLst)
                            foreach (var itmChLs in objChecklist.listaItemChecklist)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@chLsCod", objChecklist.objChecklist.chLsCod);
                                command.Parameters.AddWithValue("@itmChLsCod", itmChLs.itmChLsCod);
                                command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.ChkLstItmChkLst
                                                        (chLsCod, itmChLsCod)
                                                        VALUES(
                                                        @chLsCod, 
                                                        @itmChLsCod
                                                        );
                                                    ";
                                command.ExecuteNonQuery();
                            }
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.CheckList
                                                        (chLsDesc, chLsStatus, tipChLiCod)
                                                        VALUES(
                                                        @chLsDesc, 
                                                        @chLsStatus, 
                                                        @tipChLiCod
                                                        );
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Retornando o Id que foi inserido da equipe.
                            command.CommandText = @"
                                                        SELECT MAX(chLsCod) FROM " + _bdAgenda + @".dbo.CheckList WITH(NOLOCK);
                                                    ";
                            int lastEquipCod = 0;
                            DataTable dt = new DataTable();
                            SqlDataReader reader;
                            reader = command.ExecuteReader();
                            dt.Load(reader);
                            if (dt.Rows.Count > 0)
                            {
                                lastEquipCod = int.Parse(dt.Rows[0].ItemArray[0].ToString());
                            }

                            if (lastEquipCod > 0)
                            {
                                //-> Insere os novos itens na tabela de relacionamento (ChkLstItmChkLst)
                                foreach (var itmChLs in objChecklist.listaItemChecklist)
                                {
                                    command.Parameters.Clear();
                                    command.Parameters.AddWithValue("@chLsCod", lastEquipCod);
                                    command.Parameters.AddWithValue("@itmChLsCod", itmChLs.itmChLsCod);
                                    command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.ChkLstItmChkLst
                                                        (chLsCod, itmChLsCod)
                                                        VALUES(
                                                        @chLsCod, 
                                                        @itmChLsCod
                                                        );
                                                    ";
                                    command.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                ret = "falha ao realizar a operação.";
                                transaction.Rollback();
                                connection.Close();
                            }
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        InsereLog(objChecklist, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        public string ManterEvento(EventoManterModel objEventoManter, int usuCod)
        {
            string ret = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@eventCod", objEventoManter.objEvento.eventCod);
                        command.Parameters.AddWithValue("@eventDesc", objEventoManter.objEvento.eventDesc);
                        command.Parameters.AddWithValue("@eventLogr", objEventoManter.objEvento.eventLogr);
                        command.Parameters.AddWithValue("@eventBairr", objEventoManter.objEvento.eventBairr);
                        command.Parameters.AddWithValue("@eventDtIn", objEventoManter.objEvento.eventDtIn);
                        command.Parameters.AddWithValue("@evenDtFi", objEventoManter.objEvento.evenDtFi);
                        command.Parameters.AddWithValue("@eventObse", objEventoManter.objEvento.eventObse);
                        command.Parameters.AddWithValue("@eventStatus", objEventoManter.objEvento.eventStatus);
                        command.Parameters.AddWithValue("@horaCod", objEventoManter.objEvento.horaCod);
                        command.Parameters.AddWithValue("@cidaCod", objEventoManter.objEvento.cidaCod);
                        command.Parameters.AddWithValue("@diamCod", objEventoManter.objEvento.diamCod);
                        command.Parameters.AddWithValue("@usuCod", objEventoManter.objEvento.usuCod);
                        command.Parameters.AddWithValue("@maqCod", objEventoManter.objEvento.maqCod);
                        command.Parameters.AddWithValue("@tipChLiCod", objEventoManter.objEvento.tipChLiCod);

                        //-> Se tiver id, faz Update:
                        if (objEventoManter.objEvento.eventCod > 0)
                        {
                            //-> Altera primeiro o checklist
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.Evento
                                                        SET 
	                                                        eventDesc=@eventDesc, 
	                                                        eventLogr=@eventLogr, 
	                                                        eventBairr=@eventBairr, 
	                                                        eventDtIn=@eventDtIn, 
	                                                        evenDtFi=@evenDtFi, 
	                                                        eventObse=@eventObse, 
	                                                        eventStatus=@eventStatus, 
	                                                        horaCod=@horaCod, 
	                                                        cidaCod=@cidaCod, 
	                                                        diamCod=@diamCod, 
	                                                        usuCod=@usuCod, 
	                                                        maqCod=@maqCod, 
	                                                        tipChLiCod=@tipChLiCod
                                                        WHERE eventCod=@eventCod;
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Apaga os itens que estavam relacionados (ChkLstItmChkLst)
                            command.CommandText = @"
                                                        DELETE FROM " + _bdAgenda + @".dbo.CheckListRespostas
                                                        WHERE eventCod=@eventCod;
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Insere os novos itens na tabela de relacionamento (ChkLstItmChkLst)
                            foreach (var itmResp in objEventoManter.listaRespostas)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@eventCod", itmResp.eventCod);
                                command.Parameters.AddWithValue("@chkLstItmChkLst", itmResp.chkLstItmChkLst);
                                command.Parameters.AddWithValue("@itmChLsCod", itmResp.chkLstResp);
                                command.CommandText = @"
                                                                INSERT INTO " + _bdAgenda + @".dbo.CheckListRespostas
                                                                (eventCod, chkLstItmChkLst, chkLstResp)
                                                                VALUES(
                                                                    @eventCod, 
                                                                    @chkLstItmChkLst, 
                                                                    @itmChLsCod
                                                                );
                                                            ";
                                command.ExecuteNonQuery();
                            }
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.Evento
                                                        (eventDesc, eventLogr, eventBairr, eventDtIn, evenDtFi, eventObse, eventStatus, horaCod, 
                                                            cidaCod, diamCod, usuCod, maqCod, tipChLiCod)
                                                        VALUES(
                                                            @eventDesc, 
                                                            @eventLogr, 
                                                            @eventBairr, 
                                                            @eventDtIn, 
                                                            @evenDtFi, 
                                                            @eventObse, 
                                                            @eventStatus, 
                                                            @horaCod, 
                                                            @cidaCod, 
                                                            @diamCod, 
                                                            @usuCod, 
                                                            @maqCod, 
                                                            @tipChLiCod
                                                        );
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Retornando o Id que foi inserido da equipe.
                            command.CommandText = @"
                                                        SELECT MAX(eventCod) FROM " + _bdAgenda + @".dbo.Evento WITH(NOLOCK);
                                                    ";
                            int lastEventCod = 0;
                            DataTable dt = new DataTable();
                            SqlDataReader reader;
                            reader = command.ExecuteReader();
                            dt.Load(reader);
                            if (dt.Rows.Count > 0)
                            {
                                lastEventCod = int.Parse(dt.Rows[0].ItemArray[0].ToString());
                            }

                            if (lastEventCod > 0)
                            {
                                //-> Insere os novos itens na tabela de relacionamento (ChkLstItmChkLst)
                                foreach (var itmResp in objEventoManter.listaRespostas)
                                {
                                    command.Parameters.Clear();
                                    command.Parameters.AddWithValue("@eventCod", lastEventCod);
                                    command.Parameters.AddWithValue("@chkLstItmChkLst", itmResp.chkLstItmChkLst);
                                    command.Parameters.AddWithValue("@itmChLsCod", itmResp.chkLstResp);
                                    command.CommandText = @"
                                                                INSERT INTO " + _bdAgenda + @".dbo.CheckListRespostas
                                                                (eventCod, chkLstItmChkLst, chkLstResp)
                                                                VALUES(
                                                                    @eventCod, 
                                                                    @chkLstItmChkLst, 
                                                                    @itmChLsCod
                                                                );
                                                            ";
                                    command.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                ret = "falha ao realizar a operação.";
                                transaction.Rollback();
                                connection.Close();
                            }
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        InsereLog(objEventoManter, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }
        #endregion

        //--------------------------------------------------------------------------------------------------------------------------------

        #region PESQUISAS
        public List<DiametroFuroModel> ListaDiametroFuro(int diamCod)
        {
            List<DiametroFuroModel> listaRetorno = new List<DiametroFuroModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@diamCod", diamCod);

                        command.CommandText = @"
                                                    SELECT diamCod, diamDesc, diamMax, diamMin, diamStatus
                                                    FROM " + _bdAgenda + @".dbo.DiametroFuro WITH(NOLOCK)
                                                ";

                        if (diamCod > 0)
                        {
                            command.CommandText += " WHERE diamCod = @diamCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            DiametroFuroModel objItem;
                            while (reader.Read())
                            {
                                objItem = new DiametroFuroModel();

                                objItem.diamCod = int.Parse(reader["diamCod"].ToString());
                                objItem.diamDesc = reader["diamDesc"].ToString();
                                objItem.diamMax = int.Parse(reader["diamMax"].ToString());
                                objItem.diamMin = int.Parse(reader["diamMin"].ToString());
                                objItem.diamStatus = bool.Parse(reader["diamStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<VeiculoModel> ListaVeiculo(int veicCod)
        {
            List<VeiculoModel> listaRetorno = new List<VeiculoModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@veicCod", veicCod);

                        command.CommandText = @"
                                                    SELECT veicCod, veicMarca, veicModelo, veicAno, veicPlaca, veicObse, veicStatus, tipVeicCod
                                                    FROM " + _bdAgenda + @".dbo.Veiculo WITH(NOLOCK)
                                                ";

                        if (veicCod > 0)
                        {
                            command.CommandText += " WHERE veicCod = @veicCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            VeiculoModel objItem;
                            while (reader.Read())
                            {
                                objItem = new VeiculoModel();

                                objItem.veicCod = int.Parse(reader["veicCod"].ToString());
                                objItem.veicMarca = reader["veicMarca"].ToString();
                                objItem.veicModelo = reader["veicModelo"].ToString();
                                objItem.veicAno = int.Parse(reader["veicAno"].ToString());
                                objItem.veicPlaca = reader["veicPlaca"].ToString();
                                objItem.veicObse = reader["veicObse"].ToString();
                                objItem.veicStatus = bool.Parse(reader["veicStatus"].ToString());
                                objItem.tipVeicCod = int.Parse(reader["tipVeicCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<TipoVeiculoModel> ListaTipoVeiculo(int tipVeicCod)
        {
            List<TipoVeiculoModel> listaRetorno = new List<TipoVeiculoModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@veicCod", tipVeicCod);

                        command.CommandText = @"
                                                    SELECT tipVeicCod, tipVeicDesc, tipVeicStatus
                                                    FROM " + _bdAgenda + @".dbo.TipoVeiculo WITH(NOLOCK);
                                                ";

                        if (tipVeicCod > 0)
                        {
                            command.CommandText += " WHERE tipVeicCod = @tipVeicCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            TipoVeiculoModel objItem;
                            while (reader.Read())
                            {
                                objItem = new TipoVeiculoModel();

                                objItem.tipVeicCod = int.Parse(reader["tipVeicCod"].ToString());
                                objItem.tipVeicDesc = reader["tipVeicDesc"].ToString();
                                objItem.tipVeicStatus = bool.Parse(reader["tipVeicStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<MaquinaModel> ListaMaquina(int maqCod)
        {
            List<MaquinaModel> listaRetorno = new List<MaquinaModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@maqCod", maqCod);

                        command.CommandText = @"
                                                    SELECT maqCod, maqMarca, maqModelo, maqObse, maqStatus, diamCod, veicCod
                                                    FROM " + _bdAgenda + @".dbo.Maquina WITH(NOLOCK)
                                                ";

                        if (maqCod > 0)
                        {
                            command.CommandText += " WHERE maqCod = @maqCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            MaquinaModel objItem;
                            while (reader.Read())
                            {
                                objItem = new MaquinaModel();

                                objItem.maqCod = int.Parse(reader["maqCod"].ToString());
                                objItem.maqMarca = reader["maqMarca"].ToString();
                                objItem.maqModelo = reader["maqModelo"].ToString();
                                objItem.maqObse = reader["maqObse"].ToString();
                                objItem.maqStatus = bool.Parse(reader["maqStatus"].ToString());
                                objItem.diamCod = int.Parse(reader["diamCod"].ToString());
                                objItem.veicCod = int.Parse(reader["veicCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<AparelhoNavegacaoModel> ListaAparelhoNavegacao(int apNavCod)
        {
            List<AparelhoNavegacaoModel> listaRetorno = new List<AparelhoNavegacaoModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@apNavCod", apNavCod);

                        command.CommandText = @"
                                                    SELECT apNavCod, apNavMarcMod, apNavObse, apNavStatus
                                                    FROM " + _bdAgenda + @".dbo.AparelhoNavegacao WITH(NOLOCK)
                                                ";

                        if (apNavCod > 0)
                        {
                            command.CommandText += " WHERE apNavCod = @apNavCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            AparelhoNavegacaoModel objItem;
                            while (reader.Read())
                            {
                                objItem = new AparelhoNavegacaoModel();

                                objItem.apNavCod = int.Parse(reader["apNavCod"].ToString()); ;
                                objItem.apNavMarcMod = reader["apNavMarcMod"].ToString(); ;
                                objItem.apNavObse = reader["apNavObse"].ToString(); ;
                                objItem.apNavStatus = bool.Parse(reader["apNavStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<EquipeModel> ListaEquipe(int equipCod)
        {
            List<EquipeModel> listaRetorno = new List<EquipeModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@equipCod", equipCod);

                        command.CommandText = @"
                                                    SELECT equipCod, equipDesc, equipStatus, apNavCod, maqCod
                                                    FROM " + _bdAgenda + @".dbo.Equipe WITH(NOLOCK)
                                                ";

                        if (equipCod > 0)
                        {
                            command.CommandText += " WHERE equipCod = @equipCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            EquipeModel objItem;
                            while (reader.Read())
                            {
                                objItem = new EquipeModel();

                                objItem.equipCod = int.Parse(reader["equipCod"].ToString());
                                objItem.equipDesc = reader["equipDesc"].ToString();
                                objItem.equipStatus = bool.Parse(reader["equipStatus"].ToString());
                                objItem.apNavCod = int.Parse(reader["apNavCod"].ToString());
                                objItem.maqCod = int.Parse(reader["maqCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<UsuarioTbModel> ListaUsuario(int usuCod)
        {
            List<UsuarioTbModel> listaRetorno = new List<UsuarioTbModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@usuCod", usuCod);

                        command.CommandText = @"
                                                    SELECT usuCod, usuNome, usuLogin, usuSenha, usuStatus, perfCod
                                                    FROM " + _bdAgenda + @".dbo.Usuario WITH(NOLOCK)
                                                ";

                        if (usuCod > 0)
                        {
                            command.CommandText += " WHERE usuCod = @usuCod";
                        }

                        command.CommandText += " ORDER BY usuNome";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            UsuarioTbModel objItem;
                            while (reader.Read())
                            {
                                objItem = new UsuarioTbModel();
                                objItem.usuCod = int.Parse(reader["usuCod"].ToString());
                                objItem.usuNome = reader["usuNome"].ToString();
                                objItem.usuLogin = reader["usuLogin"].ToString();
                                objItem.usuSenha = "";// reader["usuSenha"].ToString();
                                objItem.usuStatus = bool.Parse(reader["usuStatus"].ToString());
                                objItem.perfCod = int.Parse(reader["perfCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<PerfilModel> ListaPerfil(int perfCod)
        {
            List<PerfilModel> listaRetorno = new List<PerfilModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@perfCod", perfCod);

                        command.CommandText = @"
                                                    SELECT perfCod, perfDesc, perfStatus
                                                    FROM " + _bdAgenda + @".dbo.Perfil WITH(NOLOCK)
                                                ";

                        if (perfCod > 0)
                        {
                            command.CommandText += " WHERE perfCod = @perfCod";
                        }

                        command.CommandText += " ORDER BY perfDesc";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            PerfilModel objItem;
                            while (reader.Read())
                            {
                                objItem = new PerfilModel();
                                objItem.perfCod = int.Parse(reader["perfCod"].ToString());
                                objItem.perfDesc = reader["perfDesc"].ToString();
                                objItem.perfStatus = bool.Parse(reader["perfStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<HorarioModel> ListaHorario(int horaCod)
        {
            List<HorarioModel> listaRetorno = new List<HorarioModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@horaCod", horaCod);

                        command.CommandText = @"
                                                    SELECT horaCod, horaDesc, horaStatus
                                                    FROM " + _bdAgenda + @".dbo.Horario WITH(NOLOCK)
                                                ";

                        if (horaCod > 0)
                        {
                            command.CommandText += " WHERE horaCod = @horaCod";
                        }

                        command.CommandText += " ORDER BY horaDesc";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            HorarioModel objItem;
                            while (reader.Read())
                            {
                                objItem = new HorarioModel();
                                objItem.horaCod = int.Parse(reader["horaCod"].ToString());
                                objItem.horaDesc = reader["horaDesc"].ToString();
                                objItem.horaStatus = bool.Parse(reader["horaStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<EstadoModel> ListaEstado(int estCod)
        {
            List<EstadoModel> listaRetorno = new List<EstadoModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@estCod", estCod);

                        command.CommandText = @"
                                                    SELECT estCod, estDesc, estSigl, ibgeCod
                                                    FROM " + _bdAgenda + @".dbo.Estado WITH(NOLOCK)
                                                ";

                        if (estCod > 0)
                        {
                            command.CommandText += " WHERE estCod = @estCod";
                        }

                        command.CommandText += " ORDER BY estDesc";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            EstadoModel objItem;
                            while (reader.Read())
                            {
                                objItem = new EstadoModel();
                                objItem.estCod = int.Parse(reader["estCod"].ToString());
                                objItem.estDesc = reader["estDesc"].ToString();
                                objItem.estSigl = reader["estSigl"].ToString();
                                objItem.ibgeCod = int.Parse(reader["ibgeCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<CidadeModel> ListaCidade(int cidaCod, int estCod)
        {
            List<CidadeModel> listaRetorno = new List<CidadeModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@estCod", estCod);
                        command.Parameters.AddWithValue("@cidaCod", cidaCod);

                        command.CommandText = @"
                                                    SELECT cidaCod, cidaDesc, ibgeCod, estCod
                                                    FROM " + _bdAgenda + @".dbo.Cidade WITH(NOLOCK)
                                                ";

                        if (cidaCod > 0 && estCod > 0)
                        {
                            command.CommandText += " WHERE cidaCod = @cidaCod AND estCod = @estCod ";
                        }
                        else if (cidaCod > 0 && estCod == 0)
                        {
                            command.CommandText += " WHERE cidaCod = @cidaCod ";
                        }
                        else if (cidaCod == 0 && estCod > 0)
                        {
                            command.CommandText += " WHERE estCod = @estCod ";
                        }

                        command.CommandText += " ORDER BY cidaDesc";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            CidadeModel objItem;
                            while (reader.Read())
                            {
                                objItem = new CidadeModel();
                                objItem.cidaCod = int.Parse(reader["cidaCod"].ToString());
                                objItem.cidaDesc = reader["cidaDesc"].ToString();
                                objItem.ibgeCod = int.Parse(reader["ibgeCod"].ToString());
                                objItem.estCod = int.Parse(reader["estCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<ItemCheckListModel> ListaItemChecklist(int itmChLsCod)
        {
            List<ItemCheckListModel> listaRetorno = new List<ItemCheckListModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@itmChLsCod", itmChLsCod);

                        command.CommandText = @"
                                                    SELECT itmChLsCod, itmChLsDesc, itmChLsObrig, itmChLsStatus
                                                    FROM " + _bdAgenda + @".dbo.ItemCheckList WITH(NOLOCK)
                                                ";

                        if (itmChLsCod > 0)
                        {
                            command.CommandText += " WHERE itmChLsCod = @itmChLsCod";
                        }

                        command.CommandText += " ORDER BY itmChLsDesc";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            ItemCheckListModel objItem;
                            while (reader.Read())
                            {
                                objItem = new ItemCheckListModel();
                                objItem.itmChLsCod = int.Parse(reader["itmChLsCod"].ToString());
                                objItem.itmChLsDesc = reader["itmChLsDesc"].ToString();
                                objItem.itmChLsObrig = bool.Parse(reader["itmChLsObrig"].ToString());
                                objItem.itmChLsStatus = bool.Parse(reader["itmChLsStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<UsuarioTbModel> ListaUsuariosDisponiveis()
        {
            List<UsuarioTbModel> listaRetorno = new List<UsuarioTbModel>();
            try
            {
                List<UsuarioTbModel> listaUsuarios = new List<UsuarioTbModel>();
                List<EquipeUsuarioModel> listaEquipeUsuario = new List<EquipeUsuarioModel>();

                listaUsuarios = ListaUsuario(0);
                foreach (var item in listaUsuarios)
                {
                    UsuarioTbModel objUsr = new UsuarioTbModel();
                    objUsr = item;
                    listaRetorno.Add(objUsr);
                }
                listaEquipeUsuario = ListaEquipeUsuario(0);

                foreach (var itmUsr in listaUsuarios)
                {
                    foreach (var itmEquiUsr in listaEquipeUsuario)
                    {
                        if (itmUsr.usuCod == itmEquiUsr.usuCod)
                        {
                            listaRetorno.Remove(itmUsr);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<EquipeUsuarioModel> ListaEquipeUsuario(int equipCod)
        {
            List<EquipeUsuarioModel> listaRetorno = new List<EquipeUsuarioModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@equipCod", equipCod);

                        command.CommandText = @"
                                                    SELECT 
	                                                    Equ.equipCod, Equ.equipDesc, Equ.equipStatus 
	                                                    , Usr.usuCod, Usr.usuNome, Usr.usuStatus 
	                                                    , Perf.perfCod , Perf.perfDesc 
                                                    FROM " + _bdAgenda + @".dbo.UsuarioEquipe AS UsrEqu WITH(NOLOCK)
                                                    INNER JOIN " + _bdAgenda + @".dbo.Equipe AS Equ WITH(NOLOCK) ON Equ.equipCod = UsrEqu.equipCod 
                                                    INNER JOIN " + _bdAgenda + @".dbo.Usuario AS Usr WITH(NOLOCK) ON Usr.usuCod = UsrEqu.usuCod 
                                                    INNER JOIN " + _bdAgenda + @".dbo.Perfil AS Perf WITH(NOLOCK) ON Perf.perfCod = Usr.perfCod 
                                                ";

                        if (equipCod > 0)
                        {
                            command.CommandText += " WHERE Equ.equipCod = @equipCod";
                        }

                        command.CommandText += " ORDER BY Equ.equipDesc, Usr.usuNome";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            EquipeUsuarioModel objItem;
                            while (reader.Read())
                            {
                                objItem = new EquipeUsuarioModel();

                                objItem.equipCod = int.Parse(reader["equipCod"].ToString());
                                objItem.equipDesc = reader["equipDesc"].ToString();
                                objItem.equipStatus = bool.Parse(reader["equipStatus"].ToString());
                                objItem.usuCod = int.Parse(reader["usuCod"].ToString());
                                objItem.usuNome = reader["usuNome"].ToString();
                                objItem.usuStatus = bool.Parse(reader["usuStatus"].ToString());
                                objItem.perfCod = int.Parse(reader["perfCod"].ToString());
                                objItem.perfDesc = reader["perfDesc"].ToString();

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<TipoChecklistModel> ListaTipoCheckList(int tipChLiCod)
        {
            List<TipoChecklistModel> listaRetorno = new List<TipoChecklistModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@tipChLiCod", tipChLiCod);

                        command.CommandText = @"
                                                    SELECT tipChLiCod, tipChLiDesc, tipChLiStatus
                                                    FROM " + _bdAgenda + @".dbo.TipoCheckList WITH(NOLOCK)
                                                ";

                        if (tipChLiCod > 0)
                        {
                            command.CommandText += " WHERE tipChLiCod = @tipChLiCod";
                        }

                        command.CommandText += " ORDER BY tipChLiDesc";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            TipoChecklistModel objItem;
                            while (reader.Read())
                            {
                                objItem = new TipoChecklistModel();
                                objItem.tipChLiCod = int.Parse(reader["tipChLiCod"].ToString());
                                objItem.tipChLiDesc = reader["tipChLiDesc"].ToString();
                                objItem.tipChLiStatus = bool.Parse(reader["tipChLiStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<ChecklistModel> ListaCheckList(int chLsCod)
        {
            List<ChecklistModel> listaRetorno = new List<ChecklistModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@chLsCod", chLsCod);

                        command.CommandText = @"
                                                    SELECT chLsCod, chLsDesc, chLsStatus, tipChLiCod
                                                    FROM " + _bdAgenda + @".dbo.CheckList WITH(NOLOCK)
                                                ";

                        if (chLsCod > 0)
                        {
                            command.CommandText += " WHERE chLsCod = @chLsCod";
                        }

                        command.CommandText += " ORDER BY chLsDesc";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            ChecklistModel objItem;
                            while (reader.Read())
                            {
                                objItem = new ChecklistModel();
                                objItem.chLsCod = int.Parse(reader["chLsCod"].ToString());
                                objItem.chLsDesc = reader["chLsDesc"].ToString();
                                objItem.chLsStatus = bool.Parse(reader["chLsStatus"].ToString());
                                objItem.tipChLiCod = int.Parse(reader["tipChLiCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<ChlistItmChlistModel> ListaCheckListItemCheckList(int chLsCod)
        {
            List<ChlistItmChlistModel> listaRetorno = new List<ChlistItmChlistModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@chLsCod", chLsCod);

                        command.CommandText = @"
                                                    SELECT chkLstItmChkLst, chLsCod, itmChLsCod
                                                    FROM " + _bdAgenda + @".dbo.ChkLstItmChkLst WITH(NOLOCK)
                                                ";

                        if (chLsCod > 0)
                        {
                            command.CommandText += " WHERE chLsCod = @chLsCod";
                        }

                        command.CommandText += " ORDER BY itmChLsCod";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            ChlistItmChlistModel objItem;
                            while (reader.Read())
                            {
                                objItem = new ChlistItmChlistModel();
                                objItem.chkLstItmChkLst = int.Parse(reader["chkLstItmChkLst"].ToString());
                                objItem.chLsCod = int.Parse(reader["chLsCod"].ToString());
                                objItem.itmChLsCod = int.Parse(reader["itmChLsCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<CheckListItensModel> ListaChLsByCheckList(int chLsCod)
        {
            List<CheckListItensModel> listaRetorno = new List<CheckListItensModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@chLsCod", chLsCod);

                        command.CommandText = @"
                                                    SELECT
	                                                    ChkLstItmChkLst.chkLstItmChkLst, ChkLstItmChkLst.chLsCod, ChkLstItmChkLst.itmChLsCod
	                                                    , ItemCheckList.itmChLsDesc, ItemCheckList.itmChLsObrig, ItemCheckList.itmChLsStatus
                                                    FROM " + _bdAgenda + @".dbo.ChkLstItmChkLst AS ChkLstItmChkLst
                                                    INNER JOIN " + _bdAgenda + @".dbo.ItemCheckList AS ItemCheckList ON ItemCheckList.itmChLsCod  = ChkLstItmChkLst.itmChLsCod 
                                                    WHERE ChkLstItmChkLst.chLsCod = @chLsCod AND ItemCheckList.itmChLsStatus = 1
                                                    ORDER BY ItemCheckList.itmChLsDesc;
                                                ";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            CheckListItensModel objItem;
                            while (reader.Read())
                            {
                                objItem = new CheckListItensModel();


                                objItem.chkLstItmChkLst = int.Parse(reader["chkLstItmChkLst"].ToString());
                                objItem.chLsCod = int.Parse(reader["chLsCod"].ToString());
                                objItem.itmChLsCod = int.Parse(reader["itmChLsCod"].ToString());
                                objItem.itmChLsDesc = reader["itmChLsDesc"].ToString();
                                objItem.itmChLsObrig = bool.Parse(reader["itmChLsObrig"].ToString());
                                objItem.itmChLsStatus = bool.Parse(reader["itmChLsStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<EventoModel> ListaEventoAtivo(int eventCod)
        {
            List<EventoModel> listaRetorno = new List<EventoModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@eventCod", eventCod);

                        command.CommandText = @"
                                                    SELECT 
	                                                    Evento.eventCod, Evento.eventDesc, Evento.eventLogr, Evento.eventBairr, Evento.eventDtIn, 
	                                                    Evento.evenDtFi, Evento.eventObse, Evento.eventStatus, 
	                                                    Evento.horaCod, Horario.horaDesc,
	                                                    Evento.cidaCod, Cidade.cidaDesc, Estado.estSigl,
	                                                    Evento.diamCod, DiametroFuro.diamDesc,
	                                                    Evento.usuCod, Usuario.usuNome,
	                                                    Evento.maqCod, Maquina.maqMarca, Maquina.maqModelo,
                                                        Evento.tipChLiCod , TipoCheckList.tipChLiDesc 
	                                                    FROM " + _bdAgenda + @".dbo.Evento AS Evento WITH(NOLOCK)
                                                    INNER JOIN " + _bdAgenda + @".dbo.Horario AS Horario WITH(NOLOCK) ON Horario.horaCod = Evento.horaCod
                                                    INNER JOIN " + _bdAgenda + @".dbo.Cidade AS Cidade WITH(NOLOCK) ON Cidade.cidaCod = Evento.cidaCod
                                                    INNER JOIN " + _bdAgenda + @".dbo.Estado AS Estado WITH(NOLOCK) ON Estado.estCod = Cidade.estCod 
                                                    INNER JOIN " + _bdAgenda + @".dbo.DiametroFuro AS DiametroFuro WITH(NOLOCK) ON DiametroFuro.diamCod = Evento.diamCod
                                                    INNER JOIN " + _bdAgenda + @".dbo.Usuario AS Usuario WITH(NOLOCK) ON Usuario.usuCod = Evento.usuCod 
                                                    INNER JOIN " + _bdAgenda + @".dbo.Maquina AS Maquina WITH(NOLOCK) ON Maquina.maqCod = Evento.maqCod
                                                    INNER JOIN " + _bdAgenda + @".dbo.TipoCheckList AS TipoCheckList WITH(NOLOCK) ON TipoCheckList.tipChLiCod = Evento.tipChLiCod
                                                    --WHERE Evento.evenDtFi >= (SELECT DATEADD(dd, 0, DATEDIFF(dd, 0, GETDATE())))
                                                ";

                        if (eventCod > 0)
                        {
                            command.CommandText += " AND Evento.eventCod = @eventCod ";
                        }

                        command.CommandText += " ORDER BY Evento.eventDtIn;";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            EventoModel objItem;
                            while (reader.Read())
                            {
                                objItem = new EventoModel();

                                objItem.eventCod = int.Parse(reader["eventCod"].ToString());
                                objItem.eventDesc = reader["eventDesc"].ToString();
                                objItem.eventLogr = reader["eventLogr"].ToString();
                                objItem.eventBairr = reader["eventBairr"].ToString();
                                objItem.eventDtIn = DateTime.Parse(reader["eventDtIn"].ToString());
                                objItem.evenDtFi = DateTime.Parse(reader["evenDtFi"].ToString());
                                objItem.eventObse = reader["eventObse"].ToString();
                                objItem.eventStatus = bool.Parse(reader["eventStatus"].ToString());
                                objItem.horaCod = int.Parse(reader["horaCod"].ToString());
                                objItem.cidaCod = int.Parse(reader["cidaCod"].ToString());
                                objItem.diamCod = int.Parse(reader["diamCod"].ToString());
                                objItem.usuCod = int.Parse(reader["usuCod"].ToString());
                                objItem.maqCod = int.Parse(reader["maqCod"].ToString());
                                objItem.tipChLiCod = int.Parse(reader["tipChLiCod"].ToString());

                                objItem.horaDesc = reader["horaDesc"].ToString();
                                objItem.cidaDesc = reader["cidaDesc"].ToString();
                                objItem.estSigl = reader["estSigl"].ToString();
                                objItem.diamDesc = reader["diamDesc"].ToString();
                                objItem.usuNome = reader["usuNome"].ToString();
                                objItem.maqMarca = reader["maqMarca"].ToString();
                                objItem.maqModelo = reader["maqModelo"].ToString();
                                objItem.tipChLiDesc = reader["tipChLiDesc"].ToString();

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<MaquinaModel> ListaMaquinasDisponiveis(PesqMaqDispModel objPesquisa)
        {
            objPesquisa.eventDtIn = objPesquisa.eventDtIn.Date;
            objPesquisa.evenDtFi = objPesquisa.evenDtFi.Date;

            List<MaquinaModel> listaMaquinas = new List<MaquinaModel>();
            List<MaquinaModel> listaMaquinasAgendadas = new List<MaquinaModel>();
            List<MaquinaModel> listaMaquinasRetorno = new List<MaquinaModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        #region LISTANDO TODAS AS MÁQUINAS
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@diamCod", objPesquisa.diamCod);
                        command.Parameters.AddWithValue("@eventDtIn", objPesquisa.eventDtIn);
                        command.Parameters.AddWithValue("@evenDtFi", objPesquisa.evenDtFi);
                        command.CommandText = @"
                                                    SELECT maqCod, maqMarca, maqModelo, maqObse, maqStatus, diamCod, veicCod
                                                    FROM " + _bdAgenda + @".dbo.Maquina WITH(NOLOCK)
                                                    WHERE maqStatus = 1 AND diamCod = @diamCod;
                                                ";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            MaquinaModel objItem;
                            while (reader.Read())
                            {
                                objItem = new MaquinaModel();

                                objItem.maqCod = int.Parse(reader["maqCod"].ToString());
                                objItem.maqMarca = reader["maqMarca"].ToString();
                                objItem.maqModelo = reader["maqModelo"].ToString();
                                objItem.maqObse = reader["maqObse"].ToString();
                                objItem.maqStatus = bool.Parse(reader["maqStatus"].ToString());
                                objItem.diamCod = int.Parse(reader["diamCod"].ToString());
                                objItem.veicCod = int.Parse(reader["veicCod"].ToString());

                                listaMaquinas.Add(objItem);
                            }
                        }
                        reader.Close();
                        #endregion

                        #region LISTANDO TODAS AS MÁQUINAS QUE JÁ POSSUEM AGENDAMENTO NESTE PERÍODO

                        //command.CommandText = @"
                        //                            SELECT
	                       //                             Maquina.maqCod, Maquina.maqMarca, Maquina.maqModelo, Maquina.maqObse, Maquina.maqStatus, Maquina.diamCod, Maquina.veicCod
                        //                            FROM " + _bdAgenda + @".dbo.Maquina AS Maquina
                        //                            INNER JOIN " + _bdAgenda + @".dbo.DiametroFuro AS DiametroFuro ON DiametroFuro.diamCod = Maquina.diamCod
                        //                            INNER JOIN " + _bdAgenda + @".dbo.Evento AS Evento ON Evento.maqCod = Maquina.maqCod
                        //                            WHERE Maquina.maqStatus = 1
	                       //                             AND DiametroFuro.diamCod = @diamCod
	                       //                             AND (Evento.eventDtIn >= @eventDtIn AND eventDtIn <= eventDtIn
	                       //                             OR Evento.evenDtFi >= @evenDtFi AND eventDtIn <= eventDtIn)
	                       //                             AND Evento.evenDtFi >= (SELECT DATEADD(dd, 0, DATEDIFF(dd, 0, GETDATE())));
                        //                        ";

                        command.CommandText = @"
                                                    SELECT
                                                        Maquina.maqCod, Maquina.maqMarca, Maquina.maqModelo, Maquina.maqObse, Maquina.maqStatus, 
                                                        Maquina.diamCod, Maquina.veicCod
                                                    FROM " + _bdAgenda + @".dbo.Maquina AS Maquina
                                                    INNER JOIN " + _bdAgenda + @".dbo.DiametroFuro AS DiametroFuro ON DiametroFuro.diamCod = Maquina.diamCod
                                                    INNER JOIN " + _bdAgenda + @".dbo.Evento AS Evento ON Evento.maqCod = Maquina.maqCod
                                                    WHERE Maquina.maqStatus = 1
                                                        AND DiametroFuro.diamCod = @diamCod
                                                        AND Evento.evenDtFi >= (SELECT DATEADD(dd, 0, DATEDIFF(dd, 0, GETDATE())))
                                                        AND (
    		                                                    (@eventDtIn >= Evento.eventDtIn AND @evenDtFi >= Evento.evenDtFi)
    		                                                    AND @eventDtIn <= Evento.evenDtFi
                                                        );
                                                ";

                        reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            MaquinaModel objItem;
                            while (reader.Read())
                            {
                                objItem = new MaquinaModel();

                                objItem.maqCod = int.Parse(reader["maqCod"].ToString());
                                objItem.maqMarca = reader["maqMarca"].ToString();
                                objItem.maqModelo = reader["maqModelo"].ToString();
                                objItem.maqObse = reader["maqObse"].ToString();
                                objItem.maqStatus = bool.Parse(reader["maqStatus"].ToString());
                                objItem.diamCod = int.Parse(reader["diamCod"].ToString());
                                objItem.veicCod = int.Parse(reader["veicCod"].ToString());

                                listaMaquinasAgendadas.Add(objItem);
                            }
                        }
                        reader.Close();
                        #endregion

                        #region REMOVENDO DA LISTA DE MÁQUINAS, AS QUE JÁ EXISTIREM AGENDAMENTO
                        if (listaMaquinasAgendadas.Count > 0)
                        {
                            foreach (var itemMaq in listaMaquinas)
                            {

                                foreach (var itemMaqAgen in listaMaquinasAgendadas)
                                {
                                    if (itemMaqAgen.maqCod != itemMaq.maqCod)
                                    {
                                        listaMaquinasRetorno.Add(itemMaq);
                                    }
                                }
                            }
                        }
                        else
                        {
                            listaMaquinasRetorno = listaMaquinas;
                        }
                        #endregion

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaMaquinasRetorno;
        }

        public bool VerificaLogin(string usuLogin)
        {
            bool ret = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@usuLogin", usuLogin);

                        command.CommandText = @"
                                                    SELECT usuCod, usuNome, usuLogin, usuSenha, usuStatus, perfCod
                                                    FROM " + _bdAgenda + @".dbo.Usuario
                                                    WHERE usuLogin = @usuLogin;
                                                ";

                        DataTable dt = new DataTable();
                        SqlDataReader reader;
                        reader = command.ExecuteReader();
                        dt.Load(reader);
                        if (dt.Rows.Count > 0)
                        {
                            ret = true;
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return ret;
        }
        #endregion
    }
}
