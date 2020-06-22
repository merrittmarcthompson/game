#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamebook
{
   class PageCreator
   {
      // Either creates the first page of the world story, or creates the next page after a page, based on the chosen reaction.

      // DebugMode on make it add annotations about how merges were done, etc.
      public bool DebugMode { get; set; } = false;

      public Page BuildFirst(
         World world)
      {
         return Build(world.FirstUnit, world.Settings, new Stack<Unit> (), "");
      }

      public Page BuildNext(
         Page page,
         string reactionText)
      {
         // Get the chosen arrow.
         if (!page.Reactions.TryGetValue(reactionText, out var chosen))
            throw new InvalidOperationException(string.Format($"No arrow for reaction '{reactionText}'."));

         var trace = "";

         // First finish off the previous page by adding to the score counts for all the arrows offered to the player. So map from the list of reactions to a list of corresponding score settings. The 'SelectMany(result => result)' at the bottom merges together the results from the query and functional parts. Without it, it would be an enumeration of the offered items containing an enumeration of the code/id items. We need the query part so we can store the isChosenOne value in the 'let' for use later on.
         foreach ((var isChosenOne, var scoreSetting) in
            (
               from offered in page.Reactions.Values
               let isChosenOne = offered.ReactionArrow == chosen.ReactionArrow
               select offered.ReactionArrow.Code
                  .Traverse()
                  .OfType<ScoreCode>()
                  .Where(scoreCode => !scoreCode.SortOnly)
                  .SelectMany(scoreCode => scoreCode.Ids)
                  .Select(id => (isChosenOne, page.Settings[id] as ScoreSetting))
            ).SelectMany(result => result))
         {
            if (isChosenOne)
               scoreSetting.IncreaseChosenCount();
            scoreSetting.IncreaseOpportunityCount();
         }

         // Make a little report for debugging purposes.
         var sortDictionary = new Dictionary<double, string>();
         double uniquifier = 0.0;
         foreach (var (key, scoreSetting) in
            page.Settings
               .Where(setting => setting.Value is ScoreSetting)
               .Select(setting => (setting.Key, setting.Value as ScoreSetting)))
         {
            // Ex. brave    43% (3/7) false
            string line = String.Format($"{key,-12} {scoreSetting.PercentString()} ({scoreSetting.RatioString()}) {(scoreSetting.Value ? "true" : "false")}");
            sortDictionary.Add(scoreSetting.ScoreValue + uniquifier, line);
            uniquifier += 0.00001;
         }
         var scoresReportWriter = new StreamWriter("scores.txt", false);
         foreach (var line in
            sortDictionary
               .OrderByDescending(pair => pair.Key)
               .Select(pair => pair.Value))
         {
            scoresReportWriter.WriteLine(line);
         }
         scoresReportWriter.Close();

         // Move to the unit the chosen arrow points to.
         return Build(chosen.ReactionArrow.TargetUnit, page.Settings, page.NextTargetUnitOnReturn, trace);
      }

      private Page Build(
         Unit firstUnit,
         Dictionary<string, Setting> previousSettings,
         Stack<Unit> previousNextTargetUnitOnReturn,
         string startingTrace)
      {
         // Starting with the given unit box, a) merge the texts of all units connected below it into one text, and b) collect all the reaction arrows.
         var accumulatedReactions = new Dictionary<string, ScoredReactionArrow>();
         // The action text will contain all the merged action texts.
         var accumulatedActionTexts = startingTrace;
         // If there were no reaction arrows, we've reached an end point and need to return to 
         var gotAReactionArrow = false;
         // Reactions are sorted by score, which is a floating point number. But some reactions may have the same score. So add a small floating-point sequence number to each one, to disambiguate them.
         double reactionScoreDisambiguator = 0;
         // The next page will start with the previous page's settings and return stack.
         var settings = new Dictionary<string, Setting>(previousSettings);
         var nextTargetUnitOnReturn = new Stack<Unit>(previousNextTargetUnitOnReturn);
         // Flip the stack back to the right direction.
         nextTargetUnitOnReturn = new Stack<Unit>(nextTargetUnitOnReturn);

         // Scores use this to compute whether you are above average in a score. Set it now, before creating the page, so it can be used in conditions evaluated throughout page creation.
         ScoreSetting.Average = settings.Values
            .OfType<ScoreSetting>()
            .Select(scoreSetting => scoreSetting.ScoreValue)
            .DefaultIfEmpty()
            .Average();

         if (DebugMode)
            accumulatedActionTexts += String.Format($"@{Game.PositiveDebugTextStart}average = {ScoreSetting.Average:0.00}%{Game.DebugTextStop}");

         // This recursive routine will accumulate all the action and reaction text values in the above variables.
         Accumulate(firstUnit, nextTargetUnitOnReturn, settings);
         while (!gotAReactionArrow)
         {
            // We got to a dead end without finding any reaction options for the player. So pop back to a pushed location and continue merging from there.
            if (!nextTargetUnitOnReturn.Any())
               throw new InvalidOperationException(string.Format($"Got to a dead end with no place to return to."));
            var unit = nextTargetUnitOnReturn.Pop();
            if (DebugMode)
               accumulatedActionTexts += "@" + Game.PositiveDebugTextStart + "pop " + unit.UniqueId + Game.DebugTextStop;
            Accumulate(unit, nextTargetUnitOnReturn, settings);
         }

         return new Page(FixPlus(accumulatedActionTexts), accumulatedReactions, settings, nextTargetUnitOnReturn);

         void Accumulate(
            Unit unit,
            Stack<Unit> nextTargetUnitOnReturn,
            Dictionary<string, Setting> settings)
         {
            // First append this action box's own text and execute any settings.
            if (accumulatedActionTexts.Length != 0)
               accumulatedActionTexts += " ";
            accumulatedActionTexts += EvaluateText(unit.ActionCode, settings);

            EvaluateSettingsAndScores(unit.ActionCode, settings, out string trace1);
            accumulatedActionTexts += trace1;

            // Next examine all the arrows for the action.
            var allWhensFailed = true;
            var whenElseArrows = new List<Arrow>();
            var returnArrows = new List<ReturnArrow>();
            foreach (var arrow in unit.Arrows)
            {
               if (arrow is ReturnArrow returnArrow)
                  // We'll deal with these return arrows at the end of the loop.
                  returnArrows.Add(returnArrow);
               else if (arrow.Code.Traverse().Where(code => code is WhenElseCode).Any())
                  // Save 'when else' arrows for possible later execution.
                  whenElseArrows.Add(arrow);
               else
               {
                  // If conditions in the arrow are false, then just ignore the arrow completely. This includes all types of arrows.
                  (var succeeded, var hadWhen) = EvaluateWhen(arrow.Code, settings, out string trace2);
                  accumulatedActionTexts += trace2;
                  if (!succeeded)
                     continue;
                  if (hadWhen)
                     allWhensFailed = false;
                  AccumulateArrow(arrow, nextTargetUnitOnReturn, settings);
               }
            }
            if (allWhensFailed)
               // If none of the 'when EXPRESSIONS' arrows succeeded, execute the 'when else' arrows now.
               foreach (var arrow in whenElseArrows)
                  AccumulateArrow(arrow, nextTargetUnitOnReturn, settings);
            if (returnArrows.Any())
            {
               // Create a unit on the fly and push it on the stack for execution on return. This converts the return arrows to merge arrows.
               var returnUnit = World.BuildReturnUnitFor(returnArrows);
               nextTargetUnitOnReturn.Push(returnUnit);
            }
         }

         void AccumulateArrow(
            Arrow arrow,
            Stack<Unit> nextTargetUnitOnReturn,
            Dictionary<string, Setting> settings)
         {
            switch (arrow)
            {
               case MergeArrow mergeArrow:
                  // There may be 'set' parameters for a referential merge.
                  EvaluateSettingsAndScores(arrow.Code, settings, out string trace);
                  accumulatedActionTexts += trace;

                  if (DebugMode)
                     accumulatedActionTexts += "@" + Game.PositiveDebugTextStart + "merge" + (mergeArrow.DebugSceneId != null ? " " + mergeArrow.DebugSceneId : "") + Game.DebugTextStop;

                  // There are two kinds of merge arrows.
                  Unit targetUnit;
                  if (mergeArrow.TargetSceneUnit != null)
                  {
                     targetUnit = mergeArrow.TargetSceneUnit;
                     // When we finish the jump to the other scene, we will continue merging with the action this arrow points to.
                     nextTargetUnitOnReturn.Push(mergeArrow.TargetUnit);
                  }
                  else
                     // It's a local merge arrow. Merge the action it points to.
                     // It should be impossible for it to have no target. Let it crash if that's the case.
                     targetUnit = mergeArrow.TargetUnit;
                  // Call this routine again recursively. It will append the target's text and examine the target's arrows.
                  Accumulate(targetUnit, nextTargetUnitOnReturn, settings);
                  break;
               case ReactionArrow reactionArrow:
                  gotAReactionArrow = true;
                  double highestScore = 0;
                  var reactionText = EvaluateText(reactionArrow.Code, settings);
                  // There's a little trickiness with links here...
                  if (reactionText.Length > 0 && reactionText[0] == '{')
                  {
                     // If it's in braces, it refers to a hyperlink already in the text. Don't make a new hyperlink for it. Just take off the braces. When the user clicks on the link, it won't have braces.
                     reactionText = reactionText.Substring(1);
#pragma warning disable CA1307 // Specify StringComparison
                     var end = reactionText.IndexOf("}");
#pragma warning restore CA1307 // Specify StringComparison
                     if (end != -1)
                        reactionText = reactionText.Substring(0, end);
                     // -1 tells the UI to not put embedded hyperlinks on the list on the screen.
                     highestScore = -1;
                  }
                  else
                  {
                     // Sort by scores.
                     highestScore = reactionArrow.Code.Traverse()
                        .OfType<ScoreCode>()
                        .SelectMany(scoreCode => scoreCode.Ids)
                        .Select(id => settings[id])
                        .OfType<ScoreSetting>()
                        .Select(scoreSetting => scoreSetting.ScoreValue)
                        .DefaultIfEmpty(0)
                        .Max();
                  }
                  accumulatedReactions[reactionText] = new ScoredReactionArrow(highestScore, reactionArrow);
                  reactionScoreDisambiguator += 0.00001;
                  break;
            }
         }
      }

      private bool EvaluateConditions(
         IEnumerable<Expression> expressions,
         Dictionary<string, Setting> settings,
         out string outTrace,
         string originalSourceText)
      {
         outTrace = "";
         foreach (var expression in expressions)
         {
            // These should be impossible due to syntax checking in the parser.
            if (!settings.TryGetValue(expression.LeftId, out Setting leftSetting))
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
         CodeTree codeTree,
         Dictionary<string, Setting> settings,
         out string outTrace)
      {
         string trace = "";
         // When there are no 'when' directives, it always succeeds.
         var whenCodes = codeTree.Traverse().OfType<WhenCode>();
         var whenCount = whenCodes.Count();
         var hadWhen = whenCount > 0;
         var allSucceeded = whenCodes
            .Where(whenCode => EvaluateConditions(whenCode.Expressions, settings, out trace, codeTree.SourceText))
            .Count() == whenCount;
         outTrace = trace;
         return (allSucceeded, hadWhen);
      }

      public static string DereferenceString(
         string id,
         Dictionary<string, Setting> settings)
      {
         if (!settings.TryGetValue(id, out Setting setting))
            throw new InvalidOperationException(String.Format($"Reference to undefined setting {id}."));
         if (!(setting is StringSetting stringSetting))
            throw new InvalidOperationException(String.Format($"Setting {id} must be a string."));
         return stringSetting.Value;
      }

      private static string GetSpecialText(
         string specialId,
         Dictionary<string, Setting> settings,
         string originalSourceText)
      {
         if (specialId == "John" || specialId == "Jane")
            return DereferenceString("jane", settings);
         else if (specialId == "Smith")
            return DereferenceString("smith", settings);
         else
         {
            bool heroIsMale = (settings["male"] as BooleanSetting)!.Value == true;
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
               throw new InvalidOperationException(string.Format($"Unknown special ID '{specialId}' in\n{originalSourceText}"));
         }
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

      private string EvaluateText(
         CodeTree codeTree,
         Dictionary<string, Setting> settings)
      {
         string accumulator = "";
         foreach (var code in codeTree.Traverse(ifExpressions => EvaluateConditions(ifExpressions, settings, out var trace, codeTree.SourceText)))
         {
            accumulator += code switch
            {
               CharacterCode characterCode => characterCode.Characters,
               SpecialCode specialCode => GetSpecialText(specialCode.Id, settings, codeTree.SourceText),
               _ => ""
            };
         }
         // Always returns an empty string if there is no useful text.
         return NormalizeText(accumulator);
      }

      private void EvaluateSettingsAndScores(
         CodeTree codeTree,
         Dictionary<string, Setting> settings,
         out string outTrace)
      {
         string trace = "";

         foreach (var code in
            codeTree.Traverse(ifExpressions => EvaluateConditions(ifExpressions, settings, out var trace, codeTree.SourceText)))
         {
            switch (code)
            {
               case SetCode setCode:
                  foreach (var expression in setCode.Expressions)
                  {
                     if (expression.RightId == null)
                        // This is [set tvOn] or [set not tvOn].
                        settings[expression.LeftId] = new BooleanSetting(!expression.Not);
                     else
                        settings[expression.LeftId] = new StringSetting(expression.RightId);
                     if (DebugMode)
                        trace += "@" + Game.PositiveDebugTextStart + "set " + (expression.Not ? "not " : "") + expression.LeftId + (expression.RightId != null ? "=" + expression.RightId : "") + Game.DebugTextStop;
                  }
                  break;
               case TextCode textCode:
                  settings[textCode.Id] = new StringSetting(textCode.Text);
                  if (DebugMode)
                     trace += "@" + Game.PositiveDebugTextStart + "text " + textCode.Id + "=" + textCode.Text + Game.DebugTextStop;
                  break;
               case ScoreCode scoreCode:
                  if (!scoreCode.SortOnly)
                     foreach (var (id, scoreSetting) in scoreCode.Ids.Select(id => (id, settings[id] as ScoreSetting)))
                     {
                        scoreSetting.IncreaseChosenCount();
                        // Better raise this too, otherwise you could have more choices than opportunities to choose, i.e greater than 100% score.
                        scoreSetting.IncreaseOpportunityCount();
                        if (DebugMode)
                           trace += String.Format($"@{Game.PositiveDebugTextStart}<{id} → {scoreSetting.RatioString()} {scoreSetting.PercentString()} {(scoreSetting.Value ? "true" : "false")}>{Game.DebugTextStop}");
                     }
                  break;
            }
         }
         outTrace = trace;
      }

      private static string FixPlus(
         string text)
      {
         // We always put in a space when we concatenate different text parts, but sometimes you don't want that, so you can put in a plus sign to stop that. Ex. "hello" joined with "there" => "hello there", but "hello+" joined with "there" => "hellothere".  Useful for things like 'He said "+' joined with 'I am a fish."' => 'He said "I am a fish."'
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
