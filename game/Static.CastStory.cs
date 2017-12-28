using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
  public static partial class Static
  {
    public static StoryStatus CastStory(
      List<NotExpression> notExpressions,
      Tags mapTags,
      Dictionary<string, string> internalNames)
    {
      var storyStatus = new StoryStatus();

      bool IsVariable(
        string name)
      {
        return Char.IsUpper(name[0]);
      }
      
      string EvaluateLabelList(
        string name,
        List<string> labels)
      {
        if (IsVariable(name))
        {
          if (!storyStatus.Variables.ContainsKey(name))
          {
            Log.Add(String.Format("undefined variable {0}", name));
            return null;
          }
          name = storyStatus.Variables[name];
        }
        else
        {
          if (internalNames.ContainsKey(name))
          {
            name = internalNames[name];
          }
        }
        string value = null;
        foreach (var label in labels)
        {
          value = mapTags.LookupFirst(name, label);
          // If we can't find any part along the way, fail. 'null' means that it was never tagged, which is different from being tagged with no value (ex. [tag hero.isShort]), which has the value "".
          if (value == null)
            return null;
          name = value;
        }
        return value;
      }

      bool EvaluateExpression(
        NotExpression notExpression)
      {
        // Evaluate the left side of the expression.
        var leftValue = EvaluateLabelList(notExpression.Expression.LeftName, notExpression.Expression.LeftLabels);
        if (leftValue == null)
          return false != notExpression.Not;

        // If there's no right side, success.
        if(String.IsNullOrEmpty(notExpression.Expression.RightName))
          return true != notExpression.Not;

        // Otherwise, compare to right side.
        var rightValue = EvaluateLabelList(notExpression.Expression.RightName, notExpression.Expression.RightLabels);
        return leftValue == rightValue != notExpression.Not;
      }

      bool TryRecursively(
        int index)
      {
        // We've gotten to the end of the not-expressions--success.
        if (index >= notExpressions.Count)
          return true;
        var notExpression = notExpressions[index];

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
        if (IsVariable(notExpression.Expression.LeftName) && !storyStatus.Variables.ContainsKey(notExpression.Expression.LeftName))
        {
          // Iteration cases are followed by labels to test:
          if (notExpression.Expression.LeftLabels.Any())
          {
            foreach ((var candidateName, var value) in mapTags.LookupAllWithLabel("isNode"))
            {
              // Set the variable to each node name in the tags, then evaluate the whole expression.
              storyStatus.Variables[notExpression.Expression.LeftName] = candidateName;
              if (EvaluateExpression(notExpression))
              {
                // If you got all the way to the end using this name, good.
                if (TryRecursively(index + 1))
                  return true;
              }
              // If it didn't work, either because it didn't evaluate or a subsequent not-expression didn't evaluate, then go on and try the next node name.
            }
            // None of them worked.
            storyStatus.Variables.Remove(notExpression.Expression.LeftName);
            return false;
          }
          // Assignment cases have a right side to assign from, but no labels on the left:
          else if (!String.IsNullOrEmpty(notExpression.Expression.RightName))
          {
            var value = EvaluateLabelList(notExpression.Expression.RightName, notExpression.Expression.RightLabels);
            if (value == null)
              return false;
            storyStatus.Variables[notExpression.Expression.LeftName] = value;
            if (TryRecursively(index + 1))
              return true;
            storyStatus.Variables.Remove(notExpression.Expression.LeftName);
            return false;
          }
          else
          {
            Log.Add("Expected labels or an assignment after a variable.");
            return false;
          }
        }
        // It's an ID or already-defined variable. Just evaluate the expression:
        else
        {
          if (EvaluateExpression(notExpression))
          {
            // Good, go on to the next one.
            return TryRecursively(index + 1);
          }
          return false;
        }
      }

      // START
      if (TryRecursively(0))
        return storyStatus;
      return null;
    }
  }
}