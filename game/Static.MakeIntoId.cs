using System;

namespace Game
{
   public static partial class Static
   {
      // ex. change "map.boneyard-simplified" into "map_boneyard_simplified".
      public static string MakeIntoId(
        string text)
      {
         var result = "";
         foreach (var letter in text)
         {
            if (Char.IsLetterOrDigit(letter) || letter == '_')
            {
               result += letter;
            }
            else
            {
               result += '_';
            }
         }
         return result;
      }
   }
}