﻿namespace Game
{
  public class Token
  {
    public static TokenType EndOfSourceText = new TokenType("end of source text");
    public static TokenType Text = new TokenType("text");
    public static TokenType Id = new TokenType("an identifier");
    public static TokenType End = new TokenType("'end'");
    public static TokenType If = new TokenType("'if'");
    public static TokenType Else = new TokenType("'else'");
    public static TokenType Or = new TokenType("'or'");
    public static TokenType Not = new TokenType("'not'");
    public static TokenType Equal = new TokenType("an equal sign");
    public static TokenType Lower = new TokenType("'lower'");
    public static TokenType Raise = new TokenType("'raise'");
    public static TokenType Set = new TokenType("'set'");

    public TokenType Type { get; set; }
    public string Value { get; set; }
    public int LineNumber { get; set; }

    public Token(
      TokenType type,
      string value,
      int lineNumber)
    {
      Type = type;
      Value = value;
      LineNumber = lineNumber;
    }

    public override string ToString()
    {
      string result = Type.Name;
      if (Type == Id || Type == Text)
      {
        result += " '" + Value + "'";
      }
      return result;
    }
  }
}
