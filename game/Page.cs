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

      public double Score { get; private set; }
      public ReactionArrow ReactionArrow { get; private set; }

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
      public Dictionary<string, Setting> Settings { get; private set; }

      // Stack of return merge locations.
      public Stack<Unit> NextTargetUnitOnReturn { get; private set; }

      // This is the body of the text on the screen.
      public string ActionText { get; private set; }

      // The keys are the reaction texts that appear below the action text. The reaction arrow data is used by the game to transition to the next unit.
      public Dictionary<string, ScoredReactionArrow> Reactions { get; private set; }

      public Page(
         string actionText,
         Dictionary<string, ScoredReactionArrow> reactions,
         Dictionary<string, Setting> settings,
         Stack<Unit> nextTargetUnitOnReturn)
      {
         ActionText = actionText;
         Reactions = reactions;
         Settings = settings;
         NextTargetUnitOnReturn = nextTargetUnitOnReturn;
      }
   }
}
