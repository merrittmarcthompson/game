using System.Collections.Generic;

namespace Game
{
   public class Expression
   {
      public string LeftName { get; set; }
      public List<string> LeftLabels { get; set; } = new List<string>();
      public string RightName { get; set; }
      public List<string> RightLabels { get; set; } = new List<string>();

      public override string ToString()
      {
         // Ex. Door.isOpen=OtherDoor.isOpen
         string result;
         result = LeftName;
         foreach (var label in LeftLabels)
         {
            result += "." + label;
         }
         result += "=" + RightName;
         foreach (var label in RightLabels)
         {
            result += "." + label;
         }
         return result;
      }
   }
}
