#nullable enable
using System;
using System.Collections.Generic;

namespace Gamebook
{
   public class Token
   {
      public TokenType Type { get; private set; }
      public string Value { get; private set; }
      public int LineNumber { get; private set; }

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
         if (Type == TokenType.Id || Type == TokenType.Characters)
            result += " '" + Value + "'";
         return result;
      }
   }
}
