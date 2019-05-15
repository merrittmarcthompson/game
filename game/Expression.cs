using System.Collections.Generic;

namespace Game
{
   public class Expression
   {
      // This represents:
      //    tvOn
      //    not tvOn
      //    tvOn=mr_rogers
      public string LeftId = null;
      public string RightId = null;
      public bool Not = false;

      public override string ToString()
      {
         string result ="";
         if (Not)
         {
            result += "not ";
         }
         result += LeftId;
         if (RightId != null)
         {
            result += "=" + RightId;
         }
         return result;
      }
   }
}
