using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
   // Partial: there's also the LoadSource function in its own file.
   public static partial class Engine
   {
      // The engine is all about the following global variables:

      // The Tags contain the state of the game: where you are, what people think of you, etc.
      private static Tags Tags = new Tags();

      // RootNodeNames is a list of all the root nodes where story trees start.
      private static List<string> RootNodeNames = new List<string>();

      // CurrentNodeName is the one node within a story tree that we are on right now. If it is null, we aren't in a story tree right now. In that case, we show a list of all the starting nodes that are appropriate for the current situation.
      private static string CurrentNodeName = null;

      // The same story tree can apply to different characters, locations, objects, etc. As we go through a story tree, we collect what the current values of those are.
      private static Dictionary<string, object> CurrentVariables;

      public static void SelectLink(
         string link)
      {
      }

      private static string ValueString(
         object value,
         Dictionary<string, object> variables)
      {
         if (value == null)
         {
            Log.Fail("Value is null", variables);
         }
         if (value is string)
            return value as string;
         return EvaluateText(value, variables);
      }

      private static bool EvaluateItemCondition(
         string nodeName,
         Dictionary<string, object> variables)
      {
         // When there are no when directives, it always succeeds.
         var allSucceeded = true;
         var text = Tags.FirstWithNameAndLabel(nodeName, "text");
         (text as SequenceObject).Traverse((@object) =>
         {
            if (!(@object is WhenObject whenObject))
               return true;
            if (!TryRecursively(0, whenObject.NotExpressions, variables))
            {
               allSucceeded = false;
            }
            return true;
         });
         return allSucceeded;
      }

      private static string GetSpecialText(
   string specialId,
   Dictionary<string, object> variables)
      {
         if (specialId == "John" || specialId == "Jane")
         {
            return ValueString(Tags.FirstWithNameAndLabel("hero", "jane", true), variables);
         }
         else if (specialId == "Smith")
         {
            return ValueString(Tags.FirstWithNameAndLabel("hero", "smith", true), variables);
         }
         else
         {
            bool heroIsMale = Tags.FirstWithNameAndLabel("hero", "isMale") != null;
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
               Log.Fail(String.Format("Unknown special ID {0}.", specialId), variables);
               return "";
            }
         }
      }

      private static bool IsVariable(
         string name)
      {
         return Char.IsUpper(name[0]);
      }

      private static (string, string) EvaluateLabelListGetLastNameAndLabel(
         string name,
         List<string> labels,
         Dictionary<string, object> variables,
         bool mustNotBeNull = false)
      {
         if (name == null)
         {
            if (mustNotBeNull)
            {
               Log.Fail("EvaluateLabelListGetLastNameAndLabel passed empty name");
            }
            return (null, null);
         }
         // Oddly, the parser interprets this: [hero.stage=Stage] as this: [hero.stage=.Stage]. Maybe that should be fixed at some point. But for now, just have this function "fix" it by shifting the value out of the label and into the name.
         if (name == "" && labels.Count() == 1)
         {
            name = labels[0];
            labels = new List<string>();
         }
         // For example, 'OtherSide.target.isOpen'. This function will return whatever OtherSide.target is as the name and 'isOpen' as the label.
         if (IsVariable(name))
         {
            if (!variables.ContainsKey(name))
            {
               Log.Fail(String.Format("undefined variable {0}", name), variables);
            }
            name = ValueString(Log.FailWhenNull(true, variables[name], name), variables);
         }
         // In the case with no labels, the value is the name itself.
         if (!labels.Any())
            return (name, null);
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
            lastValue = Tags.FirstWithNameAndLabel(name, label, mustNotBeNull);
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
        Dictionary<string, object> variables,
        bool mustNotBeNull = false)
      {
         (var lastName, var lastLabel) = EvaluateLabelListGetLastNameAndLabel(name, labels, variables, mustNotBeNull);
         if (lastName == null)
            return null;
         // This is the [tag hero.stage=DestinationStage] case -- no labels.
         if (lastLabel == null)
            return lastName;
         // Don't convert this to a string here. Do that as late as possible to make sure there are no old values in it.
         return Tags.FirstWithNameAndLabel(lastName, lastLabel, mustNotBeNull);
      }

      private static bool EvaluateExpression(
        NotExpression notExpression,
        Dictionary<string, object> variables)
      {
         // Evaluate the left side of the expression.
         var leftValue = EvaluateLabelListFirst(notExpression.Expression.LeftName, notExpression.Expression.LeftLabels, variables);
         if (leftValue == null)
            return false != notExpression.Not;

         // If there's no right side, success. (Oddly, the parser interprets this: [hero.stage=Stage] as this: [hero.stage=.Stage]. Maybe that should be fixed at some point. But for now, just have this function check the labels, not the name).

         if (!notExpression.Expression.RightLabels.Any())
            return true != notExpression.Not;

         // Otherwise, compare to right side.
         var rightValue = EvaluateLabelListFirst(notExpression.Expression.RightName, notExpression.Expression.RightLabels, variables, true);
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
            //Log.Add(new string(' ', (index + 1) * 2) + "Win!");
            return true;
         }

         var notExpression = notExpressions[index];
         /*
         string variableList = "";
         foreach (var variable in variables)
         {
            variableList += variable.Key + "=" + variable.Value + " ";
         }
         Log.Add(new string(' ', (index + 1) * 2) + notExpression + " | " + variableList);
         */
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
               Log.Fail("Expected labels or an assignment after a variable.", variables);
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
                  accumulator += ValueString(EvaluateLabelListFirst(substitutionObject.Expression.LeftName, substitutionObject.Expression.LeftLabels, variables, true), variables);
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

      private static string EvaluateItemText(
         object nodeName,
         Dictionary<string, object> variables)
      {
         return EvaluateText(Tags.FirstWithNameAndLabel(nodeName as string, "text"), variables);
      }

      private static void EvaluateTags(
         object text,
         Dictionary<string, object> variables)
      {
         (text as SequenceObject).Traverse((@object) =>
         {
            switch (@object)
            {
               case TagObject tagObject:
                  if (tagObject.IsUntag)
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
                     if (!tagObject.IsBag)
                     {
                        Tags.Remove(leftName, leftLabel);
                     }
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
                     var rightValue = "";
                     if (rightLabel == null)
                     {
                        // This covers the [tag hero.stage=Stage] case.
                        rightValue = rightName;
                     }
                     else
                     {
                        // This covers the [tag hero.stage=other.stage] case.
                        rightValue = ValueString(Tags.FirstWithNameAndLabel(rightName, rightLabel, true), variables);
                     }
                     Tags.Add(leftName, leftLabel, rightValue);
                  }
                  break;
            }
            return true;
         });
      }

      public static string BuildNextText()
      {
         var resultText = "";

         if (CurrentNodeName == null)
         {
            // Present a menu of all the root story nodes which are appropriate to the current situation. For example, if the hero is located on a street, show the beginnings of all the stories that occur on that street.
            foreach (var nodeName in RootNodeNames)
            {
               // Evaluate the node's when clause. If true, the story is appropriate for the menu.
               var variables = new Dictionary<string, object>();
               if (EvaluateItemCondition(nodeName, variables))
               {
                  // EvaluateItemCondition returned the variables that succeeded. Use them to build the result text.
                  resultText = EvaluateItemText(nodeName, variables);

                  // Now put in all the option arrow texts.
                  foreach (var arrowNameObject in Tags.AllWithNameAndLabel(nodeName, "arrow"))
                  {
                     resultText += "@";
                     var arrowName = ValueString(arrowNameObject, variables);
                     if (EvaluateItemCondition(arrowName, variables))
                     {
                        resultText += "@";
                        resultText += EvaluateItemText(arrowName, variables);
                     }
                  }

               }
            }
         }
         else
         {
            // If we are in the middle of a story, show the current story node and its options (the arrows).
         }
         return resultText;
      }
   }
}
