using System;
using System.Collections.Generic;
using System.Linq;

namespace Gamebook
{
   public static class Engine
   {
      public static bool DebugMode = false;

      // State represents the state of the game. It implements undoing game choices and going back to previous game states.
      private class State
      {
         // The settings contain the state of the game: where you are, what people think of you, etc.
         public Dictionary<string, object> Settings = new Dictionary<string, object>();

         // This is the one action within a story tree that we are on right now. If it is null, we aren't in a story tree. In that case, we show a list of all the starting actions that are appropriate for the current situation.
         public Round Round = null;

         // The same story tree can apply to different characters, locations, objects, etc. As we go through a story tree, we collect what the current values of those are.
         public Dictionary<string, Round> ReactionTargetRounds = new Dictionary<string, Round>();

         // Stack of return merge locations for referential merges.
         public Stack<Round> NextTargetRoundOnReturn = new Stack<Round>();

         public State() { }

         public State(State other)
         {
            Settings = new Dictionary<string, object>(other.Settings);
            Round = other.Round;
            ReactionTargetRounds = new Dictionary<string, Round>(other.ReactionTargetRounds);
            NextTargetRoundOnReturn = new Stack<Round>(other.NextTargetRoundOnReturn);
         }
      }

      private static Stack<State> UndoStack = new Stack<State>();

      private static State Current = new State();

      public static void Undo()
      {
         if (UndoStack.Count == 0)
            return;
         Current = UndoStack.Pop();
      }

      public static bool canUndo()
      {
         return UndoStack.Count != 0;
      }

      private static string ValueString(
         object value)
      {
         if (value == null)
            Log.Fail("Value is null");
         if (value is string)
            return value as string;
         return EvaluateText(value as SequenceCode);
      }

      private static bool EvaluateConditions(
         IEnumerable<Expression> expressions,
         out string outTrace)
      {
         outTrace = "";
         foreach (var expression in expressions)
         {
            var found = Current.Settings.TryGetValue(expression.LeftId, out object leftObject);
            var leftValue = "";
            string traceLeftValue;
            if (found)
            {
               if (leftObject == null)
                  traceLeftValue = "<true>";
               else
               {
                  leftValue = ValueString(leftObject);
                  traceLeftValue = leftValue;
               }
            }
            else
               traceLeftValue = "<false>";
            if (found == expression.Not)
            {
               if (DebugMode)
                  outTrace += "@`" + (expression.Not ? "not " : "") + expression.LeftId + "(" + traceLeftValue + ")? <fail>`";
               return false;
            }
            string traceEqualRight = "";
            if (expression.RightId != null)
            {
               traceEqualRight = "=" + expression.RightId;
               if (leftValue == expression.RightId == expression.Not)
               {
                  if (DebugMode)
                     outTrace += "@`" + (expression.Not ? "not " : "") + expression.LeftId + "(" + traceLeftValue + ")" + traceEqualRight + "? <fail>`";
                  return false;
               }
            }
            if (DebugMode)
               outTrace += "@`" + (expression.Not ? "not " : "") + expression.LeftId + "(" + traceLeftValue + ")" + traceEqualRight + "?`";
         }
         return true;
      }

      private static bool EvaluateCondition(
         Code topCode,
         out string outTrace)
      {
         string trace = "";
         // When there are no 'when' directives, it always succeeds.
         var allSucceeded = true;
         topCode.Traverse((code) =>
         {
            if (!(code is WhenCode whenCode))
               return true;
            if (!EvaluateConditions(whenCode.GetExpressions(), out trace))
               allSucceeded = false;
            return true;
         });
         outTrace = trace;
         return allSucceeded;
      }

      public static void Set(
         string id,
         string value)
      {
         Current.Settings[id] = value;
      }

      public static string Get(
         string id)
      {
         if (Current.Settings.TryGetValue(id, out object value))
            return ValueString(value);
         return "";
      }

      private static string GetSpecialText(
         string specialId)
      {
         if (specialId == "John" || specialId == "Jane")
         {
            if (Current.Settings.TryGetValue("jane", out object value))
               return ValueString(value);
            Log.Fail("No value for 'Jane' or 'John'");
         }
         else if (specialId == "Smith")
         {
            if (Current.Settings.TryGetValue("smith", out object value))
               return ValueString(value);
            Log.Fail("No value for 'Smith'");
         }
         else
         {
            bool heroIsMale = Current.Settings.ContainsKey("male");
            if (specialId == "he" || specialId == "she")
               return heroIsMale ? "he" : "she";
            else if (specialId == "He" || specialId == "She")
               return heroIsMale ? "He" : "She";
            else if (specialId == "him" || specialId == "her")
               return heroIsMale ? "him" : "her";
            else if (specialId == "Him" || specialId == "Her")
               return heroIsMale ? "Him" : "Her";
            else if (specialId == "his" || specialId == "hers")
               return heroIsMale ? "his" : "her";
            else if (specialId == "His" || specialId == "Hers")
               return heroIsMale ? "His" : "Her";
            else if (specialId == "himself" || specialId == "herself")
               return heroIsMale ? "himself" : "herself";
            else if (specialId == "Himself" || specialId == "Herself")
               return heroIsMale ? "Himself" : "Herself";
            else if (specialId == "man" || specialId == "woman")
               return heroIsMale ? "man" : "woman";
            else if (specialId == "Man" || specialId == "Woman")
               return heroIsMale ? "Man" : "Woman";
            else if (specialId == "boy" || specialId == "girl")
               return heroIsMale ? "boy" : "girl";
            else if (specialId == "Boy" || specialId == "Girl")
               return heroIsMale ? "Boy" : "Girl";
            else if (specialId == "Mr" || specialId == "Ms")
               return heroIsMale ? "Mr." : "Ms";
            else if (specialId == "Mrs")
               return heroIsMale ? "Mr." : "Mrs.";
            else
               Log.Fail(String.Format("Unknown special ID {0}.", specialId));
         }
         return "";
      }

      private static string NormalizeText(
         string text)
      {
         // Remove sequences of more than one space within text, plus remove all leading and trailing spaces. This ensures that strings with no information are zero-length.
         string fixedText = "";
         // true: skip leading spaces too.
         bool hadSpace = true;
         foreach (char letter in text)
         {
            if (letter == ' ' || letter == '\t' || letter == '\n')
            {
               if (!hadSpace)
               {
                  hadSpace = true;
                  fixedText += ' ';
               }
            }
            else
            {
               hadSpace = false;
               fixedText += letter;
            }
         }
         // Get rid of trailing spaces too.
         return fixedText.Trim();
      }

      private static string EvaluateText(
         Code value)
      {
         string accumulator = "";
         value.Traverse((code) =>
         {
            switch (code)
            {
               case CharacterCode characterCode:
                  accumulator += characterCode.Characters;
                  break;
               case SubstitutionCode substitutionCode:
                  accumulator += ValueString(substitutionCode.Id);
                  break;
               case IfCode ifCode:
                  return EvaluateConditions(ifCode.GetExpressions(), out var trace);
               case SpecialCode specialCode:
                  accumulator += GetSpecialText(specialCode.Id);
                  break;
            }
            return true;
         });
         // Always returns an empty string if there is no useful text.
         return NormalizeText(accumulator);
      }

      private static void EvaluateSettings(
         Code topCode,
         out string outTrace)
      {
         string trace = "";
         topCode.Traverse((code) =>
         {
            switch (code)
            {
               case SetCode setCode:
                  foreach (var expression in setCode.GetExpressions())
                  {
                     if (expression.Not)
                        Current.Settings.Remove(expression.LeftId);
                     else
                        // If it is [set tall], the right ID will be null.
                        Current.Settings[expression.LeftId] = expression.RightId;
                     if (DebugMode)
                        trace += "@`set " + (expression.Not ? "not " : "") + expression.LeftId + (expression.RightId != null ? "=" + expression.RightId : "") + "`";
                  }
                  break;
               case TextCode textCode:
                  Current.Settings[textCode.Id] = textCode.Text;
                  if (DebugMode)
                     trace += "@`text " + textCode.Id + "=" + textCode.Text + "`";
                  break;
            }
            return true;
         });
         outTrace = trace;
      }

      private static string FixPlus(
         string text)
      {
         // We always put in a space when we concatenate different text parts, but sometimes you don't want that, so you can put in a plus sign to stop that. Ex. "hello" joined with "there" => "hello there", but "hello+" joined with "there" => "hellothere".  Useful for things like 'He said "+' joined with "'I am a fish."' => 'He said "I am a fish."'
         string fixedText = "";
         bool addSpaces = true;
         foreach (char letter in text)
         {
            if (letter == '+')
               addSpaces = false;
            else if (letter == ' ')
            {
               if (addSpaces)
                  fixedText += ' ';
            }
            else
            {
               fixedText += letter;
               addSpaces = true;
            }
         }
         return fixedText;
      }

      private static (string, List<string>) BuildRoundText(
            Round firstRound)
      {
         // Starting with the given round box, a) merge the texts of all rounds connected below it into one text, and b) collect all the reaction arrows.
         List<string> accumulatedReactionTexts = new List<string>();

         // The action text will contain all the merged action texts.
         var accumulatedActionTexts = "";

         // Build these too.
         Current.ReactionTargetRounds = new Dictionary<string, Round>();

         // This recursive routine will accumulate all the action and reaction text values in the above variables.
         Accumulate(firstRound);

         return (FixPlus(accumulatedActionTexts), accumulatedReactionTexts);

         void Accumulate(
            Round round)
         {
            string trace;
            // First append this action box's own text and execute any settings.
            if (accumulatedActionTexts.Length == 0)
               accumulatedActionTexts = EvaluateText(round.ActionCode);
            else
               accumulatedActionTexts += " " + EvaluateText(round.ActionCode);

            EvaluateSettings(round.ActionCode, out trace);
            accumulatedActionTexts += trace;

            // Next examine all the arrows for the action.
            var arrowCount = 0;
            foreach (var arrow in round.GetArrows())
            { 
               // If conditions in the arrow are false, then just ignore the arrow completely. This includes both reaction and merge arrows.
               bool succeeded = EvaluateCondition(arrow.Code, out trace);
               accumulatedActionTexts += trace;
               if (!succeeded)
                  continue;
               ++arrowCount;
               switch (arrow)
               {
                  case MergeArrow mergeArrow:
                     // There may be 'set' parameters for a referential merge.
                     EvaluateSettings(arrow.Code, out trace);
                     accumulatedActionTexts += trace;

                     if (DebugMode)
                        accumulatedActionTexts += "@`merge" + (mergeArrow.DebugSceneId != null ? " " + mergeArrow.DebugSceneId : "") + "`";

                     // There are two kinds of merge arrows.
                     Round targetAction;
                     if (mergeArrow.TargetSceneRound != null)
                     {
                        targetAction = mergeArrow.TargetSceneRound;
                        // When we finish the jump to the other scene, we will continue merging with the action this arrow points to.
                        Current.NextTargetRoundOnReturn.Push(mergeArrow.TargetRound);
                     }
                     else
                        // It's a local merge arrow. Merge the action it points to.
                        // It should be impossible for it to have no target. Let it crash if that's the case.
                        targetAction = mergeArrow.TargetRound;
                     // Call this routine again recursively. It will append the target's text and examine the target's arrows.
                     Accumulate(targetAction);
                     break;
                  case ReactionArrow reactionArrow:
                     var reactionText = EvaluateText(reactionArrow.Code);
                     // There's a little trickiness with links here...
                     if (reactionText.Length > 0 && reactionText[0] == '{')
                     {
                        // If it's in braces, it refers to a hyperlink already in the text. Don't make a new hyperlink for it. Just take off the braces. When the user clicks on the link, it won't have braces.
                        reactionText = reactionText.Substring(1);
                        var end = reactionText.IndexOf("}");
                        if (end != -1)
                           reactionText = reactionText.Substring(0, end);
                     }
                     else
                        accumulatedReactionTexts.Add("{" + reactionText + "}");
                     Current.ReactionTargetRounds[reactionText] = reactionArrow.TargetRound;
                     break;
               }
            }
            if (arrowCount == 0)
            {
               // This is a terminal action of a scene. If this scene was referenced by another scene, we need to continue merging back in the referencing scene.
               if (Current.NextTargetRoundOnReturn.Any())
                  Accumulate(Current.NextTargetRoundOnReturn.Pop());
            }
         }
      }

      public static (string, List<string>) BuildRoundTextForReaction(
         string reactionText)
      {
         // The UI calls this to obtain a text representation of the next screen to appear.
         if (reactionText != null)
         {
            // Move to new state. Otherwise, redisplay existing state.
            UndoStack.Push(new State(Current));
            if (!Current.ReactionTargetRounds.TryGetValue(reactionText, out Current.Round))
               Log.Fail(String.Format("No arrow for reaction '{0}'", reactionText));
         }
         // Show the current action box and its reaction arrows.
         return BuildRoundText(Current.Round);
      }

      public static void Start()
      {
         Current.Round = Round.Load();
      }
   }
}
