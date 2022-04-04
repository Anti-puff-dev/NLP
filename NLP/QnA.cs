using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySQL;

namespace NLP
{
    public class QnA
    {
        public static string Experiment = "default";
        public static string DbQTable = "nlp_questions";
        public static string DbATable = "nlp_answers";
        public static string DbConnection
        {
            get => MySQL.DbConnection.ConnString;
            set { MySQL.DbConnection.ConnString = value; }
        }
        public static float PoolingRate = 0.7f;
        public static float SimilarityThreshold = 0.4f;

        public static void Train(string question, string answer)
        {
            string answer_id = "";
            try
            {
                answer_id = Data.Query($"SELECT answer_id FROM {DbATable} WHERE phrase=?phrase AND experiment=?experiment", new string[] { answer, Experiment }).Tables[0].Rows[0][0].ToString();
            } catch(Exception err) { }


            if (String.IsNullOrEmpty(answer_id))
            {
                answer_id = Data.Query($"INSERT INTO {DbATable} (experiment, phrase) VALUES (?experiment, ?phrase);SELECT LAST_INSERT_ID();", new string[] { Experiment, answer }).Tables[0].Rows[0][0].ToString();
            }

            Data.Query($"INSERT IGNORE {DbQTable} (experiment, answer_id, phrase) VALUES (?experiment, ?answer_id, ?phrase)", new string[] { Experiment, answer_id, question });
        }


        public static void Train(string[] questions, string answer)
        {
            foreach(string question in questions)
            {
                Train(question, answer);
            }
        }


        public static string Predict(string text)
        {
            text = Sanitize.HardApply(text);
            string[] list = text.Split(new char[] { ' ', '\t' });
            string[] words = list.Distinct().ToArray<string>();

            string _match = "";

            foreach (string token in words)
            {
                string ptoken = Tokenize.WordPooling(token, PoolingRate);
                if (token.Length > 2) _match += (_match == "" ? "" : ",") + ("\"" + token + "\"");
                if (ptoken.Length > 2) _match += (_match == "" ? "" : ",") + ("" + ptoken + "*");
            }

            string query = $"SET @q:='{text}';SET @m:='{_match}';SELECT question_id, {DbQTable}.answer_id,{DbATable}.phrase, (MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)) AS relevance,(SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase)) AS distance FROM {DbQTable} INNER JOIN {DbATable} ON {DbQTable}.answer_id={DbATable}.answer_id WHERE {DbQTable}.experiment=?experiment AND MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)>0 AND (SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase))>{(SimilarityThreshold*10)} ORDER BY (distance*(MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE))) DESC LIMIT 1;";
     
            Models.QnA.Result _result = MySQL.Json.Select.Fill(Data.Query(query, new string[] { Experiment })).Single<Models.QnA.Result>();

            if (!String.IsNullOrEmpty(_result.phrase))
            {
                return _result.phrase;
            }
            else
            {
                return "";
            }
        }


        public static string[] Predict(string text, int results)
        {
            text = Sanitize.HardApply(text);
            string[] list = text.Split(new char[] { ' ', '\t' });
            string[] words = list.Distinct().ToArray<string>();

            string _match = "";

            foreach (string token in words)
            {
                string ptoken = Tokenize.WordPooling(token, PoolingRate);
                if (token.Length > 2) _match += (_match == "" ? "" : ",") + ("\"" + token + "\"");
                if (ptoken.Length > 2) _match += (_match == "" ? "" : ",") + ("" + ptoken + "*");
            }

            string query = $"SET @q:='{text}';SET @m:='{_match}';SELECT question_id, {DbQTable}.answer_id,{DbATable}.phrase, (MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)) AS relevance,(SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase)) AS distance FROM {DbQTable} INNER JOIN {DbATable} ON {DbQTable}.answer_id={DbATable}.answer_id WHERE {DbQTable}.experiment=?experiment AND MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)>0 AND (SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase))>{(SimilarityThreshold * 10)} ORDER BY (distance*(MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE))) DESC LIMIT {results};";
            Models.QnA.Result[] _result = MySQL.Json.Select.Fill(Data.Query(query, new string[] {Experiment })).Multiple<Models.QnA.Result>();

            if (_result.Length > 0)
            {
                string[] r = new string[_result.Length];
                for(int i=0; i< _result.Length; i++)
                {
                    r[i] = _result[i].phrase;
                }

                return r;
            }
            else
            {
                return new string[] { };
            }
        }



        #region Functions
        public static void ClearDb()
        {
            Data.Query($"DELETE FROM {DbQTable} WHERE experiment=?experiment", new string[] { Experiment });
            Data.Query($"DELETE FROM {DbATable} WHERE experiment=?experiment", new string[] { Experiment });
        }
        #endregion Functions
    }
}
