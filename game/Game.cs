﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
         public Dictionary<string, object> Settings = new Dictionary<string, object>();
         // This is the one action within a story tree that we are on right now.
         public Unit Unit = null;
         // Stack of return merge locations for referential merges.
         public Stack<Unit> NextTargetUnitOnReturn = new Stack<Unit>();

         public State() { }

         public State(State other)
         {
            Settings = new Dictionary<string, object>(other.Settings);
            Unit = other.Unit;
            NextTargetUnitOnReturn = new Stack<Unit>(other.NextTargetUnitOnReturn);
         }
      }

      // VARIABLES

      // Add annotations about how merges were done, etc.
      public bool DebugMode = false;

      // This relates the user choice of reactions to the next units that are associated with them. It gets regenerated on every screen build, so it doesn't need to be part of the state.
      private Dictionary<string, Unit> ReactionTargetUnits = new Dictionary<string, Unit>();

      // The current state of the game: what unit we are on, what are the current settings, etc.
      [JsonProperty] // Have to add this for private members.
      private State Current = new State();

      // Pop back to old states to implement undo.
      [JsonProperty] // Have to add this for private members.
      private Stack<State> UndoStack = new Stack<State>();

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

      private string ValueString(
         object value)
      {
         if (value == null)
            Log.Fail("Value is null");
         if (value is string)
            return value as string;
         return EvaluateText(value as SequenceCode);
      }

      private bool EvaluateConditions(
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
                  outTrace += "@`" + (expression.Not ? "not " : "") + expression.LeftId + "(" + traceLeftValue + ")? <false>~";
               return false;
            }
            string traceEqualRight = "";
            if (expression.RightId != null)
            {
               traceEqualRight = "=" + expression.RightId;
               if (leftValue == expression.RightId == expression.Not)
               {
                  if (DebugMode)
                     outTrace += "@`" + (expression.Not ? "not " : "") + expression.LeftId + "(" + traceLeftValue + ")" + traceEqualRight + "? <false>~";
                  return false;
               }
            }
            if (DebugMode)
               outTrace += "@`" + (expression.Not ? "not " : "") + expression.LeftId + "(" + traceLeftValue + ")" + traceEqualRight + "? <true>~";
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

      public void Set(
         string id,
         string value)
      {
         Current.Settings[id] = value;
      }

      public string Get(
         string id)
      {
         if (Current.Settings.TryGetValue(id, out object value))
            return ValueString(value);
         return "";
      }

      private string GetSpecialText(
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

      private void EvaluateSettings(
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
                        trace += "@`set " + (expression.Not ? "not " : "") + expression.LeftId + (expression.RightId != null ? "=" + expression.RightId : "") + "~";
                  }
                  break;
               case TextCode textCode:
                  Current.Settings[textCode.Id] = textCode.Text;
                  if (DebugMode)
                     trace += "@`text " + textCode.Id + "=" + textCode.Text + "~";
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

      public (string, List<string>) BuildText()
      {
         // Starting with the current unit box, a) merge the texts of all units connected below it into one text, and b) collect all the reaction arrows.
         List<string> accumulatedReactionTexts = new List<string>();

         // The action text will contain all the merged action texts.
         var accumulatedActionTexts = "";

         // Build these too.
         ReactionTargetUnits = new Dictionary<string, Unit>();

         // This recursive routine will accumulate all the action and reaction text values in the above variables.
         Accumulate(Current.Unit);

         return (FixPlus(accumulatedActionTexts), accumulatedReactionTexts);

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
                        accumulatedActionTexts += "@`merge" + (mergeArrow.DebugSceneId != null ? " " + mergeArrow.DebugSceneId : "") + "~";

                     // There are two kinds of merge arrows.
                     Unit targetAction;
                     if (mergeArrow.TargetSceneUnit != null)
                     {
                        targetAction = mergeArrow.TargetSceneUnit;
                        // When we finish the jump to the other scene, we will continue merging with the action this arrow points to.
                        Current.NextTargetUnitOnReturn.Push(mergeArrow.TargetUnit);
                     }
                     else
                        // It's a local merge arrow. Merge the action it points to.
                        // It should be impossible for it to have no target. Let it crash if that's the case.
                        targetAction = mergeArrow.TargetUnit;
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
                     ReactionTargetUnits[reactionText] = reactionArrow.TargetUnit;
                     break;
               }
            }
            if (arrowCount == 0)
            {
               // This is a terminal action of a scene. If this scene was referenced by another scene, we need to continue merging back in the referencing scene.
               if (Current.NextTargetUnitOnReturn.Any())
                  Accumulate(Current.NextTargetUnitOnReturn.Pop());
            }
         }
      }

      public void MoveToReaction(
         string reactionText)
      {
         UndoStack.Push(new State(Current));
         if (!ReactionTargetUnits.TryGetValue(reactionText, out Current.Unit))
            Log.Fail(String.Format($"No arrow for reaction '{reactionText}'"));
      }
   }
}
