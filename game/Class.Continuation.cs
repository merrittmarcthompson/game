using System.Collections.Generic;

namespace Game
{
   public class Continuation
   {
      // A continuation is a point where a story can be continued. This can either be one of the starting nodes of a story, or a node you have reached in the middle of a story.
      public Dictionary<string, string> Variables = new Dictionary<string, string>();
      public string NodeName;
      public bool IsStart;
      public List<Reaction> Reactions = new List<Reaction>();
   }
}
