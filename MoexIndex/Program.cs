//Copyright © Сергей Дворцов, 2017,  Все права защищены

namespace MoexIndex
{

    using MoreLinq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    class Program
    {
        private static string MICEXINDEXCF = "http://moex.com/iss/statistics/engines/stock/markets/index/analytics/MICEXINDEXCF.xml?iss.meta=on&iss.dp=comma&iss.df=%25Y-%25m-%25d&iss.tf=%25H%3A%25M%3A%25S&iss.dtf=%25Y.%25m.%25d%20%25H%3A%25M%3A%25S&iss.json=extended&callback=JSON_CALLBACK&limit=100&date=";

        static void Main(string[] args)
        {

            var uri = string.Format("{0}{1}", MICEXINDEXCF, DateTime.Now.ToString("yyyy-MM-dd"));

            try
            {
                // Creates an HttpWebRequest for the specified URL. 
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                // Sends the HttpWebRequest and waits for a response.
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream stm = response.GetResponseStream();
                    XElement element = XElement.Load(stm);
                    var el = element.Elements().FirstOrDefault(e => e.Attribute("id").Value == "analytics");
                    var columnsElement = el.Element("metadata").Element("columns").Elements();

                    var columns = new Dictionary<string, string>(); 

                    columnsElement.ForEach(c => 
                    {
                        columns.Add(c.Attribute("name").Value, c.Attribute("type").Value);    
                    });

                    var rows = el.Element("rows").Elements();

                    rows.ForEach(row =>
                    {
                        columns.ForEach(kvp => 
                        {
                            switch (kvp.Key)
                            {


                                default:
                                    break;
                            }

                        });

                    });





                }
                else
                {
                    Console.WriteLine("StatusDescription is: {0}", response.StatusDescription);
                }

                response.Close();



            }
            catch (WebException e)
            {
                Console.WriteLine("\r\nWebException Raised. The following error occured : {0}", e.Status);
            }
            catch (Exception e)
            {
                Console.WriteLine("\nThe following Exception was raised : {0}", e.Message);
            }


            Console.Read();

        }
    }
}
