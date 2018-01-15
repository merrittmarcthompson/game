using System;

namespace Game
{
   public static partial class Transform
   {
      // ex. change "hello" to “hello”.
      public static string VerticalToMatchingQuotes(
        string text)
      {
         var result = "";
         var testText = " " + text;
         for (int i = 1; i < testText.Length; ++i)
         {
            var letter = testText[i];
            if (letter == '"')
            {
               if (testText[i - 1] == ' ')
               {
                  result += '“';
               }
               else
               {
                  result += '”';
               }
            }
            else
            {
               result += letter;
            }
         }
         return result;
      }
   }
}
