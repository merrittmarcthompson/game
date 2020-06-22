#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamebook
{
   public class TokenType
   {
      public string Name { get; private set; }

      // private: the static token types below are the only ones that can be created.
      private TokenType(
        string name)
      {
         Name = name;
      }

      public override string ToString()
      {
         return Name;
      }

      // Here are all the token types:
      public static TokenType EndOfSourceText { get; private set; } = new TokenType("end of source text");

      public static TokenType Id { get; private set; } = new TokenType("an identifier");
      public static TokenType ScoreId { get; private set; } = new TokenType("a score identifier");
      public static TokenType StringId { get; private set; } = new TokenType("a string identifier");
      public static TokenType BooleanId { get; private set; } = new TokenType("a flag identifier");
      public static TokenType SpecialId { get; private set; } = new TokenType("a special identifier");

      public static TokenType Characters { get; private set; } = new TokenType("characters");

      public static TokenType End { get; private set; } = new TokenType("'end'");
      public static TokenType Else { get; private set; } = new TokenType("'else'");
      public static TokenType If { get; private set; } = new TokenType("'if'");
      public static TokenType Not { get; private set; } = new TokenType("'not'");
      public static TokenType Merge { get; private set; } = new TokenType("'merge'");
      public static TokenType Or { get; private set; } = new TokenType("'or'");
      public static TokenType Return { get; private set; } = new TokenType("'return'");
      public static TokenType Scene { get; private set; } = new TokenType("'scene'");
      public static TokenType Set { get; private set; } = new TokenType("'set'");
      public static TokenType Score { get; private set; } = new TokenType("'score'");
      public static TokenType Sort { get; private set; } = new TokenType("'sort'");
      public static TokenType Text { get; private set; } = new TokenType("'text'");
      public static TokenType When { get; private set; } = new TokenType("'when'");

      public static TokenType Comma { get; private set; } = new TokenType("a comma");
      public static TokenType Equal { get; private set; } = new TokenType("an equal sign");
      public static TokenType Period { get; private set; } = new TokenType("a period");
   }
}
