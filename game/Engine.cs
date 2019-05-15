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
      private static List<string> RootActionIds = new List<string>();

      // A list of all the root actions that can be merged into other scenes.
      private static Dictionary<string, string> MergeActionIds = new Dictionary<string, string>();

      // This contains the scenes.
      public static Tags Tags = new Tags();

      // State represents the state of the game. It implements undoing game choices and going back to previous game states.
      private class State
      {
         // The settings contain the state of the game: where you are, what people think of you, etc.
         public Dictionary<string, object> Settings = new Dictionary<string, object>();

         // This is the one action within a story tree that we are on right now. If it is null, we aren't in a story tree. In that case, we show a list of all the starting actions that are appropriate for the current situation.
         public string ActionId = null;

         // The same story tree can apply to different characters, locations, objects, etc. As we go through a story tree, we collect what the current values of those are.
         public Dictionary<string, string> ReactionsActionIds;

         // Stack of return merge locations for referential merges.
         public Stack<string> NextTargetActionIdOnReturn = new Stack<string>();

         public State()
         {
         }

         public State(State other)
         {
            Settings = other.Settings;
            ActionId = other.ActionId;
            ReactionsActionIds = other.ReactionsActionIds;
            NextTargetActionIdOnReturn = other.NextTargetActionIdOnReturn;
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

      public static void SelectReaction(
         string reactionText)
      {
         UndoStack.Push(new State(Current));
         Current.ActionId = Current.ReactionsActionIds[reactionText];
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
         List<Expression> expressions)
      {
         foreach (var expression in expressions)
         {
            if (!Current.Settings.TryGetValue(expression.LeftId, out object leftValue))
               return false;
            if (expression.RightId != null && ValueString(leftValue) != expression.RightId)
               return false;
         }
         return true;
      }

      private static bool EvaluateItemCondition(
         string itemId)
      {
         // When there are no 'when' directives, it always succeeds.
         var allSucceeded = true;
         var text = Tags.FirstWithNameAndLabel(itemId, "text");
         (text as SequenceObject).Traverse((@object) =>
         {
            if (!(@object is WhenObject whenObject))
               return true;
            if (!EvaluateConditions(whenObject.Expressions))
            {
               allSucceeded = false;
            }
            return true;
         });
         return allSucceeded;
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
                  return EvaluateConditions(ifObject.Expressions);
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

      private static string EvaluateName(
         object text)
      {
         string result = null;
         (text as SequenceObject).Traverse((@object) =>
         {
            switch (@object)
            {
               case NameObject nameObject:
                  result = nameObject.NameId;
                  return true;
            }
            return false;
         });
         return result;
      }

      private static void EvaluateSettings(
         object text)
      {
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
                  }
                  break;
            }
            return true;
         });
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
            // First append this action box's own text and execute any settings.
            accumulatedActionTexts += EvaluateItemText(actionName);
            EvaluateSettings(Tags.FirstWithNameAndLabel(actionName, "text"));

            // Next examine all the arrows for the action.
            var arrowCount = 0;
            foreach (var arrowNameObject in Tags.AllWithNameAndLabel(actionName, "arrow"))
            {
               var arrowName = ValueString(arrowNameObject);
               // If conditions in the arrow are false, then just ignore the arrow completely. This includes both reaction and merge arrows.
               if (!EvaluateItemCondition(arrowName))
                  continue;
               ++arrowCount;
               // Check the arrow type. There are only two kinds of arrows.
               MergeObject mergeObject = EvaluateMerge(Tags.FirstWithNameAndLabel(arrowName, "text"));
               if (mergeObject != null)
               {
                  // It's a merge arrow. There are two kinds of merge arrows.
                  string targetActionName;
                  if (mergeObject.SceneId != null)
                  {
                     // It's a referential merge arrow. Merge the action it references by name.
                     if (!MergeActionIds.ContainsKey(mergeObject.SceneId))
                     {
                        Log.Fail("Unknown scene name " + mergeObject.SceneId);
                     }
                     targetActionName = MergeActionIds[mergeObject.SceneId];
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
                  Current.ReactionsActionIds[reactionText] = ValueString(Tags.FirstWithNameAndLabel(arrowName, "target"));
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

      public static string BuildNextText()
      {
         // The UI calls this to obtain a text representation of the next screen to appear.
         Current.ReactionsActionIds = new Dictionary<string, string>();

         if (Current.ActionId != null)
         {
            // This means we are in the middle of a scene. We have just moved to this action.
            // Show the current action box and its reaction arrows. False means, if there are no reactions, fail. You always have to have a way to move forward.
            var resultText = BuildOneActionText(Current.ActionId, false);
            // Null result means it got to a terminal action (with no reactions), so it's time to go back to a menu.
            if (resultText != null)
               return resultText;
         }
         // If we get here, we have never entered or we are just exiting a scene. Present a menu of all the root scene actions which are appropriate to the current situation. For example, if the hero is located on a street, show the beginnings of all the scenes that start on that street.
         var accumulator = "";
         foreach (var actionId in RootActionIds)
         {
            // Evaluate the actions's when clause. If true, the story is appropriate for the menu.
            if (EvaluateItemCondition(actionId))
            {
               // EvaluateItemCondition returned the variables that succeeded. Use them to build the result text.
               // True means allow no reactions. This lets us put "description-only" scenes on a menu.
               EvaluateSettings(Tags.FirstWithNameAndLabel(actionId, "text"));
               accumulator += BuildOneActionText(actionId, true);
            }
         }
         return accumulator;
      }
   }
}
