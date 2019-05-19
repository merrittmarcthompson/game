using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
   // Partial: there's also the LoadSource function in its own file.
   public static partial class Engine
   {
      // The engine is all about the following global variables:

      // A list of all the root actions where scene trees start.
      private static Dictionary<string, string> RootSceneActionIds = new Dictionary<string, string>();

      // This contains the scenes.
      public static Tags Tags = new Tags();

      public static bool DebugMode = false;

      // State represents the state of the game. It implements undoing game choices and going back to previous game states.
      private class State
      {
         // The settings contain the state of the game: where you are, what people think of you, etc.
         public Dictionary<string, object> Settings = new Dictionary<string, object>();

         // This is the one action within a story tree that we are on right now. If it is null, we aren't in a story tree. In that case, we show a list of all the starting actions that are appropriate for the current situation.
         public string ActionId = null;

         // The same story tree can apply to different characters, locations, objects, etc. As we go through a story tree, we collect what the current values of those are.
         public Dictionary<string, string> ReactionTargetActionIds = new Dictionary<string, string>();

         // Stack of return merge locations for referential merges.
         public Stack<string> NextTargetActionIdOnReturn = new Stack<string>();

         public State()
         {
         }

         public State(State other)
         {
            Settings = new Dictionary<string, object>(other.Settings);
            ActionId = other.ActionId;
            ReactionTargetActionIds = new Dictionary<string, string>(other.ReactionTargetActionIds);
            NextTargetActionIdOnReturn = new Stack<string>(other.NextTargetActionIdOnReturn);
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
         {
            Log.Fail("Value is null");
         }
         if (value is string)
            return value as string;
         return EvaluateText(value);
      }

      private static bool EvaluateConditions(
         List<Expression> expressions,
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
               {
                  traceLeftValue = "<true>";
               }
               else
               {
                  leftValue = ValueString(leftObject);
                  traceLeftValue = leftValue;
               }
            }
            else
            {
               traceLeftValue = "<false>";
            }
            if (found == expression.Not)
            {
               if (DebugMode)
               {
                  outTrace += "@`" + (expression.Not? "not ": "") + expression.LeftId + "(" + traceLeftValue + ")? <fail>";
               }
               return false;
            }
            string traceEqualRight = "";
            if (expression.RightId != null)
            {
               traceEqualRight = "=" + expression.RightId;
               if (leftValue == expression.RightId == expression.Not)
               {
                  if (DebugMode)
                  {
                     outTrace += "@`" + (expression.Not ? "not " : "") + expression.LeftId + "(" + traceLeftValue + ")" + traceEqualRight + "? <fail>";
                  }
                  return false;
               }
            }
            if (DebugMode)
            {
               outTrace += "@`" + (expression.Not ? "not " : "") + expression.LeftId + "(" + traceLeftValue + ")" + traceEqualRight + "?";
            }
         }
         return true;
      }

      private static bool EvaluateItemCondition(
         string itemId,
         out string outTrace)
      {
         string trace = "";
         // When there are no 'when' directives, it always succeeds.
         var allSucceeded = true;
         var text = Tags.FirstWithNameAndLabel(itemId, "text");
         (text as SequenceObject).Traverse((@object) =>
         {
            if (!(@object is WhenObject whenObject))
               return true;
            if (!EvaluateConditions(whenObject.Expressions, out trace))
            {
               allSucceeded = false;
            }
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
            {
               return heroIsMale ? "he" : "she";
            }
            else if (specialId == "He" || specialId == "She")
            {
               return heroIsMale ? "He" : "She";
            }
            else if (specialId == "him" || specialId == "her")
            {
               return heroIsMale ? "him" : "her";
            }
            else if (specialId == "Him" || specialId == "Her")
            {
               return heroIsMale ? "Him" : "Her";
            }
            else if (specialId == "his" || specialId == "hers")
            {
               return heroIsMale ? "his" : "her";
            }
            else if (specialId == "His" || specialId == "Hers")
            {
               return heroIsMale ? "His" : "Her";
            }
            else if (specialId == "himself" || specialId == "herself")
            {
               return heroIsMale ? "himself" : "herself";
            }
            else if (specialId == "Himself" || specialId == "Herself")
            {
               return heroIsMale ? "Himself" : "Herself";
            }
            else if (specialId == "man" || specialId == "woman")
            {
               return heroIsMale ? "man" : "woman";
            }
            else if (specialId == "Man" || specialId == "Woman")
            {
               return heroIsMale ? "Man" : "Woman";
            }
            else if (specialId == "boy" || specialId == "girl")
            {
               return heroIsMale ? "boy" : "girl";
            }
            else if (specialId == "Boy" || specialId == "Girl")
            {
               return heroIsMale ? "Boy" : "Girl";
            }
            else if (specialId == "Mr" || specialId == "Ms")
            {
               return heroIsMale ? "Mr." : "Ms";
            }
            else if (specialId == "Mrs")
            {
               return heroIsMale ? "Mr." : "Mrs.";
            }
            else
            {
               Log.Fail(String.Format("Unknown special ID {0}.", specialId));
            }
         }
         return "";
      }

      private static string EvaluateText(
         object value)
      {
         string accumulator = "";
         (value as SequenceObject).Traverse((@object) =>
         {
            switch (@object)
            {
               case CharacterObject characterObject:
                  accumulator += characterObject.Characters;
                  break;
               case SubstitutionObject substitutionObject:
                  accumulator += ValueString(substitutionObject.Id);
                  break;
               case IfObject ifObject:
                  return EvaluateConditions(ifObject.Expressions, out var trace);
               case SpecialObject specialObject:
                  accumulator += GetSpecialText(specialObject.Id);
                  break;
            }
            return true;
         });
         return Transform.RemoveExtraBlanks(accumulator);
      }

      private static string EvaluateItemText(
         object itemName)
      {
         return EvaluateText(Tags.FirstWithNameAndLabel(itemName as string, "text"));
      }

      private static MergeObject EvaluateMerge(
         object text)
      {
         MergeObject result = null;
         (text as SequenceObject).Traverse((@object) =>
         {
            switch (@object)
            {
               case MergeObject mergeObject:
                  result = mergeObject;
                  return true;
            }
            return false;
         });
         return result;
      }

      private static string EvaluateScene(
         object text)
      {
         string result = null;
         (text as SequenceObject).Traverse((@object) =>
         {
            switch (@object)
            {
               case SceneObject sceneObject:
                  result = sceneObject.SceneId;
                  return true;
            }
            return false;
         });
         return result;
      }

      private static void EvaluateSettings(
         object text,
         out string outTrace)
      {
         string trace = "";
         (text as SequenceObject).Traverse((@object) =>
         {
            switch (@object)
            {
               case SetObject setObject:
                  foreach (var expression in setObject.Expressions)
                  {
                     if (expression.Not)
                     {
                        Current.Settings.Remove(expression.LeftId);
                     }
                     else
                     {
                        // If it is [set tall], the right ID will be null.
                        Current.Settings[expression.LeftId] = expression.RightId;
                     }
                     if (DebugMode)
                     {
                        trace += "@`set " + (expression.Not ? "not " : "") + expression.LeftId + (expression.RightId != null ? "=" + expression.RightId : "");
                     }
                  }
                  break;
               case TextObject textObject:
                  Current.Settings[textObject.Id] = textObject.Text;
                  if (DebugMode)
                  {
                     trace += "@`text " + textObject.Id + "=" + textObject.Text;
                  }
                  break;
            }
            return true;
         });
         outTrace = trace;
      }

      private static string BuildOneActionText(
         string firstActionName,
         bool allowNoReactions)
      {
         // Starting with the given action box, a) merge the texts of all actions connected below it into one text, and b) collect all the reaction arrows and append them at the end.
         // Reaction arrows and merge arrows can be intermingled, but we want all the reaction arrows at the bottom. So we're going to accumulate the reaction texts in a separate variable and append it later.
         var accumulatedReactionTexts = "";

         // The action text will contain all the merged action texts.
         var accumulatedActionTexts = "";

         // This recursive routine will accumulate all the action and reaction text values in the above variables.
         Accumulate(firstActionName);

         if (accumulatedReactionTexts.Length == 0 && !allowNoReactions)
            return null;
         return accumulatedActionTexts + accumulatedReactionTexts;

         void Accumulate(
            string actionName)
         {
            string trace;
            // First append this action box's own text and execute any settings.
            accumulatedActionTexts += EvaluateItemText(actionName);
            EvaluateSettings(Tags.FirstWithNameAndLabel(actionName, "text"), out trace);
            accumulatedActionTexts += trace;

            // Next examine all the arrows for the action.
            var arrowCount = 0;
            foreach (var arrowNameObject in Tags.AllWithNameAndLabel(actionName, "arrow"))
            {
               var arrowName = ValueString(arrowNameObject);
               // If conditions in the arrow are false, then just ignore the arrow completely. This includes both reaction and merge arrows.
               bool succeeded = EvaluateItemCondition(arrowName, out trace);
               accumulatedActionTexts += trace;
               if (!succeeded)
                  continue;
               ++arrowCount;
               // Check the arrow type. There are only two kinds of arrows.
               MergeObject mergeObject = EvaluateMerge(Tags.FirstWithNameAndLabel(arrowName, "text"));
               if (mergeObject != null)
               {
                  // There may be 'set' parameters for a referential merge.
                  EvaluateSettings(Tags.FirstWithNameAndLabel(arrowName, "text"), out trace);
                  accumulatedActionTexts += trace;

                  if (DebugMode)
                  {
                     accumulatedActionTexts += "@`merge" + (mergeObject.SceneId != null ? " " + mergeObject.SceneId : "");
                  }

                  // It's a merge arrow. There are two kinds of merge arrows.
                  string targetActionName;
                  if (mergeObject.SceneId != null)
                  {
                     // It's a referential merge arrow. Merge the action it references by name.
                     if (!RootSceneActionIds.ContainsKey(mergeObject.SceneId))
                     {
                        Log.Fail("Unknown scene name " + mergeObject.SceneId);
                     }
                     targetActionName = RootSceneActionIds[mergeObject.SceneId];
                     // When we finish the jump to the other scene, we will continue merging with the action this arrow points to.
                     Current.NextTargetActionIdOnReturn.Push(Tags.FirstWithNameAndLabel(arrowName, "target") as string);
                  }
                  else
                  {
                     // It's a local merge arrow. Merge the action it points to.
                     // It should be impossible for it to have no target. Let it crash if that's the case.
                     targetActionName = Tags.FirstWithNameAndLabel(arrowName, "target") as string;
                  }
                  // Call this routine again recursively. It will append the target's text and examine the target's arrows.
                  Accumulate(targetActionName);
               }
               else
               {
                  // It's a reaction arrow.
                  var reactionText = EvaluateItemText(arrowName);
                  // There's a little trickiness with links here...
                  if (reactionText.Length > 0 && reactionText[0] == '{')
                  {
                     // If it's in braces, it refers to a hyperlink already in the text. Don't make a new hyperlink for it. Just take off the braces. When the user clicks on the link, it won't have braces.
                     reactionText = reactionText.Substring(1);
                     var end = reactionText.IndexOf("}");
                     if (end != -1)
                     {
                        reactionText = reactionText.Substring(0, end);
                     }
                  }
                  else
                  {
                     accumulatedReactionTexts += "@~";
                     accumulatedReactionTexts += "{" + reactionText + "}";
                  }
                  Current.ReactionTargetActionIds[reactionText] = ValueString(Tags.FirstWithNameAndLabel(arrowName, "target"));
               }
            }
            if (arrowCount == 0)
            {
               // This is a terminal action of a scene. If this scene was referenced by another scene, we need to continue merging back in the referencing scene.
               if (Current.NextTargetActionIdOnReturn.Any())
               {
                  Accumulate(Current.NextTargetActionIdOnReturn.Pop());
               }
            }
         }
      }

      public static string BuildActionTextForReaction(
         string reactionText)
      {
         // The UI calls this to obtain a text representation of the next screen to appear.
         if (reactionText != null)
         {
            UndoStack.Push(new State(Current));
            if (!Current.ReactionTargetActionIds.TryGetValue(reactionText, out Current.ActionId))
            {
               Log.SetSourceText(null);
               Log.Fail(String.Format("No arrow for reaction '{0}'", reactionText));
            }
            Current.ActionId = Current.ReactionTargetActionIds[reactionText];
         }
         Current.ReactionTargetActionIds = new Dictionary<string, string>();

         // Show the current action box and its reaction arrows. False means, if there are no reactions, fail. You always have to have a way to move forward.
         var resultText = BuildOneActionText(Current.ActionId, false);
         return resultText;
      }
   }
}
