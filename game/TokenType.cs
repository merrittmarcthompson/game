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
      public string Name { get; }

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
      public static TokenType EndOfSourceText { get; } = new TokenType("end of source text");

      public static TokenType Id { get; } = new TokenType("an identifier");
      public static TokenType ScoreId { get; } = new TokenType("a score identifier");
      public static TokenType StringId { get; } = new TokenType("a string identifier");
      public static TokenType BooleanId { get; } = new TokenType("a flag identifier");
      public static TokenType SpecialId { get; } = new TokenType("a special identifier");

      public static TokenType Characters { get; } = new TokenType("characters");

      public static TokenType End { get; } = new TokenType("'end'");
      public static TokenType Else { get; } = new TokenType("'else'");
      public static TokenType If { get; } = new TokenType("'if'");
      public static TokenType Not { get; } = new TokenType("'not'");
      public static TokenType Merge { get; } = new TokenType("'merge'");
      public static TokenType Or { get; } = new TokenType("'or'");
      public static TokenType Return { get; } = new TokenType("'return'");
      public static TokenType Scene { get; } = new TokenType("'scene'");
      public static TokenType Set { get; } = new TokenType("'set'");
      public static TokenType Score { get; } = new TokenType("'score'");
      public static TokenType Sort { get; } = new TokenType("'sort'");
      public static TokenType Text { get; } = new TokenType("'text'");
      public static TokenType When { get; } = new TokenType("'when'");

      public static TokenType Comma { get; } = new TokenType("a comma");
      public static TokenType Equal { get; } = new TokenType("an equal sign");
      public static TokenType Period { get; } = new TokenType("a period");
   }
}
