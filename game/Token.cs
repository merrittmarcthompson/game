namespace Game
{
   public class Token
   {
      public static TokenType EndOfSourceText = new TokenType("end of source text");

      public static TokenType Id = new TokenType("an identifier");
      public static TokenType Special = new TokenType("a special identifier");
      public static TokenType Characters = new TokenType("characters");

      public static TokenType End = new TokenType("'end'");
      public static TokenType Else = new TokenType("'else'");
      public static TokenType If = new TokenType("'if'");
      public static TokenType Score = new TokenType("'score'");
      public static TokenType Not = new TokenType("'not'");
      public static TokenType Or = new TokenType("'or'");
      public static TokenType Merge = new TokenType("'merge'");
      public static TokenType Name = new TokenType("'name'");
      public static TokenType Set = new TokenType("'set'");
      public static TokenType Text = new TokenType("'text'");
      public static TokenType When = new TokenType("'when'");

      public static TokenType Comma = new TokenType("a comma");
      public static TokenType Equal = new TokenType("an equal sign");
      public static TokenType Period = new TokenType("a period");

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
         if (Type == Id || Type == Characters)
         {
            result += " '" + Value + "'";
         }
         return result;
      }
   }
}
