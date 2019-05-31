using System.Collections.Generic;

namespace Gamebook
{
   //  All of the graphml source code gets converted into this data structure.

   public class Action
   {
      public SequenceOperation Sequence;
      public List<Arrow> Arrows = new List<Arrow>();
   }

   public class Arrow
   {
      public Action TargetAction;
      public SequenceOperation Sequence;
   }

   public class MergeArrow: Arrow
   {
      public Action TargetSceneAction;
      public string DebugSceneId;
      public string DebugSourceName;
   }

   public class ReactionArrow: Arrow
   {
   }
}
