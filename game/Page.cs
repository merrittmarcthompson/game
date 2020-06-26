#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamebook
{
   public class ScoredReactionArrow
   {
      // This is just part of the Page Reactions interface.

      public double Score { get; }
      public ReactionArrow ReactionArrow { get; }

      public ScoredReactionArrow(
         double score,
         ReactionArrow reactionArrow)
      {
         Score = score;
         ReactionArrow = reactionArrow;
      }
   }

   // Page is a finished page, ready for display, plus all the information necessary to create the next page.
   public class Page
   {
      // The settings contain the state of the game: where you are, what people think of you, etc.
      public Dictionary<string, Setting> Settings { get; }

      // Stack of return merge locations.
      public Stack<Node> NextTargetNodeOnReturn { get; }

      // This is the body of the text on the screen.
      public string ActionText { get; }

      // The keys are the reaction texts that appear below the action text. The reaction arrow data is used by the game to transition to the next node.
      public Dictionary<string, ScoredReactionArrow> Reactions { get; }

      public Page(
         string actionText,
         Dictionary<string, ScoredReactionArrow> reactions,
         Dictionary<string, Setting> settings,
         Stack<Node> nextTargetNodeOnReturn)
      {
         ActionText = actionText;
         Reactions = reactions;
         Settings = settings;
         NextTargetNodeOnReturn = nextTargetNodeOnReturn;
      }
   }
}
