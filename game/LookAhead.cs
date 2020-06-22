#nullable enable
using System;
using System.Collections.Generic;

namespace Gamebook
{
   public class LookAhead
   {
      private TokenList TokenList;
      private int TokenIndex = 0;
      public string Value { get; private set; }
      public TokenType Type { get; private set; }

      public LookAhead(
         TokenList tokenList)
      {
         TokenList = tokenList;
         Value = "";
         Type = TokenType.EndOfSourceText;
      }

      public bool Got(
         TokenType type)
      {
         // This should never go off the end. There is already an end of source text marker at the end of the tokens.
         var token = TokenList.At(TokenIndex++);
         if (token.Type == type)
         {
            Value = token.Value;
            Type = type;
            return true;
         }
         --TokenIndex;
         return false;
      }

      public void Require(
         TokenType expected,
         string sourceCode,
         string sourceNameForErrorMessages)
      {
         // This should never go off the end. There is already an end of source text marker at the end of the tokens.
         var actual = TokenList.At(TokenIndex++);
         if (actual.Type != expected)
            throw new InvalidOperationException(string.Format($"file {sourceNameForErrorMessages} line {actual.LineNumber}: expected {expected} but got '{actual.Value}' in\n{sourceCode}"));
         Value = actual.Value;
         Type = expected;
      }
   }

}
