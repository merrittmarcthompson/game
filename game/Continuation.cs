using System.Collections.Generic;

namespace Game
{
   public class Continuation
   {
      // A continuation is a point where a story can be continued. This can either be one of the starting nodes of a story, or a node you have reached in the middle of a story.
      public Dictionary<string, object> Variables = new Dictionary<string, object>();
      public string NodeName;
      public bool IsStart;
   }
}
