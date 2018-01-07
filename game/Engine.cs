using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
   public static partial class Engine
   {
      // It's all about these global variables.
      private static Tags Tags = new Tags();
      private static List<Continuation> Continuations = new List<Continuation>();

      private static string GetSpecialText(
         string specialId,
         Dictionary<string, object> variables)
      {
         if (specialId == "p")
         {
            return "\r\n";
         }
         else if (specialId == "First")
         {
            return ValueString(Tags.FirstWithNameAndLabel("hero", "first"), variables);
         }
         else if (specialId == "Last")
         {
            return ValueString(Tags.FirstWithNameAndLabel("hero", "last"), variables);
         }
         else
         {
            bool heroIsMale = Tags.FirstWithNameAndLabel("hero", "isMale") != null;
            if (specialId == "he")
            {
               return heroIsMale ? "he" : "she";
            }
            else if (specialId == "He")
            {
               return heroIsMale ? "He" : "She";
            }
            else if (specialId == "him")
            {
               return heroIsMale ? "him" : "her";
            }
            else if (specialId == "Him")
            {
               return heroIsMale ? "Him" : "Her";
            }
            else if (specialId == "his")
            {
               return heroIsMale ? "his" : "her";
            }
            else if (specialId == "His")
            {
               return heroIsMale ? "His" : "Her";
            }
            else if (specialId == "himself")
            {
               return heroIsMale ? "himself" : "herself";
            }
            else if (specialId == "Himself")
            {
               return heroIsMale ? "Himself" : "Herself";
            }
            else if (specialId == "man")
            {
               return heroIsMale ? "man" : "woman";
            }
            else if (specialId == "Man")
            {
               return heroIsMale ? "Man" : "Woman";
            }
            else if (specialId == "boy")
            {
               return heroIsMale ? "boy" : "girl";
            }
            else if (specialId == "Boy")
            {
               return heroIsMale ? "Boy" : "Girl";
            }
            else if (specialId == "Mr")
            {
               return heroIsMale ? "Mr." : "Ms";
            }
            else
            {
               Log.Fail(String.Format("Unknown special ID {0}.", specialId));
               return "";
            }
         }
      }

      private static string EvaluateItemText(
         object nodeName,
        Dictionary<string, object> variables)
      {
         return EvaluateText(Tags.FirstWithNameAndLabel(nodeName as string, "text"), variables);
      }

      private static string EvaluateText(
        object value,
        Dictionary<string, object> variables)
      {
         if (variables == null)
         {
            variables = new Dictionary<string, object>();
         }
         string accumulator = "";
         (value as SequenceObject).Traverse((@object) =>
         {
            switch (@object)
            {
               case TextObject textObject:
                  accumulator += textObject.Text;
                  break;
               case SubstitutionObject substitutionObject:
                  accumulator += ValueString(EvaluateLabelListFirst(substitutionObject.Expression.LeftName, substitutionObject.Expression.LeftLabels, variables), variables);
                  break;
               case IfObject ifObject:
                  return TryRecursively(0, ifObject.NotExpressions, variables);
               case SpecialObject specialObject:
                  accumulator += GetSpecialText(specialObject.Id, variables);
                  break;
            }
            return true;
         });
         return accumulator;
      }

      private static void EvaluateItemTags(
        object nodeName,
        Dictionary<string, object> variables)
      {
         if (variables == null)
         {
            variables = new Dictionary<string, object>();
         }
         var text = Tags.FirstWithNameAndLabel(nodeName as string, "text");
         (text as SequenceObject).Traverse((@object) =>
         {
            switch (@object)
            {
               case TagObject tagObject:
                  if (tagObject.Untag)
                  {
                     (var leftName, var leftLabel) = EvaluateLabelListGetLastNameAndLabel(tagObject.Expression.LeftName, tagObject.Expression.LeftLabels, variables);
                     if (leftName == null)
                        break;
                     (var rightName, var rightLabel) = EvaluateLabelListGetLastNameAndLabel(tagObject.Expression.RightName, tagObject.Expression.RightLabels, variables);
                     if (rightName == null)
                     {
                        Tags.Remove(leftName, leftLabel);
                        break;
                     }
                     var rightValue = Tags.FirstWithNameAndLabel(rightName, rightLabel);
                     Tags.Remove(leftName, leftLabel, rightValue);
                  }
                  else
                  {
                     (var leftName, var leftLabel) = EvaluateLabelListGetLastNameAndLabel(tagObject.Expression.LeftName, tagObject.Expression.LeftLabels, variables);
                     if (leftName == null)
                        break;
                     Tags.Remove(leftName, leftLabel);
                     if (tagObject.RightText != null)
                     {
                        Tags.Add(leftName, leftLabel, tagObject.RightText);
                        break;
                     }
                     (var rightName, var rightLabel) = EvaluateLabelListGetLastNameAndLabel(tagObject.Expression.RightName, tagObject.Expression.RightLabels, variables);
                     if (rightName == null)
                     {
                        Tags.Add(leftName, leftLabel, "");
                        break;
                     }
                     var rightValue = Tags.FirstWithNameAndLabel(rightName, rightLabel);
                     Tags.Add(leftName, leftLabel, rightValue);
                  }
                  break;
            }
            return true;
         });
      }

      private static string SelectBestArrow(
         Dictionary<string, string> reasons)
      {
         // Put cool algorithm here when we figure it out.
         return reasons.First().Key;
      }

      private static bool IsVariable(
        string name)
      {
         return Char.IsUpper(name[0]);
      }

      private static string ValueString(
         object value,
         Dictionary<string, object> variables)
      {
         if (value == null)
         {
            Log.Fail("value is null");
         }
         if (value is string)
            return value as string;
         return EvaluateText(value, variables);
      }

      private static (string, string) EvaluateLabelListGetLastNameAndLabel(
         string name,
         List<string> labels,
         Dictionary<string, object> variables)
      {
         if (name == null)
         {
            return (null, null);
         }
         // For example, 'OtherSide.target.isOpen'. This function will return whatever OtherSide.target is as the name and 'isOpen' as the label.
         if (IsVariable(name))
         {
            if (!variables.ContainsKey(name))
            {
               Log.Fail(String.Format("undefined variable {0}", name));
            }
            name = ValueString(variables[name], variables);
         }
         object lastValue = null;
         string lastLabel = null;
         string lastName = null;
         foreach (var label in labels)
         {
            lastName = name;
            lastLabel = label;
            if (label == labels[labels.Count() - 1])
               break;
            // Otherwise, get the value make it the next name.
            lastValue = Tags.FirstWithNameAndLabel(name, label);
            // If we can't find any part along the way, fail. 'null' means that it was never tagged, which is different from being tagged with no value (ex. [tag hero.isShort]), which has the value "".
            if (lastValue == null)
               return (null, null);
            name = ValueString(lastValue, variables);
         }
         // If we reached the last one, return the last name and label. Don't worry about whether you can find the last one. The caller will take care of that to suit their own purposes.
         return (lastName, lastLabel);
      }

      private static IEnumerable<object> EvaluateLabelListAll(
        string name,
        List<string> labels,
        Dictionary<string, object> variables)
      {
         (var lastName, var lastLabel) = EvaluateLabelListGetLastNameAndLabel(name, labels, variables);
         if (lastName != null)
         {
            // Don't convert this to strings here. Do that as late as possible to make sure there are no old values in them.
            return Tags.AllWithNameAndLabel(lastName, lastLabel);
         }
         return Enumerable.Empty<object>();
      }

      private static object EvaluateLabelListFirst(
        string name,
        List<string> labels,
        Dictionary<string, object> variables)
      {
         (var lastName, var lastLabel) = EvaluateLabelListGetLastNameAndLabel(name, labels, variables);
         if (lastName == null)
            return null;
         // Don't convert this to a string here. Do that as late as possible to make sure there are no old values in it.
         return Tags.FirstWithNameAndLabel(lastName, lastLabel);
      }

      private static bool EvaluateExpression(
        NotExpression notExpression,
        Dictionary<string, object> variables)
      {
         // Evaluate the left side of the expression.
         var leftValue = EvaluateLabelListFirst(notExpression.Expression.LeftName, notExpression.Expression.LeftLabels, variables);
         if (leftValue == null)
            return false != notExpression.Not;

         // If there's no right side, success.
         if (String.IsNullOrEmpty(notExpression.Expression.RightName))
            return true != notExpression.Not;

         // Otherwise, compare to right side.
         var rightValue = EvaluateLabelListFirst(notExpression.Expression.RightName, notExpression.Expression.RightLabels, variables);
         return ValueString(leftValue, variables) == ValueString(rightValue, variables) != notExpression.Not;
      }

      private static bool TryRecursively(
        int index,
        List<NotExpression> notExpressions,
        Dictionary<string, object> variables)
      {
         // We've gotten to the end of the not-expressions--success.
         if (index >= notExpressions.Count)
         {
            Log.Add(new string(' ', (index + 1) * 2) + "Win!");
            return true;
         }

         var notExpression = notExpressions[index];

         string variableList = "";
         foreach (var variable in variables)
         {
            variableList += variable.Key + "=" + variable.Value + " ";
         }
         Log.Add(new string(' ', (index + 1) * 2) + notExpression + " | " + variableList);

         /* There are various cases here:

             Iteration over all values that satisfy:

               VARIABLE.LABEL-LIST
                 Find all names that satisfy the label list.
               VARIABLE.LABEL-LIST=VARIABLE.LABEL-LIST
                 Find all names that satisfy the equality using the current value of the right variable.
               VARIABLE.LABEL-LIST=ID.LABEL-LIST
                 Find all names that satisfy the equality.

             Simple evaluation of constants:

               ID.LABEL-LIST
                 Just evaluate it.
               ID.LABEL-LIST=VARIABLE.LABEL-LIST
                 Just evaluate for the current value of the right variable.
               ID.LABEL-LIST=ID.LABEL-LIST
                 Just evaluate it.

             Variable assignments:

               VARIABLE=VARIABLE.LABEL-LIST
                 Assign the right side to the variable using the current value of the right variable only.
               VARIABLE=ID.LABEL-LIST
                 Assign the right side to the variable.
         */
         // If it's a variable that has no value yet:
         if (IsVariable(notExpression.Expression.LeftName) && !variables.ContainsKey(notExpression.Expression.LeftName))
         {
            // Iteration cases are followed by labels to test:
            if (notExpression.Expression.LeftLabels.Any())
            {
               foreach ((var candidateName, var value) in Tags.AllWithLabel("isNode"))
               {
                  // Set the variable to each node name in the tags, then evaluate the whole expression.
                  variables[notExpression.Expression.LeftName] = candidateName;
                  if (EvaluateExpression(notExpression, variables))
                  {
                     // If you got all the way to the end using this name, good.
                     if (TryRecursively(index + 1, notExpressions, variables))
                        return true;
                  }
                  // If it didn't work, either because it didn't evaluate or a subsequent not-expression didn't evaluate, then go on and try the next node name.
               }
               // None of them worked.
               variables.Remove(notExpression.Expression.LeftName);
               return false;
            }
            // Assignment cases have a right side to assign from, but no labels on the left:
            else if (!String.IsNullOrEmpty(notExpression.Expression.RightName))
            {
               // Get every match to the expression. There may be multiple ones, ex. 'Destination=Door.arrow' where there may be multiple arrows.
               foreach (var value in EvaluateLabelListAll(notExpression.Expression.RightName, notExpression.Expression.RightLabels, variables))
               {
                  variables[notExpression.Expression.LeftName] = value;
                  if (TryRecursively(index + 1, notExpressions, variables))
                     return true;
                  variables.Remove(notExpression.Expression.LeftName);
               }
               return false;
            }
            else
            {
               Log.Fail("Expected labels or an assignment after a variable.");
               return false;
            }
         }
         // It's an ID or already-defined variable. Just evaluate the expression:
         else
         {
            if (EvaluateExpression(notExpression, variables))
            {
               // Good, go on to the next one.
               return TryRecursively(index + 1, notExpressions, variables);
            }
            return false;
         }
      }

      private static (bool, string) EvaluateStoryArrow(
         string arrowName,
         Dictionary<string, object> variables)
      {
         // When there are no when directives, it always succeeds.
         var allSucceeded = true;
         var optionText = "";
         var arrowText = Tags.FirstWithNameAndLabel(arrowName, "text");
         (arrowText as SequenceObject).Traverse((@object) =>
         {
            if (!(@object is WhenObject whenObject))
               return true;
            if (!TryRecursively(0, whenObject.NotExpressions, variables))
            {
               allSucceeded = false;
            }
            return true;
         });
         optionText = EvaluateItemText(arrowName, variables);
         return (allSucceeded, optionText);
      }

      private static Description AddToDescription(
         Description description,
         Continuation continuation)
      {
         description.Text += EvaluateItemText(continuation.NodeName, continuation.Variables) + "\r\n";
         foreach (var arrowNameObject in Tags.AllWithNameAndLabel(continuation.NodeName, "arrow"))
         {
            var arrowName = ValueString(arrowNameObject, continuation.Variables);
            (var allSucceeded, var optionText) = EvaluateStoryArrow(arrowName, continuation.Variables);
            if (allSucceeded && !String.IsNullOrWhiteSpace(optionText))
            {
               var newOption = new Description.Option();
               newOption.ArrowName = ValueString(arrowName, continuation.Variables);
               newOption.Text = optionText;
               newOption.Continuation = continuation;
               description.Options.Add(newOption);
            }
         }
         return description;
      }

      private static (Continuation, Continuation) ShiftContinuationToArrowTarget(
         string arrowName,
         Continuation continuation)
      {
         Continuation removedContinuation = null;
         var newContinuation = new Continuation();
         newContinuation.IsStart = false;
         newContinuation.NodeName = ValueString(Tags.FirstWithNameAndLabel(arrowName, "target"), continuation.Variables);
         newContinuation.Variables = continuation.Variables;
         EvaluateItemTags(newContinuation.NodeName, newContinuation.Variables);
         if (continuation.IsStart)
         {
            // Keep it, but clear it for its next use.
            continuation.Variables = new Dictionary<string, object>();
         }
         else
         {
            removedContinuation = continuation;
         }
         return (newContinuation, removedContinuation);
      }

      public static void ShiftContinuationByChoice(
         Description.Option option)
      {
         (var newContinuation, var removedContinuation) = ShiftContinuationToArrowTarget(option.ArrowName, option.Continuation);
         if (removedContinuation != null)
         {
            Continuations.Remove(option.Continuation);
         }
         Continuations.Add(newContinuation);
      }

      public static Description UpdateContinuations()
      {
         var description = new Description();
         var removedContinuations = new List<Continuation>();
         var addedContinuations = new List<Continuation>();
         // Go over every possible description. We are going to ignore some or all of these.
         foreach (var continuation in Continuations)
         {
            var arrowReasons = new Dictionary<string, string>();
            var continuationHasPlayerOptions = false;
            var arrowCount = 0;
            foreach (var arrowNameObject in Tags.AllWithNameAndLabel(continuation.NodeName, "arrow"))
            {
               var arrowName = ValueString(arrowNameObject, continuation.Variables);
               ++arrowCount;
               (var allSucceeded, var optionText) = EvaluateStoryArrow(arrowName, continuation.Variables);
               if (!allSucceeded)
                  continue; // to next arrow
               // We always have to add the arrow, even if there was no reason specified for the arrow due to '&& !arrowReasons.Any()' condition below.
               arrowReasons.Add(arrowName, "put the real reason here later when we've figured that out");
               if (!String.IsNullOrWhiteSpace(optionText))
               {
                  continuationHasPlayerOptions = true;
               }
            }
            if (arrowCount > 0 && !arrowReasons.Any())
            {
               // No successful arrows means there's no way to move forward with this story right now.
               continue; // to next continuation
            }
            var describedContinuation = continuation;
            if (arrowCount > 0 && !continuationHasPlayerOptions)
            {
               // There are no player options so we can move forward automatically.
               var bestArrowName = SelectBestArrow(arrowReasons);
               (var newContinuation, var removedContinuation) = ShiftContinuationToArrowTarget(bestArrowName, continuation);
               if (removedContinuation != null)
               {
                  removedContinuations.Add(continuation);
               }
               addedContinuations.Add(newContinuation);
               describedContinuation = newContinuation;
            }
            AddToDescription(description, describedContinuation);
            if (arrowCount == 0)
            {
               removedContinuations.Add(continuation);
            }
         }
         Continuations.RemoveAll(continuation => removedContinuations.Contains(continuation));
         Continuations.AddRange(addedContinuations);
         return description;
      }

      public static void SelectItem(
         string itemName,
         bool set)
      {
         Tags.Remove(itemName, "isSelected");
         if (set)
         {
            Tags.Add(itemName, "isSelected", "");
         }
      }

      public static string GetHeroStageDescription()
      {
         var heroStage = Tags.FirstWithNameAndLabel("hero", "stage");
         if (heroStage == null)
         {
            Log.Fail("Hero is not on any stage");
         }
         return EvaluateItemText(heroStage, null);
      }

      public static IEnumerable<(string nodeText, string targetName)> HeroStageContents()
      {
         var heroStage = Tags.FirstWithNameAndLabel("hero", "stage");
         if (heroStage == null)
         {
            Log.Fail("Hero is not on any stage");
         }

         foreach (var name in Tags.AllWithLabelAndValue("stage", heroStage))
         {
            var description = EvaluateItemText(name, null);
            if (String.IsNullOrWhiteSpace(description))
               continue;
            yield return (description, name);
         }
      }
   }
}