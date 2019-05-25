using System.Collections.Generic;

namespace Gamebook
{
   public class Action
   {
      public SequenceObject Sequence;
      public List<Arrow> Arrows = new List<Arrow>();
   }

   public class Arrow
   {
      public Action TargetAction;
      public SequenceObject Sequence;
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
