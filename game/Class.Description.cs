using System.Collections.Generic;

namespace Game
{
   public class Description
   {
      public class Reaction
      {
         public string Text;
         public string ArrowName;
      }

      public string Text;
      public Continuation Continuation;
      public List<Reaction> Reactions = new List<Reaction>();
   }
}
