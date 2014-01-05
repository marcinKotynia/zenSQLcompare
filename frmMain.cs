﻿using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Data.SqlClient;
namespace zenComparer
{
    public partial class frmMain : Form
    {

        public  static string  masterConnectionString;
         public static string slaveConnectionString ;
         public static string commandLineArguments;
         public static string commandLineTool;

        #region Initialize
        int excludedNumber;
        public frmMain()
        {
            InitializeComponent();

            //Form
            this.Text = string.Format("{0} {1} | {2}", AssemblyProduct, AssemblyVersion, AssemblyDescription);

          //  toolCompare.Click += new EventHandler(btncompare_Click);

            //Porownanie schema
            btnStartCompare.Click += new EventHandler(btncompare_Click); //porownanie schema
            btnShowDiff.Click += new EventHandler(btnShowDiff_Click);


            butSwitch.Click += new EventHandler(butSwitch_Click);
            txtMaster.KeyUp += new KeyEventHandler(ConnectionStringToTextbox);
            txtSlave.KeyUp += new KeyEventHandler(ConnectionStringToTextbox);


            //KAzde nacisniecie powoduje odswiezenie connectionstringa
            mserver.KeyUp += new KeyEventHandler(TextboxToConnectionString);
            mdatabase.KeyUp += new KeyEventHandler(TextboxToConnectionString);
            mlogin.KeyUp += new KeyEventHandler(TextboxToConnectionString);
            mpassword.KeyUp += new KeyEventHandler(TextboxToConnectionString);
            mSSI.CheckedChanged += new EventHandler(CheckedChanged);


            sserver.KeyUp += new KeyEventHandler(TextboxToConnectionString);
            sdatabase.KeyUp += new KeyEventHandler(TextboxToConnectionString);
            slogin.KeyUp += new KeyEventHandler(TextboxToConnectionString);
            spassword.KeyUp += new KeyEventHandler(TextboxToConnectionString);
            sssi.CheckedChanged += new EventHandler(CheckedChanged);

            //Ekran About
            toolAbout.Click += new EventHandler(toolAbout_Click);
            commandLineArguments = txtarguments.Text;
            commandLineTool = txtExternalTool.Text;
        }

        void btnShowDiff_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow dgvr in dgResult.Rows)
                {
                    if ((dgvr.Selected && (string)dgvr.Cells["scriptModel"].Value != "" && (string)dgvr.Cells["scriptTarget"].Value != ""))
                    {
                        Process myProc;

                        // Generate File

                        string filenameModel = string.Format("{0}_{1}_{2}_M.sql", mdatabase.Text, (string)dgvr.Cells["object"].Value, DateTime.Now.ToString("yyyymmddhhmmss"));
                        FileStream f = new FileStream(filenameModel, FileMode.Create);
                        StreamWriter s = new StreamWriter(f);

                        s.WriteLine((string)dgvr.Cells["scriptModel"].Value);

                        s.Close();
                        f.Close();


                        string filenameTarget = string.Format("{0}_{1}_{2}_T.sql", sdatabase.Text, (string)dgvr.Cells["object"].Value, DateTime.Now.ToString("yyyymmddhhmmss"));
                        f = new FileStream(filenameTarget, FileMode.Create);
                        s = new StreamWriter(f);

                        s.WriteLine((string)dgvr.Cells["scriptTarget"].Value);

                        s.Close();
                        f.Close();


                        string commandline = txtarguments.Text.Replace("%Model", Application.StartupPath + "\\" + filenameModel);
                        commandline = commandline.Replace("%Target", Application.StartupPath + "\\" + filenameTarget);

                        myProc = Process.Start(txtExternalTool.Text, commandline);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        #endregion


        /// <summary>
        /// Porownnanie
        /// </summary>
        void btncompare_Click(object sender, EventArgs e)
        {
         //   try
           // {
                Cursor.Current = Cursors.WaitCursor;
                excludedNumber = 0;
                dgResult.Rows.Clear();
                toolLog.Clear();
                result.Clear();
                logger("COMPARE START");

                compareTables();
                Refresh();
                compareObjects();

                gridToScript(true);

                logger(string.Format("COMPARE FINISHED found {0} problems. Excluded item {1}", dgResult.Rows.Count.ToString(), excludedNumber));
                Cursor.Current = Cursors.Default;

            //}
            //catch (Exception ex)
            //{
            //    Cursor.Current = Cursors.Default;
            //    MessageBox.Show(this, ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            //}
        }


        /// <summary>
        /// Dodanie pozycji do gridview
        /// </summary>
        void dgResultInsertRow(string _source, string _type, string _object, string _details, string _action, string _scriptModel, string _scriptTarget)
        {
            DataGridViewRow dgrow = new DataGridViewRow();
            DataGridViewCell dgCell;

            dgCell = new DataGridViewTextBoxCell();
            dgCell.Value = _source;
            dgrow.Cells.Add(dgCell);

            dgCell = new DataGridViewTextBoxCell();
            dgCell.Value = _type;
            dgrow.Cells.Add(dgCell);

            dgCell = new DataGridViewTextBoxCell();
            dgCell.Value = _object;
            dgrow.Cells.Add(dgCell);

            dgCell = new DataGridViewTextBoxCell();
            dgCell.Value = _details;
            dgrow.Cells.Add(dgCell);

            dgCell = new DataGridViewTextBoxCell();
            dgCell.Value = _action;
            dgrow.Cells.Add(dgCell);

            dgCell = new DataGridViewTextBoxCell();
            dgCell.Value = _scriptModel.Trim();
            dgrow.Cells.Add(dgCell);

            dgCell = new DataGridViewTextBoxCell();
            dgCell.Value = _scriptTarget.Trim();
            dgrow.Cells.Add(dgCell);

            dgResult.Rows.Add(dgrow);
            //dgResult.Refresh();

        }


        void CheckedChanged(object sender, EventArgs e)
        {
            TextboxToConnectionString(null, null);
        }



        void butSwitch_Click(object sender, EventArgs e)
        {
            string temp = txtMaster.Text;
            string temp1 = txtSlave.Text;

            txtMaster.Text = temp1;
            txtSlave.Text = temp;
            Refresh();
            ConnectionStringToTextbox(null, null);
        }

        void ConnectionStringToTextboxMethod()
        {
            try
            {
                DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                DbConnectionStringBuilder builder1 = new DbConnectionStringBuilder();

                builder.ConnectionString = txtMaster.Text.Replace(Environment.NewLine, " ");
                builder1.ConnectionString = txtSlave.Text.Replace(Environment.NewLine, " ");

                mserver.Text = connectionStringValue(builder, "Data source");
                mdatabase.Text = connectionStringValue(builder, "Initial Catalog", "database");
                mlogin.Text = connectionStringValue(builder, "User ID", "UID");
                mpassword.Text = connectionStringValue(builder, "Password");
                mSSI.Checked = connectionStringValue(builder, "Integrated Security").ToLower() == "sspi" ? true : false;

                sserver.Text = connectionStringValue(builder1, "Data source");
                sdatabase.Text = connectionStringValue(builder1, "Initial Catalog", "database");
                slogin.Text = connectionStringValue(builder1, "User ID", "UID");
                spassword.Text = connectionStringValue(builder1, "Password");
                sssi.Checked = connectionStringValue(builder1, "Integrated Security").ToLower() == "sspi" ? true : false;


                masterConnectionString = builder.ConnectionString;
                slaveConnectionString = builder1.ConnectionString;
            }
            catch (Exception ex)
            {
                toolLog.Text = ex.Message.ToString();
            }
        }


        /// <summary>
        /// Tlumaczenie connectionstring
        /// </summary>
        void ConnectionStringToTextbox(object sender, KeyEventArgs e)
        {
            ConnectionStringToTextboxMethod();
        }


        void TextboxToConnectionStringMethod()
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
            try
            {
                builder.Add("Data Source", mserver.Text);
                builder.Add("Initial Catalog", mdatabase.Text);

                if (mSSI.Checked)
                    builder.Add("Integrated Security", "SSPI");
                else
                {
                    builder.Add("User ID", mlogin.Text);
                    builder.Add("Password", mpassword.Text);
                }

                txtMaster.Text = builder.ConnectionString;
            }
            catch (Exception)
            {
                toolLog.Text = "Invalid Model connection String";
            }

            try
            {

                builder.Clear();
                builder.Add("Data Source", sserver.Text);
                builder.Add("Initial Catalog", sdatabase.Text);

                if (sssi.Checked)
                    builder.Add("Integrated Security", "SSPI");
                else
                {
                    builder.Add("User ID", slogin.Text);
                    builder.Add("Password", spassword.Text);
                }
                txtSlave.Text = builder.ConnectionString;


            }
            catch (Exception)
            {

                toolLog.Text = "Invalid Target connection String";
            }

            toolLog.Text = "";
        }


        void TextboxToConnectionString(object sender, KeyEventArgs e)
        {
            TextboxToConnectionStringMethod();
        }




        string connectionStringValue(DbConnectionStringBuilder builder, string input, string input1)
        {
            object test = "";
            if (!builder.TryGetValue(input, out test))
                if (!builder.TryGetValue(input1, out test))
                    return "";

            return test.ToString();

        }

        string connectionStringValue(DbConnectionStringBuilder builder, string input)
        {
            object test = "";
            if (!builder.TryGetValue(input, out test))
                return "";

            return test.ToString();

        }


        void logger(string text)
        {
            toolLog.Text = text;
            //toolLog.Refresh();
            if (ActiveForm != null)
                ActiveForm.Refresh();
        }



        /// <summary>
        /// Porownywanie Stored Procedure,Views,Functions,Triggers
        /// </summary>
        void compareObjects()
        {
            //Pobranie danych z master

            logger("Get schema for objects from MODEL ");

            DataTable master = new DataTable();

            //if (lblfile.Text.Length > 0)
            //master = Extensions.sqlLiteGetDataTable("select * from sqlObjects", lblfile.Text);
            //else
            master = Extensions.GetDataTable(variables.sqlObjects, txtMaster.Text);

            //Pobranie danych z slav
            logger("Get schema for objects from TARGET");
            DataTable slave = new DataTable();
            slave = Extensions.GetDataTable(variables.sqlObjects, txtSlave.Text);


            logger("START COMPARING objects SCHEMA");
            compare(master, slave, out excludedNumber);
            master.Dispose();
            slave.Dispose();
        }

        /// <summary>
        /// Porownywanie tables
        /// </summary>
        void compareTables()
        {
            //Pobranie danych z master
            logger("Get schema for tables from MODEL");
            DataTable master = new DataTable();
            DataTable slave = new DataTable();


            master = Extensions.GetDataTable(variables.sqlTables, txtMaster.Text);

            logger("Get schema for tables from TARGET");
            //Pobranie danych z slav
            slave = Extensions.GetDataTable(variables.sqlTables, txtSlave.Text);

            logger("START COMPARING tables SCHEMA");

            compare(master, slave, out excludedNumber);

            master.Dispose();
            slave.Dispose();
        }


        /// <summary>
        /// Procedura porownujaca Datatabel musi posiadac key i text
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slave"></param>
        public void compare(DataTable master, DataTable slave, out int excludedCounter)
        {

            excludedCounter = excludedNumber;

            //pobranie slave
            Hashtable ht = zenComparer.Extensions._convertDataTableToHashTable(slave, "Key", "text");

            foreach (DataRow r in master.Rows)
            {
                string option = r["type"].ToString().Trim().ToUpper(); //typ
                string action = "";
                string a = "", b = "", temp = "";
                bool excluded = false;
                string script = "";


                string ModelScript = "";
                string TargetScript = "";

                //Sprawdzenie czy wogole porownywac

                char[] delim = { ',' };
                string[] excludeditem = txtexclude.Text.ToLower().Split(delim);

                string details = "";
                string key = Extensions._getSeparatedString(r["key"].ToString().ToLower(), 0);
                key = key.Replace("dbo.", "");
                key = key.Replace("[", "");
                key = key.Replace("]", ""); //nazwa obiektu




                foreach (string s in excludeditem)
                {
                    if (key == s.ToLower())
                    {
                        excluded = true;
                        break;
                    }
                }

                if (excluded)
                {
                    excludedCounter += 1;
                    continue;

                }

                //sprawdzenie czy slave zawiera taki klucz jelsi tak trzeba porownac
                if (ht.ContainsKey(r["key"].ToString().ToLower()))
                {

                    a = ht[r["key"].ToString().ToLower()].ToString().Trim(); //pobranie ze slave
                    b = r["text"].ToString().Trim(); //pobranie z master

                    ModelScript = unification(a).Trim();//zenComparer.Extensions._cleanstring(unification(a));
                    TargetScript = unification(b).Trim();//zenComparer.Extensions._cleanstring(unification(b));


                    //Wazne cleanstring porownuje bez whitespace
                    //unification zapewnia ze tpominiete zostana texty create ktore czesto maja male duze litery
                    if (string.CompareOrdinal(zenComparer.Extensions._cleanstring(ModelScript), zenComparer.Extensions._cleanstring(TargetScript)) != 0)
                    {
                        action = "Missmatched";
                    }
                }
                else //Obiekt missing
                {
                    b = r["text"].ToString(); //pobranie z master
                    action = "Missing";
                }
                details = Extensions._getSeparatedString(b, 1);
                if (action == "Missing" || action == "Missmatched")
                    switch (option)
                    {
                        case "U":
                            if (action == "Missing") //Brak tabeli
                            {


                            }
                            break;
                        case "CO": //user tables sa bardziej skomplikowane
                            /// 0 -table name
                            /// 1- column_name
                            /// 2- isnull(cast(data_type as varchar), '')
                            /// 3- isnull(cast(character_maximum_length as varchar), '')
                            /// 4- isnull(column_default, '')
                            /// 5- numeric precision
                            /// 6- numeric_scale
                            /// 7- is nullable
                            /// 8- default constraint name
                            /// 9- isidentity 1
                            if (action == "Missing")  //brakuje kolumny
                            {

                                script = string.Format("alter table [{0}] add [{1}] {2} {3} {4}",
                                    Extensions._getSeparatedString(b, 0),  //table name
                                    Extensions._getSeparatedString(b, 1),  //column name
                                    datatype(b),  //get varchar(max) decimal(18,2)
                                       identity(b), //identity
                                       notnull(b) //not null
                                   );

                                dgResultInsertRow("Target",
                                    string.Format("{0}.{1}.{2}", SymbolToObject(option), "Column", action),
                                     key,
                                    details,
                                    "Generated script " + temp,
                                    script, "");

                            }
                            else if (action == "Missmatched") //data_type, character maximum length,numeric precision, numeric scale
                            {
                                //    0     1
                                //a	"tbldoc|docagreement|datetime|||0|0|YES|"	string
                                //b	"tbldoc|docagreement|smalldatetime|||0|0|YES|"	string
                                string x, x1;

                                //porownanie typow datetime,smalldtatime
                                x = datatype(a); //slave
                                x1 = datatype(b); //master

                                if (string.CompareOrdinal(x, x1) != 0)
                                {

                                    script = string.Format("alter table [{0}] ALTER COLUMN  [{1}] {2} {3} ",
                                      Extensions._getSeparatedString(b, 0),  //table name
                                      Extensions._getSeparatedString(b, 1),  //column name
                                      datatype(b),
                                       notnull(b) //not null
                                      );

                                    dgResultInsertRow("Target",
                                        string.Format("{0}.{1}.{2}", SymbolToObject(option), action, "data type"),
                                        key,
                                        details,
                                        String.Format("Generated script > change {0}> from:{1} to:{2} ", temp, datatype(a), datatype(b)),
                                        script, "");

                                }

                                //porownanie default
                                x = Extensions._getSeparatedString(a, 4);
                                x1 = Extensions._getSeparatedString(b, 4);

                                if (string.CompareOrdinal(x, x1) != 0)
                                {

                                    //script = string.Format("alter table [{0}] drop CONSTRAINT [{1}] DEFAULT  {2} FOR {3}",
                                    // Extensions._getSeparatedString(b, 0),  //table name
                                    // Extensions._getSeparatedString(b, 8),  //default constraint name
                                    // Extensions._getSeparatedString(b, 4),  //isnull(column_default, '')
                                    // Extensions._getSeparatedString(b, 1)  //1- column_name
                                    //  );
                                    //result.AppendText(script + variables.scriptSeparator);

                                    script = string.Format("alter table [{0}] add CONSTRAINT [{1}] DEFAULT  {2} FOR {3}",
                                     Extensions._getSeparatedString(b, 0),  //table name
                                     Extensions._getSeparatedString(b, 8),  //default constraint name
                                     Extensions._getSeparatedString(b, 4),  //isnull(column_default, '')
                                     Extensions._getSeparatedString(b, 1)  //1- column_name
                                      );

                                    dgResultInsertRow("Target",
                                        string.Format("{0}.{1}.{2}", SymbolToObject(option), action, "column_default"),
                                        key,
                                        details,
                                        String.Format("Generated script > change {0}> from:{1} to:{2} ", temp, Extensions._getSeparatedString(a, 4), Extensions._getSeparatedString(b, 4)),
                                        script, "");

                                }

                                //porownanie not null
                                x = Extensions._getSeparatedString(a, 7);
                                x1 = Extensions._getSeparatedString(b, 7);
                                if (string.CompareOrdinal(x, x1) != 0)
                                {

                                    script = string.Format("alter table [{0}] ALTER COLUMN  [{1}] {2} {3} ",
                                      Extensions._getSeparatedString(b, 0),  //table name
                                      Extensions._getSeparatedString(b, 1),  //column name
                                      datatype(b),
                                        // identity(b), //identity
                                       notnull(b) //not null
                                      );

                                    dgResultInsertRow("Target",
                                        string.Format("{0}.{1}.{2}", SymbolToObject(option), action, "not null"),
                                         key,
                                      details,
                                        "no Action > change manual ",
                                        script, "");


                                }

                                //porownanie identity
                                x = Extensions._getSeparatedString(a, 9);
                                x1 = Extensions._getSeparatedString(b, 9);
                                if (string.CompareOrdinal(x, x1) != 0)
                                {
                                    dgResultInsertRow("Target",
                                        string.Format("{0}.{1}.{2}", SymbolToObject(option), action, "identity"),
                                         key,
                                        SymbolToObject(option),
                                       details,
                                       "no Action > change manual ", "");

                                    //nie da sie dodac identity do istniejacej

                                }

                            }


                            break;

                        case "FN":  // Scalar function
                        case "TF":  // Table-valued Function
                        case "IF":  //FN ,IF,P,TR,V
                        case "P":   //FN ,IF,P,TR,V
                        case "TR":  //FN ,IF,P,TR,V
                        case "V":   //FN ,IF,P,TR,V

                            if (action == "Missmatched")
                            {
                                script = replaceAlter(r["text"].ToString());
                                ModelScript = replaceAlter(ModelScript);

                            }
                            else
                            {
                                script = r["text"].ToString();
                            }


                            dgResultInsertRow("Target",
                            string.Format("{0}.{1}", SymbolToObject(option), action),
                             key,
                            details,
                            "Generated Script",
                            script,
                            ModelScript);

                            break;

                        case "PK":
                            //  PK|pk_tblpriceitem|PRIMARY_KEY_CONSTRAINT|tblPriceItem|pricedetailID
                            /// 0- type
                            /// 1- name
                            /// 2- type description
                            /// 3- table name
                            /// 4- column name


                            if (action == "Missing")  //brakuje kolumny
                            {

                                script = string.Format("alter table [{0}]  ADD CONSTRAINT  {1} PRIMARY KEY ({2}) ",
                                            Extensions._getSeparatedString(b, 3),  //table name
                                            Extensions._getSeparatedString(b, 0),  //name
                                            Extensions._getSeparatedString(b, 4)  //column name
                                            );
                                dgResultInsertRow("Target",
                                    string.Format("{0}.{1}", SymbolToObject(option), action),
                                   key,
                                   details,
                                    "Generated Script Primary key ",
                                    script, "");

                            }




                            break;
                        default:
                            dgResultInsertRow("Target",
                                string.Format("{0}.{1}", SymbolToObject(option), action),
                                 key,
                                details,
                                "no Action > change manual ",
                                "", "");
                            break;
                    }


                //if (!excluded && !string.IsNullOrEmpty(script))
                //    result.AppendText(script + variables.scriptSeparator );



            }
            ht.Clear();
            dgResult.Refresh();
        }

        string SymbolToObject(string symbol)
        {

            if (symbol == "FN") return "SQL_SCALAR_FUNCTION";
            else if (symbol == "TF") return "Table-valued Function";
            else if (symbol == "F") return "FOREIGN_KEY_CONSTRAINT";
            else if (symbol == "U") return "USER_TABLE";
            else if (symbol == "CO") return "USER_TABLE";
            else if (symbol == "IF") return "User Function";
            else if (symbol == "P") return "SQL_STORED_PROCEDURE";
            else if (symbol == "TR") return "SQL_TRIGGER";
            else if (symbol == "D") return "DEFAULT_CONSTRAINT";
            else if (symbol == "V") return "VIEW";
            else if (symbol == "IT") return "INTERNAL_TABLE";
            else if (symbol == "PK") return "PRIMARY_KEY_CONSTRAINT";
            else if (symbol == "S") return "SYSTEM_TABLE";
            else if (symbol == "SQ") return "SERVICE_QUEUE";
            else if (symbol == "UQ") return "UNIQUE_CONSTRAINT";

            return symbol;

        }


        string showDiff(string a, string b, int charBeforeAfter)
        {
            string retval = "";

            retval += string.Format("a-length:{0} b-length:{1}\n", a.Length, b.Length);

            if (a.Length == 0 || b.Length == 0)
                return retval;

            CharEnumerator charEnum = a.GetEnumerator();
            int counter = 0;

            int astart;
            int afinish;
            int bfinish;


            while (charEnum.MoveNext())
            {
                if (a[counter] != b[counter])
                {
                    //start
                    if (counter - charBeforeAfter > -1)
                        astart = counter - charBeforeAfter;
                    else
                        astart = 0;

                    //end a
                    if (counter + (charBeforeAfter * 2) < a.Length + 1)
                        afinish = charBeforeAfter * 2;
                    else
                        afinish = a.Length - counter;


                    //end b
                    if (counter + (charBeforeAfter * 2) < b.Length + 1)
                        bfinish = charBeforeAfter * 2;
                    else
                        bfinish = b.Length - counter;


                    retval += string.Format("a-text:[{0}]\nb-text:[{1}]",
                        a.Substring(astart, afinish),
                        b.Substring(astart, bfinish));

                    return retval;
                }

                counter++;
            }
            return retval;

        }


        string identity(string item)
        {

            if (Extensions._getSeparatedString(item, 9) == "1")
                return "IDENTITY(1,1)";
            else
                return "";

        }

        string notnull(string item)
        {

            if (Extensions._getSeparatedString(item, 7) == "YES")
                return "NULL";
            else
                return "NOT NULL";
        }

        /// <summary>
        /// Zadaniem jesst zwrocenie np varchar(max) decimal(18,2)
        /// tblatribut|adddate|smalldatetime||(getutcdate())|0|0|YES|DF_tblAtribut_adddate|0
        /// Parametry getSeparatedString
        /// 0 -table name
        /// 1- column_name
        /// 2- isnull(cast(data_type as varchar), '')
        /// 3- isnull(cast(character_maximum_length as varchar), '')
        /// 4- isnull(column_default, '')
        /// 5- numeric precision
        /// 6- numeric_scale
        /// 7- null not null
        /// 8- default constraint name
        /// 9- isidentity 1
        /// </summary>
        /// <param name="item"></param>
        /// <returns>decimal(18,2) ,varchar(max)</returns>
        string datatype(string item)
        {
            //System.Diagnostics.Debug.Write("datatype:" +
            //   "[0] " + Extensions._getSeparatedString(item, 0) +
            //   "[1] " + Extensions._getSeparatedString(item, 1) +
            //   "[2] " + Extensions._getSeparatedString(item, 2) +
            //   "[3] " + Extensions._getSeparatedString(item, 3) +
            //   "[4] " + Extensions._getSeparatedString(item, 4) +
            //   "[5] " + Extensions._getSeparatedString(item, 5) +
            //   "[6] " + Extensions._getSeparatedString(item, 6) +
            //   "[7] " + Extensions._getSeparatedString(item, 7)
            //    );

            string retval = "";
            switch (Extensions._getSeparatedString(item, 2).ToLower())
            {
                case "numeric":
                case "decimal":
                    retval = String.Format("({0},{1})", Extensions._getSeparatedString(item, 5), Extensions._getSeparatedString(item, 6));
                    break;
                case "sql_variant":
                    retval = "";// String.Format("({0},{1})", Extensions._getSeparatedString(item, 5), Extensions._getSeparatedString(item, 6));
                    break;
                default:
                    retval = string.IsNullOrEmpty(Extensions._getSeparatedString(item, 3))
                        ? "" :
                        string.Concat("(", Extensions._getSeparatedString(item, 3).Replace("-1", "Max"), ")");  //maximum length
                    break;
            }
            retval = String.Concat(Extensions._getSeparatedString(item, 2),//data_type 
                 retval//,   //data_type 
                //Extensions._getSeparatedString(item, 7).ToLower() == "yes" ? " NULL" : " NOT NULL"
                 );//not null


            return correctCreateStatement(retval); //poprawka typowych bledow

        }




        /// <summary>
        /// sqlCreateTable zwraca neiprawidlowe dane trzeba poprawic
        /// </summary>
        /// <param name="strsql"></param>
        /// <returns></returns>
        string correctCreateStatement(string strsql)
        {
            strsql = strsql.ToLower();
            strsql = strsql.Replace("image(2147483647)", "image");
            strsql = strsql.Replace("ntext(1073741823)", "ntext");
            strsql = strsql.Replace("text(2147483647)", "ntext");
            strsql = strsql.Replace("xml(max)", "xml");
            return strsql;
        }


        /// <summary>
        /// Problem: system porownuje procedury uwgzledniajac male duze litery
        /// text create view nei aktualizuje sie w zwiazku z czym moga wystepowac roznice chcoaciz ich nie ma
        /// Rozwiazanie wsyzstkie sentencje create zastapic tym samym
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string unification(string input)
        {
            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+procedure\\s+", "CREATE PROCEDURE ", RegexOptions.IgnoreCase);// create procedure 'input.Replace("create procedure", "alter procedure");
            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+function\\s+", "CREATE FUNCTION ", RegexOptions.IgnoreCase);
            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+trigger\\s+", "CREATE TRIGGER ", RegexOptions.IgnoreCase);
            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+view\\s+", "CREATE VIEW ", RegexOptions.IgnoreCase);
            input = System.Text.RegularExpressions.Regex.Replace(input, "alter\\s+procedure\\s+", "ALTER PROCEDURE ", RegexOptions.IgnoreCase);// create procedure 'input.Replace("create procedure", "alter procedure");
            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+proc\\s+", "CREATE PROCEDURE ", RegexOptions.IgnoreCase);// create procedure 'input.Replace("create procedure", "alter procedure");

            input = System.Text.RegularExpressions.Regex.Replace(input, "alter\\s+function\\s+", "ALTER FUNCTION ", RegexOptions.IgnoreCase);
            input = System.Text.RegularExpressions.Regex.Replace(input, "alter\\s+trigger\\s+", "ALTER TRIGGER ", RegexOptions.IgnoreCase);
            input = System.Text.RegularExpressions.Regex.Replace(input, "alter\\s+view\\s+", "ALTER VIEW ", RegexOptions.IgnoreCase);

            return input;
        }

        /// <summary>
        /// Zamiana na alter jelsi aktualizacja
        /// </summary>
        public string replaceAlter(string input)
        {
            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+procedure\\s+", "ALTER PROCEDURE ", RegexOptions.IgnoreCase);// create procedure 'input.Replace("create procedure", "alter procedure");
            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+proc\\s+", "ALTER PROCEDURE ", RegexOptions.IgnoreCase);// create procedure 'input.Replace("create procedure", "alter procedure");

            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+function\\s+", "ALTER FUNCTION ", RegexOptions.IgnoreCase);
            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+trigger\\s+", "ALTER TRIGGER ", RegexOptions.IgnoreCase);
            input = System.Text.RegularExpressions.Regex.Replace(input, "create\\s+view\\s+", "ALTER VIEW ", RegexOptions.IgnoreCase);
            return input;
        }

        void toolAbout_Click(object sender, EventArgs e)
        {
            frmAbout a = new frmAbout();
            a.Show();
        }


        #region Informacje


        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }
        #endregion

        //zapisz
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dtSqlTables = new DataTable();
                dtSqlTables = Extensions.GetDataTable(variables.sqlTables, txtMaster.Text);
                DataTable dtSqlObjects = new DataTable();
                dtSqlObjects = Extensions.GetDataTable(variables.sqlObjects, txtMaster.Text);


                string returnValue = "";// sql_lite.datatableToSqlLiteTable(dtSqlObjects, dtSqlTables, mserver.Text, mdatabase.Text);

                MessageBox.Show(this, "Schema saved file: " + returnValue, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                MessageBox.Show(this, ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (lblfile.Text.Length > 0)
            {
                lblfile.Text = "";
                btnLoad.Text = "Load schema from file";
                GroupModel.Enabled = true;
                butSwitch.Enabled = true;
                btnSave.Enabled = true;
            }
            else
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Choose schema";
                openFileDialog.InitialDirectory = Environment.CurrentDirectory;
                openFileDialog.Filter = "SchemaFiles (*.db)|*.db";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    lblfile.Text = openFileDialog.FileName;
                    GroupModel.Enabled = false;
                    btnLoad.Text = "Clear";
                    butSwitch.Enabled = false;
                    btnSave.Enabled = false;
                }
            }
        }

        private void mSSI_CheckedChanged(object sender, EventArgs e)
        {
            mlogin.Enabled = !mSSI.Checked;
            mpassword.Enabled = !mSSI.Checked;

        }

        private void sssi_CheckedChanged(object sender, EventArgs e)
        {
            slogin.Enabled = !sssi.Checked;
            spassword.Enabled = !sssi.Checked;
        }

        private void toolLoad_Click(object sender, EventArgs e)
        {

            try
            {



                // create and show an open file dialog
                OpenFileDialog dlgOpen = new OpenFileDialog();
                dlgOpen.InitialDirectory = Application.StartupPath;
                dlgOpen.Filter = "Txt files (*.cfg)|*.cfg";

                if (dlgOpen.ShowDialog() == DialogResult.OK)
                {

                    string text;

                    StreamReader f = new StreamReader(dlgOpen.FileName);
                    text = f.ReadToEnd();
                    f.Close();





                    iniParser x = new iniParser(text);


                    //foreach (Control c in this.Controls)
                    //{
                    //    if (c is TextBox)
                    //    {
                    //        c.Text = x.getSectionString(c.Name).Trim();
                    //    }
                    //}



                    txtMaster.Text = x.getSectionString("MODEL").Trim();
                    txtSlave.Text = x.getSectionString("TARGET").Trim();
                    txtexclude.Text = x.getSectionString("EXCLUDEOBJECT").Trim();


                    ConnectionStringToTextboxMethod();
                    txtMaster.Text = x.getSectionString("MODEL").Trim();
                    txtSlave.Text = x.getSectionString("TARGET").Trim();

                    modeltable.Text = x.getSectionString("COMPARE_DATA_MODELTABLE").Trim();
                    targettable.Text = x.getSectionString("COMPARE_DATA_TARGETTABLE").Trim();
                    tablekey.Text = x.getSectionString("COMPARE_DATA_TABLEKEY").Trim();
                    excludecolumns.Text = x.getSectionString("COMPARE_DATA_EXCLUDEDCOLUMNS").Trim();




                }

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void toolSave_Click(object sender, EventArgs e)
        {
            try
            {

                string filename = string.Format("{0}_{1}.cfg", mdatabase.Text, sdatabase.Text);
                FileStream f = new FileStream(filename, FileMode.Create);
                StreamWriter s = new StreamWriter(f);

                //foreach (Control c in Controls  )
                //{
                //    if (c is TextBox)
                //    {
                //        s.WriteLine(string.Format("[{0}]",c.Name));
                //        s.WriteLine(c.Text);
                //    }
                //}



                s.WriteLine("[MODEL]");
                s.WriteLine(txtMaster.Text);
                s.WriteLine("[TARGET]");
                s.WriteLine(txtSlave.Text);
                s.WriteLine("[EXCLUDEOBJECT]");
                s.WriteLine(txtexclude.Text);

                s.WriteLine("[COMPARE_DATA_MODELTABLE]");
                s.WriteLine(modeltable.Text);
                s.WriteLine("[COMPARE_DATA_TARGETTABLE]");
                s.WriteLine(targettable.Text);
                s.WriteLine("[COMPARE_DATA_TABLEKEY]");
                s.WriteLine(tablekey.Text);
                s.WriteLine("[COMPARE_DATA_EXCLUDEDCOLUMNS]");
                s.WriteLine(excludecolumns.Text);

                s.Close();
                f.Close();
                MessageBox.Show("File Saved " + filename);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }



        void gridToScript(bool all)
        {
            result.Clear();
            int counter = 0;

            foreach (DataGridViewRow dgvr in dgResult.Rows)
            {
                if ((all) || (dgvr.Selected && (string)dgvr.Cells["scriptModel"].Value != ""))
                {
                    counter++;

                    result.AppendText(dgvr.Cells["scriptModel"].Value.ToString());
                    result.AppendText(variables.scriptSeparator);

                    dgvr.DefaultCellStyle.ForeColor = System.Drawing.Color.Blue;
                }
                dgvr.DefaultCellStyle.ForeColor = System.Drawing.Color.Black;
            }
            tabPage2.Text = "Generated [" + counter.ToString() + "] Scripts";
        }



        private void startComparData_Click(object sender, EventArgs e)
        {

            result.Clear();
            compareDATA x = new compareDATA(txtMaster.Text, txtSlave.Text,
                modeltable.Text, targettable.Text, tablekey.Text, excludecolumns.Text);
            result.AppendText(x.Script.ToString());
            MessageBox.Show("Done");

        }

        private void txtMaster_Enter(object sender, EventArgs e)
        {
            txtMaster.ForeColor = System.Drawing.Color.Black;
        }

        private void txtMaster_Leave(object sender, EventArgs e)
        {
            txtMaster.ForeColor = System.Drawing.Color.White;
        }

        private void txtSlave_Enter(object sender, EventArgs e)
        {
            txtSlave.ForeColor = System.Drawing.Color.Black;
        }

        private void txtSlave_Leave(object sender, EventArgs e)
        {
            txtSlave.ForeColor = System.Drawing.Color.White;
        }

        private void btnGridToScript_Click(object sender, EventArgs e)
        {


            gridToScript(false);
        }


    
    }
}