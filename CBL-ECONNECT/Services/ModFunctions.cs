using Microsoft.Dynamics.GP.eConnect;
using Microsoft.Dynamics.GP.eConnect.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace CBL_ECONNECT.Services
{
    public class ModFunctions
    {
        string connectionString = "Data Source=RHEMA-MICHAEL\\SQL2017;Initial Catalog=CBL-ERP;User ID=sa;Password=sa";
        string econnectconstring = "Data Source=RHEMA-MICHAEL\\SQL2017;Initial Catalog=TWO;Integrated Security=True";
        eConnectType eConnect = new eConnectType();
        public string CreatePO(int reqid)
        {
            string Trans = ""; string podocnumbers = ""; string ponumber = "";
            POPTransactionType oPOPTransactionType = new POPTransactionType();
            taPoHdr otaPoHdr = new taPoHdr();
            taPoLine_ItemsTaPoLine[] translines;
            GetNextDocNumbers oNextDoc = new GetNextDocNumbers(); //GetNextPOPReceiptNumber
            string[] vendorlist = getVendorList(reqid); int rowCount = 0;
            int i = 0;


            foreach (var item in vendorlist.Where(n => n != null))
            {
                i = 0;
                try
                {
                    string query = "";

                    if (item != string.Empty | string.IsNullOrEmpty(item) == false | item != null)
                    {
                        ponumber = oNextDoc.GetNextPONumber(IncrementDecrement.Increment, econnectconstring);


                        decimal freight = 0; decimal tax = 0; string taxscheduleid = "";
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();

                            query = $"SELECT freight,tax,TaxScheduleId FROM Po_Requisition WITH (NOLOCK) WHERE ID={reqid}";
                            // Create a SqlCommand object
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                // Execute the command and obtain a SqlDataReader
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    // Check if there are rows to read
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            freight = Convert.ToInt32(reader["freight"]); tax = Convert.ToInt32(reader["tax"]); taxscheduleid = reader["taxscheduleid"].ToString();
                                        }
                                    }
                                }
                            }
                        }

                        otaPoHdr.PONUMBER = ponumber;
                        otaPoHdr.VENDORID = item.Trim();
                        otaPoHdr.DOCDATE = DateTime.Now.ToString("yyyy-MM-dd");
                        otaPoHdr.FRTAMNT = freight;
                        otaPoHdr.FRTAMNTSpecified = true;
                        otaPoHdr.USINGHEADERLEVELTAXES = 1;
                        otaPoHdr.USINGHEADERLEVELTAXESSpecified = true;
                        //otaPoHdr.HOld = 1;

                        //otaPoHdr.TAXAMNT = tax;
                        //otaPoHdr.TAXAMNTSpecified = true;
                        //otaPoHdr.TAXSCHID = taxscheduleid;



                        query = $"Select ItemCode,ItemDescription,[Location],UoMCode,Quantity,UnitCost,VendorItemCode,NonInventory,IsCapitalItem,Currency FROM Po_RequisitionEntry WHERE Selected=1 AND Req_Id='{reqid}' AND Vendor='{item}'";

                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();

                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        rowCount = GetPORows(reqid, item.Trim());

                                        translines = Enumerable.Range(0, rowCount).Select(d => new taPoLine_ItemsTaPoLine()).ToArray();

                                        taPoLine_ItemsTaPoLine[] LineItems = new taPoLine_ItemsTaPoLine[rowCount];


                                        while (reader.Read())
                                        {
                                            otaPoHdr.CURNCYID = reader["Currency"].ToString();
                                            translines[i].PONUMBER = ponumber;
                                            translines[i].VENDORID = item;
                                            translines[i].ITEMNMBR = reader["ItemCode"].ToString();
                                            translines[i].VNDITNUM = reader["VendorItemCode"].ToString();
                                            translines[i].NONINVEN = Convert.ToInt16(reader["NonInventory"]);
                                            translines[i].LOCNCODE = reader["Location"].ToString();
                                            translines[i].QUANTITY = Convert.ToDecimal(reader["Quantity"]);
                                            translines[i].QUANTITYSpecified = true;
                                            translines[i].ITEMDESC = reader["ItemDescription"].ToString();
                                            translines[i].UNITCOST = Convert.ToDecimal(reader["UnitCost"]);
                                            translines[i].UNITCOSTSpecified = true;
                                            translines[i].UOFM = reader["UoMCode"].ToString();
                                            translines[i].REQSTDBY = reader["IsCapitalItem"].ToString();
                                            translines[i].CURNCYID = reader["Currency"].ToString();
                                            LineItems[i] = translines[i];
                                            i++;
                                        }

                                        POPTransactionType Pop = new POPTransactionType();
                                        var tempArray = Pop.taPoLine_Items;
                                        Array.Resize(ref tempArray, rowCount);
                                        Pop.taPoLine_Items = tempArray;
                                        Pop.taPoLine_Items = LineItems;

                                        Pop.taPoHdr = otaPoHdr;

                                        var tempArrays = eConnect.POPTransactionType;
                                        Array.Resize(ref tempArrays, 1);
                                        eConnect.POPTransactionType = tempArrays;
                                        eConnect.POPTransactionType[0] = Pop;

                                        string filename = "E:\\POP_Trans.xml";
                                        FileStream fs = new FileStream(filename, FileMode.Create);
                                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());

                                        XmlSerializer serializer = new XmlSerializer(typeof(eConnectType));
                                        serializer.Serialize(writer, eConnect);
                                        writer.Close();

                                        using (eConnectMethods eConCall = new eConnectMethods())
                                        {
                                            XmlDocument xmldoc = new XmlDocument();
                                            xmldoc.Load("E:\\POP_Trans.xml");
                                            string TransDocument = xmldoc.OuterXml;
                                            Trans = eConCall.CreateTransactionEntity(econnectconstring, TransDocument);
                                        }

                                    }
                                }
                            }
                        }
                        podocnumbers = string.Concat(podocnumbers, ",", ponumber);

                    }
                }
                catch (Exception ex)
                {
                    Trans = "Error:" + ex.Message.ToString();
                    throw;
                }

                //return Trans;

            }
            if (string.IsNullOrEmpty(podocnumbers))
            {
                podocnumbers = "Invalid Request ID";
            }
            else
            {

                podocnumbers = podocnumbers.Substring(1);

                //char characterToCheck = ','; string[] result; string updateQuery = "";
                //bool containsCharacter = podocnumbers.Contains(characterToCheck);

                //result = podocnumbers.Split(',');

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var updateQuery = "UPDATE TWO..POP10110 SET Capital_Item=1 where REQSTDBY='true';UPDATE TWO..POP10110 SET REQSTDBY='' WHERE REQSTDBY='true'";
                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        //command.Parameters.AddWithValue("@a", item);
                        //command.Parameters.AddWithValue("@b", reqid);
                        command.ExecuteNonQuery();
                        //if (containsCharacter)
                        //{
                        //    foreach (string item in result)
                        //    {
                        //        command.Parameters.AddWithValue("@a", item);
                        //        command.Parameters.AddWithValue("@b", reqid);
                        //        command.ExecuteNonQuery();
                        //    }
                        //}

                        //else
                        //{
                        //    command.Parameters.AddWithValue("@a", podocnumbers);
                        //    command.Parameters.AddWithValue("@b", reqid);
                        //    command.ExecuteNonQuery();
                        //}


                    }

                }

                //if (containsCharacter)
                //{
                //    foreach (string item in result)
                //    {
                //        updateQuery = "Update PO_Requisition SET NewPoNumber=@a WHERE ID=@b";
                //        using (SqlCommand command = new SqlCommand(updateQuery, connection))
                //        {
                //            command.Parameters.AddWithValue("@a", item);
                //            command.Parameters.AddWithValue("@b", reqid);
                //            command.ExecuteNonQuery();
                //        }

                //    }
                //}
                //else
                //{
                //    updateQuery = "Update PO_Requisition SET NewPoNumber=@a WHERE ID=@b";
                //    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                //    {
                //        command.Parameters.AddWithValue("@a", podocnumbers);
                //        command.Parameters.AddWithValue("@b", reqid);
                //        command.ExecuteNonQuery();
                //    }
                //}


            }


            return podocnumbers;
        }


        public string[] getVendorList(int reqid)
        {
            string[] site = new string[51];

            // Connection string to your SQL Server database
            string connectionString = "Data Source=RHEMA-MICHAEL\\SQL2017;Initial Catalog=CBL-ERP;User ID=sa;Password=sa";

            // SQL query to execute
            string query = $"SELECT DISTINCT(Vendor) Vendor FROM Po_RequisitionEntry WHERE Selected=1 AND Req_Id='{reqid}'";

            // Create and open a connection to the database
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Create a SqlCommand object
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Execute the command and obtain a SqlDataReader
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Check if there are rows to read
                        if (reader.HasRows)
                        {
                            int inc = -1;
                            while (reader.Read())
                            {
                                inc += 1;
                                site[inc] = reader["Vendor"].ToString();
                            }
                        }
                    }
                }
            }

            return site;
        }

        public int GetPORows(int reqid, string vendorid)
        {
            int count = 0;

            // Connection string to your SQL Server database
            string connectionString = "Data Source=RHEMA-MICHAEL\\SQL2017;Initial Catalog=CBL-ERP;User ID=sa;Password=sa";

            // SQL query to execute
            string query = $"SELECT COUNT(*) Total FROM Po_RequisitionEntry WHERE Req_Id='{reqid}' AND Vendor='{vendorid}'";

            // Create and open a connection to the database
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Create a SqlCommand object
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Execute the command and obtain a SqlDataReader
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Check if there are rows to read
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {

                                count = Convert.ToInt32(reader["Total"]);
                            }
                        }
                    }
                }
            }

            return count;
        }

        public string CreateVendor(int reqid)
        {
            string response = ""; string Trans = "";
            taUpdateCreateVendorRcd vendor = new taUpdateCreateVendorRcd();
            //Vendor vend = new Vendor();
            // Create the vendor.
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var query = $"Select VendorId,VendorName,ShortName,CheckName,Class,Currency FROM Vendors with (nolock) WHERE Id='{reqid}'";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                vendor.VENDORID = reader["VendorId"].ToString();
                                vendor.VENDNAME = reader["VendorName"].ToString();
                                vendor.VENDSHNM= reader["Shortname"].ToString();
                                vendor.VNDCHKNM= reader["Checkname"].ToString();
                                vendor.VNDCLSID= reader["Class"].ToString();
                                vendor.VENDSTTS = 1;
                                vendor.VENDSTTSSpecified = true;
                                vendor.CURNCYID= reader["Currency"].ToString();
                                //vendor.ADDRESS1= reader["Address"].ToString();
                                //vendor.CITY= reader["City"].ToString();
                                //vendor.COUNTRY= reader["Country"].ToString();
                                //vendor.PHNUMBR1= reader["Phone1"].ToString();

                                //vend.VENDORID = reader["VendorId"].ToString();
                                //vend.VENDNAME = reader["VendorName"].ToString();
                                //vend.VENDSHNM = reader["Shortname"].ToString();
                                //vend.VNDCHKNM = reader["Checkname"].ToString();
                                //vend.VNDCLSID = reader["Class"].ToString();
                                //vend.VENDSTTS = 1;
                                //vend.VENDSTTSSpecified = true;
                                //vend.CURNCYID = reader["Currency"].ToString();
                            }
                            
                        }

                    }
                    
                    PMVendorMasterType vendorType = new PMVendorMasterType();
                    vendorType.taUpdateCreateVendorRcd = vendor;

                    var tempArrays = eConnect.PMVendorMasterType;
                    Array.Resize(ref tempArrays, 1);
                    eConnect.PMVendorMasterType = tempArrays;
                    eConnect.PMVendorMasterType[0] = vendorType;


                    string filename = "E:\\Vendor.xml";
                    FileStream fs = new FileStream(filename, FileMode.Create);
                    XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());

                    XmlSerializer serializer = new XmlSerializer(typeof(eConnectType));
                    serializer.Serialize(writer, eConnect);
                    writer.Close();

                    using (eConnectMethods eConCall = new eConnectMethods())
                    {
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load("E:\\Vendor.xml");
                        string TransDocument = xmldoc.OuterXml;
                        Trans = eConCall.CreateTransactionEntity(econnectconstring, TransDocument);
                    }

                    return response;
                }
            }
        }

        public class Vendor
        {
            public string VENDORID { get; set; }
            public string VENDNAME { get; set; }
            public string VENDSHNM { get; set; }
            public string VNDCHKNM { get; set; }
            public string VNDCLSID { get; set; }
            public string VENDSTTS { get; set; }
            public Boolean VENDSTTSSpecified { get; set; }
            public string CURNCYID { get; set; }

        }
    }
}