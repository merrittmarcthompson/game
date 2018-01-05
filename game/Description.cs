using System.Collections.Generic;

namespace Game
{
   public class Description
   {
      public class Option
      {
         public string Text;
         public string ArrowName;
         public Continuation Continuation;
      }

      public string Text = "";
      public List<Option> Options = new List<Option>();
   }
}
