using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lime.Xml;
using Lime.Sql;

namespace Test
{
  class Program
  {
    static void Main(string[] args)
    {
      var prog = new Program();
      //prog.runSqlParser();
      //prog.runTestBuilder();
      prog.runLimeWs();

      My.Exit();
    }

    public void runSqlParser()
    {
      string sql =
        "SELECT DISTINCT\n" +
        "       idsostype, descriptive, soscategory, soscategory.sosbusinessarea,\n" +
        "       webcompany, webperson, web, department, name\n" +
        "FROM   sostype\n" +
        "WHERE  active='1':numeric AND\n" +
        "       soscategory.sosbusinessarea != 2701 AND\n" +
        "       web=1 AND (webperson=1 OR webcompany=1)\n" +
        "ORDER BY descriptive, soscategory DESC\n" +
        "LIMIT  100";

      Parser p = new Parser();
      Node query = p.Parse(sql);
      My.Write("Query: " + query.ToHumanReadableString());
    }

    public void runTestBuilder()
    {
      Node table = Builder.Table("mytable");

      My.Write("Table: " + table);
    }

    public void runTestNode()
    {
      string xml = @"
        <conditions attr=""Hello"" monkey=""Apa"">
          <exp type=""field"">active</exp>
          <exp type=""numeric"">1</exp>
        </conditions>";

      Node n = Node.Parse(xml);

      if (n.HasChildren) {
        foreach (Object o in n.Children) {
          Node c = (Node)o;
          Console.Write("Hello: {0}={1}\n", c.Attributes["type"], c.Value);
        }
      }
    }

    public void runLimeWs()
    {
      string sql =
        "SELECT DISTINCT\n" +
        "       idsostype, descriptive, soscategory, soscategory.sosbusinessarea,\n" +
        "       webcompany, webperson, web, department, name\n" +
        "FROM   sostype\n" +
        "WHERE  active='1':numeric AND\n" +
        "       soscategory.sosbusinessarea != 2701 AND\n" +
        "       web=1 AND (webperson=1 OR webcompany=1)\n" +
        "ORDER BY descriptive, soscategory DESC\n" +
        "LIMIT  0, 5";

      var cli = new Ws.DataServiceClient();
      string data = cli.GetXmlQueryData(Builder.SqlToXml(sql));
      Node ndata = Node.Parse(data);

      foreach (Node child in ndata.Children) {
        Console.Write("* {0}\n", child.Attributes["descriptive"]);
      }
    }
  }


  class My
  {
    /// <summary>
    /// Exit application
    /// </summary>
    public static void Exit()
    {
      My.Exit(0);
    }

    /// <summary>
    /// Exit application with code
    /// </summary>
    /// <param name="code"></param>
    public static void Exit(int code)
    {
#if DEBUG
      Console.WriteLine("\n------------------");
      Console.WriteLine("Hit enter to quit:");
      Console.ReadLine();
#endif
      Environment.Exit(code);
    }

    /// <summary>
    /// Write to stdout
    /// </summary>
    /// <param name="format"></param>
    /// <param name="rest"></param>
    public static void Write(string format, params object[] rest)
    {
      Console.Write(format, rest);
    }

    /// <summary>
    /// Write to stderr
    /// </summary>
    /// <param name="format"></param>
    /// <param name="rest"></param>
    public static void Werror(string format, params object[] rest)
    {
      Console.Error.Write(format, rest);
    }
  }
}
