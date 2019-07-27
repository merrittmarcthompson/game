using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamebook
{
   // To save the game, we just serialize this whole class.
   [JsonObject(MemberSerialization.OptOut)]
   public class Game
   {
      public class Page
      {
         public string ActionText { get; private set; }
         public List<string> ReactionTexts { get; private set; }

         public Page(
            string actionText,
            List<string> reactionTexts)
         {
            ActionText = actionText;
            ReactionTexts = reactionTexts;
         }
      }

      // State represents the state of the game. It implements undoing game choices and going back to previous game states. A state is a finished page, ready for display.
      private class State
      {
         // The settings contain the state of the game: where you are, what people think of you, etc.
         public Dictionary<string, Setting> Settings = new Dictionary<string, Setting>();

         // Stack of return merge locations for referential merges.
         public Stack<Unit> NextTargetUnitOnReturn = new Stack<Unit>();

         // A character version of the rendered page that appears on the screen. This value is set on Game construction.
         public Page Page;

         // This relates the user choice of reactions to the next units that are associated with them. This is what gets us to the next state.
         public Dictionary<string, ReactionArrow> ReactionArrowsByLink = new Dictionary<string, ReactionArrow>();

         public State() { }

         public State(State other)
         {
            // Make a copy of the other state which is completely detached and independent.
            Settings = new Dictionary<string, Setting>(other.Settings);

            // Don't forget to reverse due to C# stack enumeration bug.
            NextTargetUnitOnReturn = new Stack<Unit>(other.NextTargetUnitOnReturn.Reverse());

            ReactionArrowsByLink = new Dictionary<string, ReactionArrow>(other.ReactionArrowsByLink);

            // Don't need to duplicate this; it's immutable.
            Page = other.Page;
         }
      }

      // VARIABLES

      // The current state of the game: what unit we are on, what are the current settings, etc.
      // Have to add JsonProperty for private members.
      [JsonProperty] private State Current = new State();

      // Pop back to old states to implement undo.
      [JsonProperty] private Stack<State> UndoStack = new Stack<State>();

      // Add annotations about how merges were done, etc.
      public bool DebugMode = false;

      public const char NegativeDebugTextStart = '\u0001';
      public const char PositiveDebugTextStart = '\u0002';
      public const char DebugTextStop = '\u0003';

      // FUNCTIONS

      public Game(
         Unit firstUnitInGame)
      {
         // The game constructs its first page right away, ready to go. The game is always in a valid state, that is, it contains a completed page, ready to display. When we serialize and deserialize a game, we always save and restore a valid state.
         BuildPage(firstUnitInGame, "");
      }

      public void FixAfterDeserialization()
      {
         // C# stacks incorrectly enumerate backwards, so when you serialize then deserialize them, they come back reversed! Therefore make a copy of the stack to fix it. Since stacks always enumerate backwards, the process of making the new copy will reverse it!
         UndoStack = new Stack<State>(UndoStack);
         Current.NextTargetUnitOnReturn = new Stack<Unit>(Current.NextTargetUnitOnReturn);
         foreach (var state in UndoStack)
         {
            state.NextTargetUnitOnReturn = new Stack<Unit>(state.NextTargetUnitOnReturn);
         }
      }

      public Page CurrentPage { get => Current.Page; private set { } }

      public void Undo()
      {
         if (CanUndo())
            Current = UndoStack.Pop();
      }

      public bool CanUndo()
      {
         return UndoStack.Count != 0;
      }

      public void MoveToReaction(
         string reactionText)
      {
         // Before we change the state, save a copy of the old state for restoration on undo.
         UndoStack.Push(new State(Current));

         // Get the chosen arrow.
         if (!Current.ReactionArrowsByLink.TryGetValue(reactionText, out var chosenReactionArrow))
            Log.Fail(String.Format($"No arrow for reaction '{reactionText}'"));

         var trace = "";

         // Add to the score counts for all the offered arrows.
         foreach (var offeredReactionArrow in Current.ReactionArrowsByLink.Values)
         {
            offeredReactionArrow.Code.Traverse((code, originalSourceText) =>
            {
               if (!(code is ScoreCode scoreCode) || scoreCode.SortOnly)
                  return true;
               foreach (var id in scoreCode.Ids)
               {
                  var scoreSetting = Current.Settings[id] as ScoreSetting;
                  var oldRatio = scoreSetting.RatioString();
                  scoreSetting.RaiseOpportunityCount();
                  var chosen = false;
                  if (offeredReactionArrow == chosenReactionArrow)
                  {
                     chosen = true;
                     scoreSetting.RaiseChosenCount();
                  }
                  if (DebugMode)
                     trace += String.Format($"@{Game.PositiveDebugTextStart}{(chosen ? "<" : "")}{id} {oldRatio} → {scoreSetting.RatioString()} {scoreSetting.PercentString()} {(scoreSetting.Value ? "true" : "false")}{(chosen ? ">" : "")}{Game.DebugTextStop}");

               }
               return true;
            });
         }

         // Make a little report for debugging purposes.
         var sortDictionary = new Dictionary<double, string>();
         double uniquifier = 0.0;
         foreach (var setting in Current.Settings)
         {
            // Ex. brave    43% (3/7) 
            if (setting.Value is ScoreSetting scoreSetting)
            {
               string line = String.Format($"{setting.Key,-12} {scoreSetting.PercentString()} ({scoreSetting.RatioString()}) {(scoreSetting.Value ? "true" : "false")}");
               sortDictionary.Add(scoreSetting.ScoreValue + uniquifier, line);
               uniquifier += 0.00001;
            }
         }
         var scoresReportWriter = new StreamWriter("scores.txt", false);
         foreach (var line in sortDictionary.OrderByDescending(pair => pair.Key).Select(pair => pair.Value))
         {
            scoresReportWriter.WriteLine(line);
         }
         scoresReportWriter.Close();

         // Move to the unit the chosen arrow points to.
         BuildPage(chosenReactionArrow.TargetUnit, trace);
      }

      private void BuildPage(
         Unit firstUnit,
         string startingTrace)
      {
         // Starting with the current unit box, a) merge the texts of all units connected below it into one text, and b) collect all the reaction arrows.
         Dictionary<double, string> accumulatedReactionTexts = new Dictionary<double, string>();
         // The action text will contain all the merged action texts.
         var accumulatedActionTexts = startingTrace;
         // Build these too.
         Current.ReactionArrowsByLink = new Dictionary<string, ReactionArrow>();
         // If there were no reaction arrows, we've reached an end point and need to return to 
         var gotAReactionArrow = false;
         // Reactions are sorted by score, which is a floating point number. But some reactions may have the same score. So add a small floating-point sequence number to each one, to disambiguate them.
         double reactionScoreDisambiguator = 0;

         // Scores use this to compute whether you are above average in a score. Set it now, before creating the page, so it can be used in conditions evaluated throughout page creation.
         ScoreSetting.Average = Current.Settings.Values.Where(setting => setting is ScoreSetting).Select(setting => (setting as ScoreSetting).ScoreValue).DefaultIfEmpty().Average();
         if (DebugMode)
            accumulatedActionTexts += String.Format($"@{Game.PositiveDebugTextStart}average = {ScoreSetting.Average:0.00}%{Game.DebugTextStop}");

         // This recursive routine will accumulate all the action and reaction text values in the above variables.
         Accumulate(firstUnit);
         while (!gotAReactionArrow)
         {
            // We got to a dead end without finding any reaction options for the player. So pop back to a pushed location and continue merging from there.
            if (!Current.NextTargetUnitOnReturn.Any())
               throw new InvalidOperationException(string.Format($"Got to a dead end with no place to return to."));
            var unit = Current.NextTargetUnitOnReturn.Pop();
            if (DebugMode)
               accumulatedActionTexts += "@" + Game.PositiveDebugTextStart + "pop " + unit.Id + Game.DebugTextStop;
            Accumulate(unit);
         }

         Current.Page = new Page(FixPlus(accumulatedActionTexts), accumulatedReactionTexts.OrderByDescending(pair => pair.Key).Select(pair => pair.Value).ToList());

         void Accumulate(
            Unit unit)
         {
            // First append this action box's own text and execute any settings.
            if (accumulatedActionTexts.Length == 0)
               accumulatedActionTexts = EvaluateText(unit.ActionCode);
            else
               accumulatedActionTexts += " " + EvaluateText(unit.ActionCode);

            EvaluateSettingsAndScores(unit.ActionCode, out string trace1);
            accumulatedActionTexts += trace1;

            // Next examine all the arrows for the action.
            var allWhensFailed = true;
            var whenElseArrows = new List<Arrow>();
            var returnArrows = new List<ReturnArrow>();
            foreach (var arrow in unit.GetArrows())
            {
               if (arrow is ReturnArrow returnArrow)
                  // We'll deal with these return arrows at the end of the loop.
                  returnArrows.Add(returnArrow);
               else if (EvaluateWhenElse(arrow.Code))
                  // Save 'when else' arrows for possible later execution.
                  whenElseArrows.Add(arrow);
               else
               {
                  // If conditions in the arrow are false, then just ignore the arrow completely. This includes all types of arrows.
                  (var succeeded, var hadWhen) = EvaluateWhen(arrow.Code, out string trace2);
                  accumulatedActionTexts += trace2;
                  if (!succeeded)
                     continue;
                  if (hadWhen)
                     allWhensFailed = false;
                  AccumulateArrow(arrow);
               }
            }
            if (allWhensFailed)
               // If none of the 'when EXPRESSIONS' arrows succeeded, execute the 'when else' arrows now.
               foreach (var arrow in whenElseArrows)
                  AccumulateArrow(arrow);
            if (returnArrows.Any())
            {
               // Create a unit on the fly and push it on the stack for execution on return. This converts the return arrows to merge arrows.
               var returnUnit = Unit.BuildReturnUnitFor(returnArrows);
               Current.NextTargetUnitOnReturn.Push(returnUnit);
            }
         }

         void AccumulateArrow(
         Arrow arrow)
         {
            switch (arrow)
            {
               case MergeArrow mergeArrow:
                  // There may be 'set' parameters for a referential merge.
                  EvaluateSettingsAndScores(arrow.Code, out string trace);
                  accumulatedActionTexts += trace;

                  if (DebugMode)
                     accumulatedActionTexts += "@" + Game.PositiveDebugTextStart + "merge" + (mergeArrow.DebugSceneId != null ? " " + mergeArrow.DebugSceneId : "") + Game.DebugTextStop;

                  // There are two kinds of merge arrows.
                  Unit targetUnit;
                  if (mergeArrow.TargetSceneUnit != null)
                  {
                     targetUnit = mergeArrow.TargetSceneUnit;
                     // When we finish the jump to the other scene, we will continue merging with the action this arrow points to.
                     Current.NextTargetUnitOnReturn.Push(mergeArrow.TargetUnit);
                  }
                  else
                     // It's a local merge arrow. Merge the action it points to.
                     // It should be impossible for it to have no target. Let it crash if that's the case.
                     targetUnit = mergeArrow.TargetUnit;
                  // Call this routine again recursively. It will append the target's text and examine the target's arrows.
                  Accumulate(targetUnit);
                  break;
               case ReactionArrow reactionArrow:
                  gotAReactionArrow = true;
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
                  {
                     // Sort by scores.
                     double highestScore = 0;
                     var highestScoreIdPlusSpace = "";
                     reactionArrow.Code.Traverse((code, originalSourceText) =>
                     {
                        if (!(code is ScoreCode scoreCode))
                           return true;
                        foreach (var id in scoreCode.Ids)
                        {
                           ScoreSetting scoreSetting;
                           if (Current.Settings.TryGetValue(id, out Setting setting))
                              scoreSetting = setting as ScoreSetting;
                           else
                           {
                              scoreSetting = new ScoreSetting();
                              Current.Settings.Add(id, scoreSetting);
                           }

                           var value = scoreSetting.ScoreValue;
                           if (value > highestScore)
                           {
                              highestScore = value;
                              highestScoreIdPlusSpace = id + " ";
                           }
                        }
                        return true;
                     });
                     if (DebugMode)
                        reactionText =
                           Game.PositiveDebugTextStart +
                           highestScoreIdPlusSpace +
                           ((int)(highestScore * 100)).ToString() +
                           "% " +
                           Game.DebugTextStop +
                           reactionText;
                     accumulatedReactionTexts[highestScore + reactionScoreDisambiguator] = "{" + reactionText + "}";
                     reactionScoreDisambiguator += 0.00001;
                  }
                  Current.ReactionArrowsByLink[reactionText] = reactionArrow;
                  break;
            }
         }
      }

      private bool EvaluateConditions(
         IEnumerable<Expression> expressions,
         out string outTrace,
         string originalSourceText)
      {
         outTrace = "";
         foreach (var expression in expressions)
         {
            if (!Current.Settings.TryGetValue(expression.LeftId, out Setting leftSetting))
               throw new InvalidOperationException(String.Format($"Referenced undefined setting {expression.LeftId} in\n{originalSourceText}."));

            bool succeeded = false;
            if (expression.RightId == null)
            {
               // This is the 'left' or 'not left' case.
               if (!(leftSetting is AbstractBooleanSetting leftBooleanSetting))
                  throw new InvalidOperationException(String.Format($"Setting {expression.LeftId} must be a truth value in\n{originalSourceText}."));
               succeeded = leftBooleanSetting.Value != expression.Not;
               if (DebugMode)
                  outTrace +=
                     "@" +
                     (succeeded ? Game.PositiveDebugTextStart : Game.NegativeDebugTextStart) +
                     "? " +
                     (expression.Not ? "not " : "") +
                     expression.LeftId +
                     " <" +
                     (leftBooleanSetting.Value ? "true" : "false") +
                     ">" +
                     Game.DebugTextStop;
            }
            else
            {
               // This is the 'left=right' or 'not left=right' case. The right ID isn't looked up like the left one is. It's a constant string to compare to. You can't compare the values of two IDs. You can't compare two booleans.
               if (!(leftSetting is StringSetting leftStringSetting))
                  throw new InvalidOperationException(String.Format($"Setting {expression.LeftId} must be a string in\n{originalSourceText}."));
               succeeded = (leftStringSetting.Value == expression.RightId) != expression.Not;
               if (DebugMode)
                  outTrace +=
                     "@" +
                     (succeeded ? Game.PositiveDebugTextStart : Game.NegativeDebugTextStart) +
                     "? " +
                     (expression.Not ? "not " : "") +
                     expression.LeftId +
                     " <" +
                     leftStringSetting.Value +
                     "> = " +
                     expression.RightId +
                     Game.DebugTextStop;
            }
            if (!succeeded)
               return false;
         }
         return true;
      }

      private (bool, bool) EvaluateWhen(
         Code topCode,
         out string outTrace)
      {
         string trace = "";
         // When there are no 'when' directives, it always succeeds.
         var allSucceeded = true;
         var hadWhen = false;
         topCode.Traverse((code, originalSourceText) =>
         {
            if (code is WhenCode whenCode)
            {
               hadWhen = true;
               if (!EvaluateConditions(whenCode.GetExpressions(), out trace, originalSourceText))
                  allSucceeded = false;
            }
            return true;
         });
         outTrace = trace;
         return (allSucceeded, hadWhen);
      }

      public void Set(
         string id,
         string value)
      {
         Current.Settings[id] = new StringSetting(value);
      }

      private string DereferenceString(
         string id)
      {
         if (!Current.Settings.TryGetValue(id, out Setting setting))
            throw new InvalidOperationException(String.Format($"Reference to undefined setting {id}."));
         if (!(setting is StringSetting stringSetting))
            throw new InvalidOperationException(String.Format($"Setting {id} must be a string."));
         return stringSetting.Value;
      }

      public string Get(
         string id)
      {
         return DereferenceString(id);
      }

      private string GetSpecialText(
         string specialId)
      {
         if (specialId == "John" || specialId == "Jane")
            return DereferenceString("jane");
         else if (specialId == "Smith")
            return DereferenceString("smith");
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
               return heroIsMale ? "his" : "hers";
            else if (specialId == "His" || specialId == "Hers")
               return heroIsMale ? "His" : "Hers";
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

      private string NormalizeText(
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

      private string EvaluateText(
         Code value)
      {
         string accumulator = "";
         value.Traverse((code, originalSourceText) =>
         {
            switch (code)
            {
               case CharacterCode characterCode:
                  accumulator += characterCode.Characters;
                  break;
               case IfCode ifCode:
                  return EvaluateConditions(ifCode.GetExpressions(), out var trace, originalSourceText);
               case SpecialCode specialCode:
                  accumulator += GetSpecialText(specialCode.Id);
                  break;
            }
            return true;
         });
         // Always returns an empty string if there is no useful text.
         return NormalizeText(accumulator);
      }

      private bool EvaluateWhenElse(
         Code topCode)
      {
         bool result = false;
         topCode.Traverse((code, originalSourceText) =>
         {
            if (code is WhenElseCode whenElseCode)
               result = true;
            return true;
         });
         return result;
      }

      private void EvaluateSettingsAndScores(
         Code topCode,
         out string outTrace)
      {
         string trace = "";
         topCode.Traverse((code, originalSourceText) =>
         {
            switch (code)
            {
               case SetCode setCode:
                  foreach (var expression in setCode.GetExpressions())
                  {
                     if (expression.RightId == null)
                        // This is [set tvOn] or [set not tvOn].
                        Current.Settings[expression.LeftId] = new BooleanSetting(!expression.Not);
                     else
                        Current.Settings[expression.LeftId] = new StringSetting(expression.RightId);
                     if (DebugMode)
                        trace += "@" + Game.PositiveDebugTextStart + "set " + (expression.Not ? "not " : "") + expression.LeftId + (expression.RightId != null ? "=" + expression.RightId : "") + Game.DebugTextStop;
                  }
                  break;
               case TextCode textCode:
                  Current.Settings[textCode.Id] = new StringSetting(textCode.Text);
                  if (DebugMode)
                     trace += "@" + Game.PositiveDebugTextStart + "text " + textCode.Id + "=" + textCode.Text + Game.DebugTextStop;
                  break;
               case ScoreCode scoreCode:
                  if (!scoreCode.SortOnly)
                     foreach (var id in scoreCode.Ids)
                     {
                        var scoreSetting = Current.Settings[id] as ScoreSetting;
                        scoreSetting.RaiseChosenCount();
                        // Better raise this too, otherwise you could have more choices than opportunities to choose, i.e greater than 100% score.
                        scoreSetting.RaiseOpportunityCount();
                        if (DebugMode)
                           trace += String.Format($"@{Game.PositiveDebugTextStart}<{id} → {scoreSetting.RatioString()} {scoreSetting.PercentString()} {(scoreSetting.Value ? "true" : "false")}>{Game.DebugTextStop}");
                     }
                  break;
            }
            return true;
         });
         outTrace = trace;
      }

      private string FixPlus(
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


   }
}
