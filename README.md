csharp-lime-pro
===============

A C# helper module for the web services of the CRM **Lime PRO** by
[Lundalogik](https://github.com/lundalogik).

There's a [PHP version](https://github.com/poppa/php-lime-pro) and a
[Java version](https://github.com/poppa/java-lime-pro) of this module
as well.

This is not a full Lime PRO client but rather a helper module for building the
XML queries to send to Lime, as well as parsing the result. There is however
a sample client in the [test package](https://github.com/poppa/csharp-lime-pro/tree/master/Test).

## Buildning an XML query

The easiest way to build a Lime XML query is by using the SQL to XML class. It
takes an SQL query and converts it into a [Node](https://github.com/poppa/csharp-lime-pro/blob/master/src/Xml.cs#L242)
object.

```cs
using Lime.Xml;
using Lime.Sql;

// ...

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

Node node = Parser.ParseSQL(sql);

```

The SQL query above will result in an XML document like this:

```xml
<query distinct="1" top="100">
  <tables>
    <table>sostype</table>
  </tables>
  <conditions>
    <condition operator="=">
      <exp type="field">active</exp>
      <exp type="numeric">1</exp>
    </condition>
    <condition operator="!=">
      <exp type="field">soscategory.sosbusinessarea</exp>
      <exp type="numeric">2701</exp>
    </condition>
    <condition operator="=">
      <exp type="field">web</exp>
      <exp type="numeric">1</exp>
    </condition>
    <condition>
      <exp type="(" />
    </condition>
    <condition operator="=">
      <exp type="field">webperson</exp>
      <exp type="numeric">1</exp>
    </condition>
    <condition operator="=" or="1">
      <exp type="field">webcompany</exp>
      <exp type="numeric">1</exp>
    </condition>
    <condition>
      <exp type=")" />
    </condition>
  </conditions>
  <fields>
    <field>idsostype</field>
    <field sortorder="desc" sortindex="1">descriptive</field>
    <field sortorder="desc" sortindex="2">soscategory</field>
    <field>soscategory.sosbusinessarea</field>
    <field>webcompany</field>
    <field>webperson</field>
    <field>web</field>
    <field>department</field>
    <field>name</field>
  </fields>
</query>
```

### Typehints

The SQL parser determines the data types in `WHERE` clause based on whether the
value is quoted or not. If it's not quoted it's assumed to be a numeric value.
If it's quoted it's assumed to be a string value. If it's quoted a check
for if the value is a (ISO 8601) date will take place.

But in some cases you need to quote the value and have it as a numeric value,
for instance if you want to do a `IN` or `NOT IN` check on a numeric field.

If that's the case you can use typehints:

```sql
WHERE some_col NOT IN '12;13;14':numeric
```

Any thing like `:something` is assumed to be a typehint.


## Using a webservice client

Generate a webservice client from the Lime WSDL. In the test client I just 
created in the namespace `Ws`.

The webservice method `GetXmlQueryData` will return a string which is an XML
tree. This XML tree can be passed to the static `Xml.Node.Parse` method which
will turn the result into an `Xml.Node` object which also is an `Iterator` object
so it can easily be traversed.

```cs
// ...
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

var cli = new Ws.DataServiceClient("BasicHttpBinding_IDataService");
string data = cli.GetXmlQueryData(Builder.SqlToXml(sql));
Node ndata = Node.Parse(data);

foreach (Node row in ndata.Children) {
  Console.Write("* {0}\n", row.Attributes["descriptive"]);
}
```

\# 2015-06-01
