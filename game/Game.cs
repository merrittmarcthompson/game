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
      // State represents the state of the game. It implements undoing game choices and going back to previous game states.
      private class State
      {
         // The settings contain the state of the game: where you are, what people think of you, etc.
         public Dictionary<string, Setting> Settings = new Dictionary<string, Setting>();
         // This is the one unit within a story tree that we are on right now.
         public Unit Unit = null;
         // Stack of return merge locations for referential merges.
         public Stack<Unit> NextTargetUnitOnReturn = new Stack<Unit>();

         public State() { }

         public State(State other)
         {
            Settings = new Dictionary<string, Setting>(other.Settings);
            Unit = other.Unit;
            NextTargetUnitOnReturn = new Stack<Unit>(other.NextTargetUnitOnReturn);
         }
      }

      // VARIABLES

      // The current state of the game: what unit we are on, what are the current settings, etc.
      [JsonProperty] // Have to add this for private members.
      private State Current = new State();

      // Pop back to old states to implement undo.
      [JsonProperty] // Have to add this for private members.
      private Stack<State> UndoStack = new Stack<State>();

      // Add annotations about how merges were done, etc.
      public bool DebugMode = false;

      // This relates the user choice of reactions to the next units that are associated with them. Private: it gets regenerated on every screen build, so it doesn't need to be part of the state.
      private Dictionary<string, ReactionArrow> ReactionArrowsByLink = new Dictionary<string, ReactionArrow>();

      public const char NegativeDebugTextStart = '\u0001';
      public const char PositiveDebugTextStart = '\u0002';
      public const char DebugTextStop = '\u0003';

      // FUNCTIONS

      public Game(
         Unit first)
      {
         Current.Unit = first;
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

      public void Undo()
      {
         if (UndoStack.Count == 0)
            return;
         Current = UndoStack.Pop();
      }

      public bool CanUndo()
      {
         return UndoStack.Count != 0;
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
                  throw new InvalidOperationException(String.Format($"Setting {expression.LeftId} is a string, not a truth value in\n{originalSourceText}."));
               succeeded = leftBooleanSetting.Value != expression.Not;
               if (DebugMode)
                  outTrace +=
                     "@" +
                     (succeeded? Game.PositiveDebugTextStart: Game.NegativeDebugTextStart) +
                     "? " + 
                     (expression.Not ? "not " : "") + 
                     expression.LeftId +
                     " <" +
                     (leftBooleanSetting.Value? "true": "false") +
                     ">" +
                     Game.DebugTextStop;
            }
            else
            {
               // This is the 'left=right' or 'not left=right' case. The right ID isn't looked up like the left one is. It's a constant string to compare to. You can't compare the values of two IDs. You can't compare two booleans.
               if (!(leftSetting is StringSetting leftStringSetting))
                  throw new InvalidOperationException(String.Format($"Setting {expression.LeftId} must be a string, not a truth value in\n{originalSourceText}."));
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

      private bool EvaluateCondition(
         Code topCode,
         out string outTrace)
      {
         string trace = "";
         // When there are no 'when' directives, it always succeeds.
         var allSucceeded = true;
         topCode.Traverse((code, originalSourceText) =>
         {
            if (!(code is WhenCode whenCode))
               return true;
            if (!EvaluateConditions(whenCode.GetExpressions(), out trace, originalSourceText))
               allSucceeded = false;
            return true;
         });
         outTrace = trace;
         return allSucceeded;
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
               /*case SubstitutionCode substitutionCode:
                  accumulator += ValueString(substitutionCode.Id);
                  break;*/
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

      private void EvaluateSettings(
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

      public (string, List<string>) BuildPage()
      {
         // Starting with the current unit box, a) merge the texts of all units connected below it into one text, and b) collect all the reaction arrows.
         Dictionary<double, string> accumulatedReactionTexts = new Dictionary<double, string>();
         // The action text will contain all the merged action texts.
         var accumulatedActionTexts = "";
         // Build these too.
         ReactionArrowsByLink = new Dictionary<string, ReactionArrow>();
         // If there were no reaction arrows, we've reached an end point and need to return to 
         var gotAReactionArrow = false;
         // Reactions are sorted by score, which is a floating point number. But some reactions may have the same score. So add a small floating-point sequence number to each one, to disambiguate them.
         double reactionScoreDisambiguator = 0;

         // This recursive routine will accumulate all the action and reaction text values in the above variables.
         Accumulate(Current.Unit);
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
         
         return (FixPlus(accumulatedActionTexts), accumulatedReactionTexts.OrderByDescending(pair => pair.Key).Select(pair => pair.Value).ToList());

         void Accumulate(
            Unit unit)
         {
            string trace;
            // First append this action box's own text and execute any settings.
            if (accumulatedActionTexts.Length == 0)
               accumulatedActionTexts = EvaluateText(unit.ActionCode);
            else
               accumulatedActionTexts += " " + EvaluateText(unit.ActionCode);

            EvaluateSettings(unit.ActionCode, out trace);
            accumulatedActionTexts += trace;

            // Next examine all the arrows for the action.
            var arrowCount = 0;
            foreach (var arrow in unit.GetArrows())
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
                  case ReturnArrow returnArrow:
                     if (DebugMode)
                        accumulatedActionTexts += "@" + Game.PositiveDebugTextStart + "push " + returnArrow.TargetUnit.Id + Game.DebugTextStop;
                     Current.NextTargetUnitOnReturn.Push(returnArrow.TargetUnit);
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
                        double highestScore = 0;
                        reactionArrow.Code.Traverse((code, originalSourceText) =>
                        {
                           if (!(code is ScoreCode scoreCode))
                              return true;
                           foreach (var id in scoreCode.Ids)
                           {
                              // We don't have a way to declare these ahead of time right now. Put this in later.
                              /*
                                 if (!Current.Settings.TryGetValue(id, out Setting setting))
                                    throw new InvalidOperationException(String.Format($"Reference to undefined setting '{id}' in\n{originalSourceText}"));
                                 if (!(setting is ScoreSetting scoreSetting))
                                    throw new InvalidOperationException(String.Format($"'{id}' is not a score in\n{originalSourceText}."));
                               */
                              
                               // Instead, just create a score on the fly.
                              ScoreSetting scoreSetting;
                              if (Current.Settings.TryGetValue(id, out Setting setting))
                                 scoreSetting = setting as ScoreSetting;
                              else
                              {
                                 scoreSetting = new ScoreSetting();
                                 Current.Settings.Add(id, scoreSetting);
                              }
                              // END

                              var value = scoreSetting.ScoreValue;
                              if (value > highestScore)
                                 highestScore = value;
                           }
                           return true;
                        });
                        if (DebugMode)
                           reactionText = Game.PositiveDebugTextStart + ((int)(highestScore * 100)).ToString() + "% " + Game.DebugTextStop + reactionText;
                        accumulatedReactionTexts[highestScore + reactionScoreDisambiguator] = "{" + reactionText + "}";
                        reactionScoreDisambiguator += 0.00001;
                     }
                     ReactionArrowsByLink[reactionText] = reactionArrow;
                     break;
               }
            }
         }
      }

      public void MoveToReaction(
         string reactionText)
      {
         UndoStack.Push(new State(Current));
         // Get the chosen arrow.
         if (!ReactionArrowsByLink.TryGetValue(reactionText, out var chosenReactionArrow))
            Log.Fail(String.Format($"No arrow for reaction '{reactionText}'"));

         // Move to the unit it points to.
         Current.Unit = chosenReactionArrow.TargetUnit;

         // Add to the chosen counts for the arrow.
         chosenReactionArrow.Code.Traverse((code, originalSourceText) =>
         {
            if (!(code is ScoreCode scoreCode))
               return true;
            foreach (var id in scoreCode.Ids)
               // Ex. if the chosen arrow has the score 'brave', and one to the 'brave' score's chosen count.
               (Current.Settings[id] as ScoreSetting).RaiseChosenCount();
            return true;
         });

         // Add to the opportunity counts for all the arrows.
         foreach (var offeredReactionArrow in ReactionArrowsByLink.Values)
         {
            offeredReactionArrow.Code.Traverse((code, originalSourceText) =>
            {
               if (!(code is ScoreCode scoreCode))
                  return true;
               foreach (var id in scoreCode.Ids)
                  // Ex. if an opportunity to choose the score 'brave', and one to the 'brave' score's opportunity count.
                  (Current.Settings[id] as ScoreSetting).RaiseOpportunityCount();
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
               int percent = (int)(scoreSetting.ScoreValue * 100);
               string line = String.Format($"{setting.Key,-12} {percent.ToString() + "%", 4} ({scoreSetting.GetChosenCount()}/{scoreSetting.GetOpportunityCount()})");
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
      }
   }
}
