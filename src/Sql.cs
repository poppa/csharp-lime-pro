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
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;

namespace Lime.Sql
{
  /// <summary>
  /// Class for converting an SQL query into a Lime XML query
  /// </summary>
  public class Parser
  {
    /// <summary>
    /// Parse SQL to Xml.Node
    /// </summary>
    /// <param name="sql"></param>
    /// <returns></returns>
    public static Xml.Node ParseSQL(string sql)
    {
      return new Parser().Parse(sql);
    }

    /// <summary>
    /// SQL/Lime keywords
    /// </summary>
    public static HashSet<string> Keywords {
      get { return _keywords; }
    }
    private static HashSet<string> _keywords = new HashSet<string> {
      "select", "distinct", "from", "where", "limit",
      "count", "order", "by", "asc", "desc"
    };

    /// <summary>
    /// SQL/Lime operators
    /// </summary>
    public static HashSet<string> Operators
    {
      get { return _operators; }
    } 
    private static HashSet<string> _operators = new HashSet<string> {
      "!", "=", "!=", "<", ">", ">=", "<=", 
      "is", "like", "and", "or", "in", "not", "any", "all"
    };

    /// <summary>
    /// If <paramref name="word"/> a keyword=
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    public static bool IsKeyword(string word)
    {
      return _keywords.Contains(word.ToLower());
    }

    /// <summary>
    /// Is <paramref name="op"/> an operator
    /// </summary>
    /// <param name="op"></param>
    /// <returns></returns>
    public static bool IsOperator(string op)
    {
      return _operators.Contains(op.ToLower());
    }
  
    /// <summary>
    /// Parse the <paramref name="sql"/> query
    /// </summary>
    /// <param name="sql"></param>
    /// <returns></returns>
    public Xml.Node Parse(string sql)
    {
      var stokens = split(sql);
      var tokens = tokenize(stokens);
      tokens.Add(new Token(null));

      int pos = 0;

      string table = null;
      int sortCount = 0;
      string sortOrder = null;
      var fields = new List<Object>();
      var conds = new List<Xml.Node>();
      var qattr = new Dictionary<string, string>();
      var sort = new Dictionary<string, string>();
      Token andor = null;

      while (true) {
        Token t = tokens[pos];
        if (t.Type == Token.TokType.NONE) {
          break;
        }

        if (t.IsA(Token.TokType.COLUMN)) {
          while (t.IsA(Token.TokType.COLUMN)) {
            fields.Add(Xml.Builder.Field(t.Value));
            t = tokens[++pos];
          }
        }
        else if (t.IsA(Token.TokType.TABLE)) {
          table = t.Value;
        }
        else if (t.IsA(Token.TokType.LIMIT_TO)) {
          qattr["top"] = t.Value;
        }
        else if (t.IsA(Token.TokType.LIMIT_FROM)) {
          qattr["first"] = t.Value;
        }
        else if (t.IsA(Token.TokType.KEYWORD) && t.Lveq("distinct")) {
          qattr["distinct"] = "1";
        }
        else if (t.IsA(Token.TokType.COUNT)) {
          qattr["count"] = t.Value;
        }
        else if (t.IsA(Token.TokType.SORT_KEY)) {
          sort[t.Value] = Convert.ToString(++sortCount);
        }
        else if (t.IsA(Token.TokType.SORT_ORDER)) {
          sortOrder = t.Value;
        }
        else if (t.IsA(Token.TokType.OPERATOR)) {
          andor = t;
          pos++;
          continue;
        }
        else if (t.IsA(Token.TokType.PREDICATE)) {
          Token op = tokens[++pos];
          // if the next token also is an operator, we're dealing with
          // something like NOT IN
          Token op2 = tokens[pos + 1];

          string opval = op.Value;

          if (op2.IsA(Token.TokType.OPERATOR)) {
            opval += " " + op2.Value;
            pos++;
          }

          Token val = tokens[++pos];

          if (!val.IsA(Token.TokType.VALUE)) {
            throw new Exception("Expected a value token but got " + val);
          }

          string valval = val.Value;

          if (op.Lveq("like") && !string.IsNullOrEmpty(valval)) {
            if (valval[0] == '%') {
              opval = "%" + opval;
              valval = valval.Substring(1);
            }

            if (valval[-1] == '%') {
              opval += "%";
              valval = valval.Substring(0, valval.Length - 1);
            }
          }

          var attr = new Dictionary<string, string>();
          attr["operator"] = opval;

          if (tokens[pos + 1].IsA(Token.TokType.TYPEHINT)) {
            val.DataType = tokens[pos + 1].Value;
            pos++;
          }

          if (andor != null && andor.Lveq("or"))
            attr["or"] = "1";

          var cn = new List<Xml.Node>();
          cn.Add(Xml.Builder.Exp("field", t.Value));
          cn.Add(Xml.Builder.Exp(val.DataType, valval));
          conds.Add(Xml.Builder.Condition(attr, cn));
        }
        else if (t.IsA(Token.TokType.GROUP_START) || t.IsA(Token.TokType.GROUP_END)) {
          var a = new Dictionary<string, string>();
          if (andor != null && andor.Lveq("or"))
            a["or"] = "1";

          conds.Add(Xml.Builder.Condition(a, new List<Xml.Node>() { Xml.Builder.Exp(t.Value) }));
        }

        andor = null;
        pos++;
      }

      if (table == null)
        throw new Exception("No table name given in SQL query!");

      if (!qattr.ContainsKey("top") && qattr.ContainsKey("first")) {
        qattr["top"] = qattr["first"];
        qattr.Remove("first");
      }

      var q = new List<Xml.Node>();

      q.Add(Xml.Builder.Table(table));

      if (conds.Count > 0)
        q.Add(Xml.Builder.Conditions(conds));

      if (fields.Count > 0) {
        if (sort.Count > 0) {
          if (sortOrder == null)
            sortOrder = "ASC";

          for (int i = 0; i < fields.Count; i++) {
            var n = (Xml.Node)fields[i];
            if (sort.ContainsKey(n.Value.ToString())) {
              var a = new Dictionary<string, string>();
              a.Add("field", n.Value.ToString());
              a.Add("sortorder", sortOrder);
              a.Add("sortindex", sort[n.Value.ToString()]);
              fields[i] = a;
            }

            i++;
          }
        }

        q.Add(Xml.Builder.Fields(fields));
      }

      return Xml.Builder.Query(q, qattr);
    }

    /// <summary>
    /// Tokenize the list of words
    /// </summary>
    /// <param name="words"></param>
    /// <returns></returns>
    private List<Token> tokenize(List<string> words)
    {
      words.Add(null); // sentinel at end
      words.Insert(0, null); // sentinel at start

      var tokens = new List<Token>();
      tokens.Add(new Token(null));

      int pos = 1;

      while (true) {
        string word = words[pos];
        if (word == null) {
          tokens.RemoveAt(0);
          return tokens;
        }

        Token t = new Token(word);
        Token p = tokens[pos - 1];

        if (t.IsA(Token.TokType.NONE)) {
          if (p.IsA(Token.TokType.COLUMN) || (
              p.IsA(Token.TokType.KEYWORD) &&
              p.Lveq("select", "distinct", "count"))) 
          {
            t.Type = Token.TokType.COLUMN;
          }
          else if (p.IsA(Token.TokType.KEYWORD) && p.Lveq("from")) {
            t.Type = Token.TokType.TABLE;
          }
          else if ((p.IsA(Token.TokType.KEYWORD) && p.Lveq("where")) ||
                   (p.IsA(Token.TokType.OPERATOR) && p.Lveq("and", "or")) ||
                   p.IsA(Token.TokType.GROUP_START)) 
          {
            t.Type = Token.TokType.PREDICATE;
          }
          else if (p.IsA(Token.TokType.OPERATOR)) {
            t.Type = Token.TokType.VALUE;
            t.ResolveDataType();
          }
          else if (p.IsA(Token.TokType.LIMIT)) {
            t.Type = Token.TokType.LIMIT_FROM;
          }
          else if (p.IsA(Token.TokType.LIMIT_FROM)) {
            t.Type = Token.TokType.LIMIT_TO;
          }
          else if (p.IsA(Token.TokType.BY) || p.IsA(Token.TokType.SORT_KEY)) {
            t.Type |= Token.TokType.SORT_KEY;
          }
          else {
            throw new Exception(String.Format("Unresolved token type {0}. Previous token was {1}",
                                              t, p));
          }
        }

        tokens.Add(t);
        pos += 1;
      }
    }

    /// <summary>
    /// Split the query <paramref name="s"/> into string tokens
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private List<string> split(string s)
    {
      s += "\0"; // sentinel
      var ret = new List<string>();

      int pos = 0;
      while (true) {
        int start = pos;
        char c = s[pos];
        switch (c) {
          case '\0': 
            return ret;

          case '\r':
            pos += 1;
            if (s[pos] == '\n') 
              pos += 1;
            continue;

          case '\n':
            pos += 1;
            continue;

          case ' ':
          case '\t':
          case ',':
            while (s[++pos] == '\t' || s[pos] == ' ')
              ;
            continue;

          case '`':
          case '\'':
          case '"':
            pos += 1;
            while (true) {
              char d = s[pos];
              if (d == '\0')
                throw new Exception("Unterminated string literal");

              if (d == c) {
                if (s[pos - 1] != '\\') {
                  pos += 1;
                  break;
                }
              }

              pos += 1;
            }
            break;

          case '!':
          case '<':
          case '>':
            if (s[pos + 1] == '=')
              pos += 2;
            break;

          /*
          Range a..Z, 0..9 and % and :
          */
          case 'a': case 'b': case 'c': case 'd': case 'e': case 'f': case 'g':
          case 'h': case 'i': case 'j': case 'k': case 'l': case 'm': case 'n':
          case 'o': case 'p': case 'q': case 'r': case 's': case 't': case 'u':
          case 'v': case 'w': case 'x': case 'y': case 'z': case 'A': case 'B':
          case 'C': case 'D': case 'E': case 'F': case 'G': case 'H': case 'I':
          case 'J': case 'K': case 'L': case 'M': case 'N': case 'O': case 'P':
          case 'Q': case 'R': case 'S': case 'T': case 'U': case 'V': case 'W':
          case 'X': case 'Y': case 'Z': case '0': case '1': case '2': case '3':
          case '4': case '5': case '6': case '7': case '8': case '9': case '%':
          case ':':
            pos += 1;
            while (true) {
              switch (s[pos]) {
                case '\0':
                  goto innerdone;
                /*
                Range a..Z, 0..9 and % and '.'
                */
                case 'a': case 'b': case 'c': case 'd': case 'e': case 'f':
                case 'g': case 'h': case 'i': case 'j': case 'k': case 'l':
                case 'm': case 'n': case 'o': case 'p': case 'q': case 'r':
                case 's': case 't': case 'u': case 'v': case 'w': case 'x':
                case 'y': case 'z': case 'A': case 'B': case 'C': case 'D':
                case 'E': case 'F': case 'G': case 'H': case 'I': case 'J':
                case 'K': case 'L': case 'M': case 'N': case 'O': case 'P':
                case 'Q': case 'R': case 'S': case 'T': case 'U': case 'V':
                case 'W': case 'X': case 'Y': case 'Z': case '0': case '1':
                case '2': case '3': case '4': case '5': case '6': case '7':
                case '8': case '9': case '%': case '.':
                  pos += 1;
                  continue;
              }
              innerdone:
              break;
            }
        
            break;

          default:
            pos += 1;
            break;
        }

        string tmp = s.Substring(start, pos-start);
        //Console.WriteLine("Word: {0}", tmp);
        ret.Add(tmp);
      }
    }
  }

  /// <summary>
  /// SQL token
  /// </summary>
  internal class Token
  {
    /// <summary>
    /// Token types
    /// </summary>
    public enum TokType
    {
      NONE = 0,
      KEYWORD = 1 << 0,
      OPERATOR = 1 << 1,
      VALUE = 1 << 2,
      COLUMN = 1 << 3,
      PREDICATE = 1 << 4,
      TABLE = 1 << 5,
      GROUP_START = 1 << 6,
      GROUP_END = 1 << 7,
      LIMIT = 1 << 8,
      LIMIT_FROM = 1 << 9,
      LIMIT_TO = 1 << 10,
      COUNT = 1 << 11,
      SELECT = 1 << 12,
      ORDER = 1 << 13,
      BY = 1 << 14,
      SORT_ORDER = 1 << 15,
      ORDER_ASC = 1 << 16,
      ORDER_DESC = 1 << 17,
      SORT_KEY = 1 << 18,
      TYPEHINT = 1 << 19
    }

    /// <summary>
    /// Type of token
    /// </summary>
    public TokType Type
    {
      get { return _type; }
      set {
        if ((_type & TokType.NONE) == TokType.NONE)
          _type &= ~TokType.NONE;

        _type = value;
      }
    }
    private TokType _type = TokType.NONE;

    /// <summary>
    /// Token value
    /// </summary>
    public string Value { get; set; }
    /// <summary>
    /// Token lower case value
    /// </summary>
    string lv { get; set; }
    /// <summary>
    /// Datatype of value tokens
    /// </summary>
    public string DataType { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="v"></param>
    public Token(string v)
    {
      if (string.IsNullOrEmpty(v))
        return;

      lv = v.ToLower();

      if (Parser.IsKeyword(lv)) {
        Type = TokType.KEYWORD;

        if (lv == "limit")
          Type |= TokType.LIMIT;
        else if (lv == "select")
          Type |= TokType.SELECT;
        else if (lv == "count")
          Type |= TokType.COUNT;
        else if (lv == "order")
          Type |= TokType.ORDER;
        else if (lv == "by")
          Type |= TokType.BY;
        else if (lv == "asc")
          Type |= TokType.ORDER_ASC | TokType.SORT_ORDER;
        else if (lv == "desc")
          Type |= TokType.ORDER_DESC | TokType.SORT_ORDER;
      }
      else if (Parser.IsOperator(lv))
        Type = TokType.OPERATOR;
      else if (lv[0] == ':') {
        Type = TokType.TYPEHINT;
        v = v.Substring(1);
      }
      else if (lv[0] == '\'' || lv[0] == '"') {
        Type = TokType.VALUE;
        v = v.Substring(1, v.Length - 2);
        DataType = "string";

        Match m = Regex.Match(v, @"\d\d\d\d-\d\d-\d\d");

        if (m.Success)
          DataType = "date";
      }
      else if (lv[0] == '(')
        Type = TokType.GROUP_START;
      else if (lv[0] == ')')
        Type = TokType.GROUP_END;
      else if (lv[0] == '`')
        v = v.Substring(1, v.Length - 2);

      Value = v;
      lv = v.ToLower();
    }

    /// <summary>
    /// Lower case value equals any of <paramref name="rest"/>?
    /// </summary>
    /// <param name="rest"></param>
    /// <returns></returns>
    public bool Lveq(params string[] rest)
    {
      foreach (string v in rest) {
        if (v.ToLower() == lv)
          return true;
      }

      return false;
    }

    /// <summary>
    /// Is it a token of type <paramref name="t"/>?
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool IsA(TokType t)
    {
      if (t == TokType.NONE) return _type == t;
      return (_type & t) == t;
    }

    /// <summary>
    /// Resolve the datatype of of the token (if it's a value token
    /// </summary>
    public void ResolveDataType()
    {
      if (DataType == null && Value != null) {
        Match m = Regex.Match(Value, @"\d\d\d\d-\d\d-\d\d");

        if (m.Success)
          DataType = "date";
        else
          DataType = "numeric";
      }
    }

    /// <summary>
    /// To string converter
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return String.Format("Lime.Sql.Token(\"{0}\", {1})", Value, Type);
    }
  }
}
