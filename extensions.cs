﻿using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
//using System.Data.SQLite;
namespace zenComparer
{
    public static class Extensions
    {
        public static Regex regex = new Regex("[\\s]",  // \\s  [\n\r]
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.Compiled
    );

        /// <summary>
        /// convert to hashtable
        /// </summary>
        public static Hashtable _convertDataTableToHashTable(DataTable dtIn, string keyField, string valueField)
        {

            Hashtable htOut = new Hashtable();
            foreach (DataRow drIn in dtIn.Rows)
            {
                if (!htOut.ContainsKey(drIn[keyField].ToString().ToLower()))
                htOut.Add(drIn[keyField].ToString().ToLower(), drIn[valueField].ToString());
            }
            return htOut;
        }


        /// <summary>
        /// convert to hashtable
        /// </summary>
        public static Hashtable _convertDataTableToHashTable(DataTable dtIn, int columnIndexKey, int columnIndexValue)
        {

            Hashtable htOut = new Hashtable();
            foreach (DataRow drIn in dtIn.Rows)
            {
                if (!htOut.ContainsKey(drIn[columnIndexKey].ToString().ToLower()))
                    htOut.Add(drIn[columnIndexKey].ToString().ToLower(), drIn[columnIndexValue].ToString());
            }
            return htOut;
        }

        /// <summary>
        /// eliminuje whitespace bardzo wazne chodzi o entery etc
        /// </summary>
        /// <param name="IN"></param>
        /// <returns></returns>
        public static string _cleanstring(string IN)
        {
            return regex.Replace(IN, "");
        }


        public static string _getSeparatedString(string IN, int position)
        {
            char[] delim = { '|' };
            string[] splitted = IN.Split(delim);

            string temp = "";

            try
            {
                temp = splitted[position];
            }
            catch (System.Exception)
            {

                temp = "";
            }

            return temp;
        }


        public static System.Data.DataTable GetDataTable(string sql, string dsn)
        {
            DataTable dt = new DataTable();

            SqlConnection cnn = new SqlConnection(dsn);
            cnn.Open();
            SqlCommand mycommand = new SqlCommand(sql, cnn);
            mycommand.CommandText = sql;
            SqlDataReader reader = mycommand.ExecuteReader();
            dt.Load(reader);
            reader.Close();
            cnn.Close();

            return dt;
        }

        public static string MakeSafeFilename(string filename, char replaceChar)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, replaceChar);
            }
            return filename;
        }

        public static string GetValue(string sql, string dsn)
        {
            string temp;
            try
            {


                //DataTable dt = new DataTable();

                SqlConnection cnn = new SqlConnection(dsn);
                cnn.Open();
                SqlCommand mycommand = new SqlCommand(sql, cnn);
                mycommand.CommandText = sql;


                temp = mycommand.ExecuteScalar().ToString();
                cnn.Close();
            }
            catch (System.Exception ex)
            {

                temp = "ERROR " + ex.Message.ToString();
            }
            return temp;
        }

        /// <summary>
        /// Zwraca Datatable
        /// </summary>
        /// <param name="storedProcedure"></param>
        /// <param name="strsql"></param>
        /// <param name="paramss"></param>
        /// <param name="_dsn">null default</param>
        /// <param name="cacheExpire">jesli wieksze od 0 wtedy cache w minutach</param>
        /// <returns></returns>
        //public static DataTable sqlLiteGetDataTable(string sql, string _dsn)
        //{

        //    SQLiteConnection dbconn;
        //    SQLiteCommand dbCMD;


        //     string dbConnStr = string.Format("Data Source={0};UTF8Encoding=True;Version=3",_dsn);

        //    dbconn = new SQLiteConnection(dbConnStr );
        //    dbconn.Open();

        //    dbCMD = new SQLiteCommand(sql, dbconn);
        //    SQLiteDataReader reader = dbCMD.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        //    DataTable dt = new DataTable();
        //    dt.Load(reader);

        //    dbconn.Close();
        //    return dt;

        //}

    }
}