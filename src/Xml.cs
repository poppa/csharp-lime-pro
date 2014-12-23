/**
  This module is an interface to the web services of Lundalogik's web services for Lime PRO.
  http://www.lundalogik.se

  More info can be found at the Github repository https://github.com/poppa/c#-lime-pro.

  Copyright: 2014 Pontus Östlund <poppanator@gmail.com>
  License:   http://opensource.org/licenses/GPL-2.0 GPL License 2
  Link:      https://github.com/poppa
*/

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lime.Xml
{
  public static class Builder
  {
    /// <summary>
    /// SQL to XML
    /// </summary>
    /// <param name="sql"></param>
    /// <returns></returns>
    public static string SqlToXml(string sql)
    {
      return new Sql.Parser().Parse(sql).ToString();
    }

    /// <summary>
    /// Creates a <code><query distinct="1"></query></code> node.
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns></returns>
    public static Node Query(List<Node> nodes)
    {
      var d = new Dictionary<string, string>() {{ "distinct", "1" }};
      return Query(nodes, d);
    }

    /// <summary>
    /// Creates a <code><query/></code> node with attributes <paramref name="attr"/>
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="attr"></param>
    /// <returns></returns>
    public static Node Query(List<Node> nodes, Dictionary<string, string> attr)
    {
      return new Node("query", attr, nodes);
    }

    /// <summary>
    /// Creates a <code><tables><table>name</table></tables></code> node
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Node Table(string name)
    {
      return new Node("tables", new List<Node>() { new Node("table", name) });
    }

    /// <summary>
    /// Creates a <code><field>name</field></code> node.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Node Field(string name)
    {
      return new Node("field", name);
    }

    /// <summary>
    /// Creates a field node with attributes <paramref name="v"/>. The
    /// dictionary should contain the field "field" which will be the
    /// value of the node.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Node Field(Dictionary<string, string> v)
    {
      string vv = "";

      if (!String.IsNullOrEmpty(v["field"])) {
        vv = v["field"];
        v.Remove("field");
      }

      return new Node("field", v, vv);
    }

    /// <summary>
    /// Creates a fields node with <code>field</code>s.
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public static Node Fields(string[] fields)
    {
      var n = new List<Node>();

      foreach (string f in fields)
        n.Add(new Node("field", f));

      return new Node("fields", n);
    }

    /// <summary>
    /// Creates a fields node with fields <paramref name="fields"/>.
    /// The <paramref name="fields"/> argument can contain strings, Nodes
    /// and Dictionary&lt;string,string&gt; objects as per how you can create fields via the
    /// <see cref="Fields"/> methods.
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public static Node Fields(List<Object> fields)
    {
      var n = new List<Node>();

      foreach (Object o in fields) {
        Type t = o.GetType();
        if (t == typeof(Node)) {
          n.Add((Node)o);
        }
        else if (t == typeof(Dictionary<string,string>)) {
          n.Add(Field((Dictionary<string, string>)o));
        }
        else {
          n.Add(Field(o.ToString()));
        }
      }

      return new Node("fields", n);
    }

    /// <summary>
    /// Creates a <code><exp type="type"/></code> node.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Node Exp(string type)
    {
      return new Node("exp", new Dictionary<string, string>() { { "type", type } });
    }

    /// <summary>
    /// Creates an <code><exp type="type">value</exp></code> node.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Node Exp(string type, string value)
    {
      return new Node("exp", new Dictionary<string, string>() { { "type", type } }, value);
    }

    /// <summary>
    /// Creates a <code><conditions><condition/></conditions></code> node.
    /// </summary>
    /// <param name="conds"></param>
    /// <returns></returns>
    public static Node Conditions(List<Node> conds)
    {
      return new Node("conditions", conds);
    }

    /// <summary>
    /// Creates a condition node
    /// </summary>
    /// <param name="attr"></param>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static Node Condition(Dictionary<string, string> attr, List<Node> exp)
    {
      return new Node("condition", attr, exp);
    }

    /// <summary>
    /// Creates a condition node in the form of
    /// <code>
    ///   <condition operator="=">
    ///     <exp type="field">field</exp>
    ///     <exp type="string">value</exp>
    ///   </condition>
    /// </code>
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Node Condition(string field, string value)
    {
      return Condition(field, value, "string", "=");
    }

    /// <summary>
    /// Creates a condition node if the form of
    /// <code>
    ///   <condition operator="=">
    ///     <exp type="field">field</exp>
    ///     <exp type="datatype">value</exp>
    ///   </condition>
    /// </code>
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <param name="datatype"></param>
    /// <returns></returns>
    public static Node Condition(string field, string value, string datatype)
    {
      return Condition(field, value, datatype, "=");
    }

    /// <summary>
    /// Creates a condition node in the form of
    /// <code>
    ///   <condition operator="operator">
    ///     <exp type="field">field</exp>
    ///     <exp type="datatype">value</exp>
    ///   </condition>
    /// </code>
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <param name="datatype"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    public static Node Condition(string field, string value, string datatype, string op)
    {
      var a = new Dictionary<string, string>();
      a.Add("operator", op);
      return new Node("condition", a, new List<Node>() {
        Exp("field", field),
        Exp(datatype, value)
      });
    }
  }

  /// <summary>
  /// A class for building and parsing Lime XML
  /// </summary>
  public class Node
  {
    /// <summary>
    /// Node attributes
    /// </summary>
    public Dictionary<string,string> Attributes { get; set; }
    /// <summary>
    /// Node value
    /// </summary>
    public Object Value { get; set; }
    /// <summary>
    /// Node name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Parse the XML into a Node object
    /// </summary>
    /// <param name="xml"></param>
    /// <returns></returns>
    public static Node Parse(string xml)
    {
      return new Node().ParseXML(xml);
    }

    /// <summary>
    /// Construct an empty Node object
    /// </summary>
    public Node() {}

    /// <summary>
    /// Construct a Node object with name <paramref name="name"/>
    /// </summary>
    /// <param name="name"></param>
    public Node(string name)
    {
      Name = name;
    }

    /// <summary>
    /// Construct a Node object with name <paramref name="name"/> and
    /// attributes <paramref name="attributes"/>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="attributes"></param>
    public Node(string name, Dictionary<string, string> attributes)
      : this(name)
    {
      Attributes = attributes;
    }

    /// <summary>
    /// Construct a Node object with name <paramref name="name"/> and
    /// value <paramref name="value"/>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public Node(string name, string value)
      : this(name)
    {
      Value = value;
    }

    /// <summary>
    /// Construct a Node object with name <paramref name="name"/> and
    /// value <paramref name="children"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="children"></param>
    public Node(string name, List<Node> children)
      : this(name)
    {
      Value = children;
    }

    /// <summary>
    /// Construct a Node object with name <paramref name="name"/>,
    /// attributes <paramref name="attributes"/> and
    /// value <paramref name="children"/>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="attributes"></param>
    /// <param name="children"></param>
    public Node(string name, Dictionary<string, string> attributes, List<Node> children)
      : this(name, attributes)
    {
      Value = children;
    }

    /// <summary>
    /// Construct a Node object with name <paramref name="name"/>,
    /// attributes <paramref name="attributes"/> and
    /// value <paramref name="value"/>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="attributes"></param>
    /// <param name="value"></param>
    public Node(string name, Dictionary<string, string> attributes, string value)
      : this(name, attributes)
    {
      Value = value;
    }

    /// <summary>
    /// Does this object have any child nodes?
    /// </summary>
    public bool HasChildren
    {
      get {
        if (Value == null) return false;
        if (Value.GetType() == typeof(List<Node>) || Value.GetType() == typeof(ArrayList)) {
          return ((ArrayList)Value).Count > 0;
        }
        return false;
      }
    }

    /// <summary>
    /// Is the value of a List type
    /// </summary>
    /// <returns></returns>
    private bool valueIsList()
    {
      Type t = Value.GetType();
      return (t == typeof(List<Node>) || t == typeof(List<>) || t == typeof(ArrayList));
    }

    /// <summary>
    /// Enumertor for the eventual child nodes
    /// </summary>
    public IEnumerable Children
    {
      get {
        if (valueIsList()) {
          var v = (ArrayList) Value;
          foreach (Object o in v)
            yield return o;
        }

        yield break;
      }
    }

    /// <summary>
    /// Parse the <paramref name="xml"/> into a Node object
    /// </summary>
    /// <param name="xml"></param>
    /// <returns>The object being called</returns>
    public Node ParseXML(string xml)
    {
      XElement root = XElement.Parse(xml);
      ParseElement(root);
      return this;
    }

    /// <summary>
    /// Low parsing method. Called recursively. Consider internal.
    /// </summary>
    /// <param name="el"></param>
    /// <returns></returns>
    public Node ParseElement(XElement el)
    {
      Name = el.Name.LocalName;
      if (el.HasAttributes) {
        Attributes = xattr2attr(el.Attributes());
      }

      if (el.HasElements) {
        var children = el.Elements();
        for (int i = 0; i < children.Count(); i++) {
          var child = children.ElementAt(i);

          if (Value == null)
            Value = new ArrayList();

          ((ArrayList)Value).Add(new Node().ParseElement(child));
        }
      }
      else {
        string ev = el.Value.ToString();
        if (!string.IsNullOrWhiteSpace(ev))
          Value = ev;
      }

      return this;
    }

    /// <summary>
    /// To human readable XML
    /// </summary>
    /// <returns></returns>
    public string ToHumanReadableString()
    {
      string s = ToString();
      XDocument doc = XDocument.Parse(s);
      return doc.ToString();
    }

    /// <summary>
    /// Converts the object to XML
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("<" + Name + attr2str());

      if (Value == null) {
        sb.Append("/>");
      }
      else if (Value.GetType() == typeof(ArrayList)) {
        var li = (ArrayList) Value;
        if (li.Count == 0) {
          sb.Append("/>");
        }
        else {
          sb.Append(">");

          foreach (Object o in li) {
            sb.Append(o.ToString());
          }

          sb.Append("</" + Name + ">");
        }
      }
      else if (valueIsList()) {
        var li = (List<Node>) Value;
        if (li.Count == 0) {
          sb.Append("/>");
        }
        else {
          sb.Append(">");

          foreach (Object o in li) {
            sb.Append(o.ToString());
          }

          sb.Append("</" + Name + ">");
        }
      }
      else {
        String v = Value.ToString();
        if (string.IsNullOrEmpty(v.Trim())) {
          sb.Append("/>");
        }
        else {

          sb.Append(">" + quoteXml(v) + "</" + Name + ">");
        }
      }

      return sb.ToString();
    }

    /// <summary>
    /// Quote text.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    private string quoteXml(string v)
    {
      var o = new XElement("x", v);
      return o.Value.ToString();
    }

    /// <summary>
    /// Attributes to string
    /// </summary>
    /// <returns></returns>
    private string attr2str()
    {
      if (Attributes != null) {
        string ret = "";

        foreach (var x in Attributes) {
          ret += " " + x.Key + "=\"" + quoteXml(x.Value) + "\"";
        }

        return ret;
      }

      return "";
    }

    /// <summary>
    /// XAttribute to Dictionary converter
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    private Dictionary<string, string> xattr2attr(IEnumerable<XAttribute> a)
    {
      var d = new Dictionary<string, string>();

      for (int i = 0; i < a.Count(); i++) {
        var ae = a.ElementAt(i);
        d.Add(ae.Name.ToString(), ae.Value.ToString());
      }

      return d;
    }
  }
}
