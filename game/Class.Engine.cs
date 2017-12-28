using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Game
{
  public static class Engine
  {
    private static Tags MapTags = new Tags();
    private static Tags StoryTags = new Tags();
    private static Dictionary<string, string> InternalNames = new Dictionary<string, string>();
    private static Dictionary<string, SequenceObject> SequenceObjects = new Dictionary<string, SequenceObject>();
    private static List<string> ActiveArrows = new List<string>();

    public static string CurrentStage;
    public static string HeroFirstName { get; set; } = "John";
    public static string HeroLastName { get; set; } = "Smith";
    public static bool HeroIsMale { get; set; } = true;

    private static string GetInternalName(
      string externalName)
    {
      if (InternalNames.ContainsKey(externalName))
        return InternalNames[externalName];
      return externalName;
    }

    private static List<string> GetActiveArrows(
      Tags fileBaseTags)
    {
      // For stories, find all the starting nodes that no arrow points to. These are the ones where stories can start.
      // First make a list of all the boxes that are pointed to in the file.
      var result = new List<string>();
      var arePointedTo = new HashSet<string>();
      var nodeList = new List<string>();
      foreach ((var nodeOrArrowName, var nodeOrArrowValue) in fileBaseTags.LookupAllWithLabel("arrow"))
      {
        var target = fileBaseTags.LookupFirst(nodeOrArrowValue, "target");
        if (!arePointedTo.Contains(target))
        {
          arePointedTo.Add(target);
        }
      }
      // Then find all the boxes that aren't in the list.
      foreach ((var nodeName, var nodeValue) in fileBaseTags.LookupAllWithLabel("isNode"))
      {
        if (!arePointedTo.Contains(nodeName))
        {
          // When you find a box nothing points to, add to the result any arrows it has which are unconditional and have no reaction text.
          foreach (var arrowName in fileBaseTags.LookupAll(nodeName, "arrow"))
          {
            if (!SequenceObjects[arrowName].ContainsText())
            {
              result.Add(arrowName);
            }
          }
        }
      }
      return result;
    }

    private static SequenceObject CompileSourceText(
      string sourceText)
    {
      // Compile the text to an object sequence.
      var tokens = Static.SourceTextToTokens(sourceText);
      if (tokens == null)
        return null;
      return Static.TokensToObjects(tokens);
    }

    private static void AddToSequenceObjects(
      Tags fileBaseTags)
    {
      foreach ((var itemName, var itemValue) in fileBaseTags.LookupAllWithLabel("sourceText"))
      {
        Log.SetSourceText(itemValue);
        var sequenceObject = CompileSourceText(itemValue);
        Log.SetSourceText(null);
        if (sequenceObject == null)
          continue;
        SequenceObjects[itemName] = sequenceObject;
      }
    }

    private static void ProcessStory(
      Tags fileBaseTags)
    {
      // Find all the arrows that could possibly lead to new stories.
      ActiveArrows.AddRange(GetActiveArrows(fileBaseTags));
    }

    private static Tags ProcessMap(
      Tags fileBaseTags)
    {
      // Execute the object text for maps.
      var fileNewTags = new Tags();
      foreach ((var itemName, var itemValue) in fileBaseTags.LookupAllWithLabel("sourceText"))
      {
        var sequenceObject = SequenceObjects[itemName];

        // First make a pass to set up the internal name reference table for further use.
        sequenceObject.Traverse((@object) =>
        {
          if (!(@object is NameObject nameObject))
            return true;
          // You can mark a box or arrow with [name myReferenceName]. Later, when you use myReferenceName (ex. in [when myReferenceName.isDoor]) we will convert it to the internal name.
          Log.Add(String.Format("name '{0}' references '{1}'", nameObject.Name, itemName));
          InternalNames[nameObject.Name] = itemName;
          return true;
        });

        // Next make all the implicit tag names (ex. [if isLarge]) explicit (ex. [if map_test_n1.isLarge]).
        sequenceObject.Traverse((@object) =>
        {
          switch (@object)
          {
            case SubstitutionObject substitutionObject:
              if (substitutionObject.Expression.LeftName == "")
              {
                substitutionObject.Expression.LeftName = itemName;
              }
              break;
            case TagObject tagObject:
              if (tagObject.Expression.LeftName == "")
              {
                tagObject.Expression.LeftName = itemName;
              }
              break;
            case IfObject ifObject:
              foreach (var notExpression in ifObject.NotExpressions)
              {
                if (notExpression.Expression.LeftName == "")
                {
                  notExpression.Expression.LeftName = itemName;
                }
                if (notExpression.Expression.RightName == "")
                {
                  notExpression.Expression.RightName = itemName;
                }
              }
              break;
          }
          return true;
        });

        // Next execute all the tag directives.
        sequenceObject.Traverse((@object) =>
        {
          if (!(@object is TagObject tagObject))
            return true;
          // [untag name.label] is just a comment in a map file. There's nothing in the tags to start with, so everything is untagged.
          if (tagObject.Untag)
            return true;
          if (tagObject.Expression.LeftLabels.Count != 1)
          {
            Log.Fail("expected only one label in a map tag specification");
          }
          fileNewTags.Add(tagObject.Expression.LeftName, tagObject.Expression.LeftLabels[0], tagObject.Expression.RightName);
          return true;
        });
      }
      return fileNewTags;
    }

    private static Tags GetStageTagsForNodes()
    {
      Tags newTags = new Tags();
      // Mark items for what stage they are on.
      foreach ((var nodeName, var nodeValue) in MapTags.LookupAllWithLabel("isStage"))
      {
        foreach (var arrowName in MapTags.LookupAll(nodeName, "arrow"))
        {
          var subordinateNode = MapTags.LookupFirst(arrowName, "target");
          if (MapTags.LookupFirst(subordinateNode, "isStage") == null)
          {
            newTags.Add(subordinateNode, "stage", nodeName);
          }
        }
      }
      return newTags;
    }

    private static bool IsVariable(
      string name)
    {
      return Char.IsUpper(name[0]);
    }

    private static string EvaluateLabelList(
      string name,
      List<string> labels,
      Dictionary<string, string> variables)
    {
      if (IsVariable(name))
      {
        if (!variables.ContainsKey(name))
        {
          Log.Fail(String.Format("undefined variable {0}", name));
        }
        name = variables[name];
      }
      else
      {
        name = GetInternalName(name);
      }
      string value = null;
      foreach (var label in labels)
      {
        value = MapTags.LookupFirst(name, label);
        // If we can't find any part along the way, fail. 'null' means that it was never tagged, which is different from being tagged with no value (ex. [tag hero.isShort]), which has the value "".
        if (value == null)
          return null;
        name = value;
      }
      return value;
    }

    private static bool EvaluateExpression(
      NotExpression notExpression,
      Dictionary<string, string> variables)
    {
      // Evaluate the left side of the expression.
      var leftValue = EvaluateLabelList(notExpression.Expression.LeftName, notExpression.Expression.LeftLabels, variables);
      if (leftValue == null)
        return false != notExpression.Not;

      // If there's no right side, success.
      if (String.IsNullOrEmpty(notExpression.Expression.RightName))
        return true != notExpression.Not;

      // Otherwise, compare to right side.
      var rightValue = EvaluateLabelList(notExpression.Expression.RightName, notExpression.Expression.RightLabels, variables);
      return leftValue == rightValue != notExpression.Not;
    }

    private static bool TryRecursively(
      int index,
      List<NotExpression> notExpressions,
      Dictionary<string, string> variables)
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
      if (IsVariable(notExpression.Expression.LeftName) && !variables.ContainsKey(notExpression.Expression.LeftName))
      {
        // Iteration cases are followed by labels to test:
        if (notExpression.Expression.LeftLabels.Any())
        {
          foreach ((var candidateName, var value) in MapTags.LookupAllWithLabel("isNode"))
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
          var value = EvaluateLabelList(notExpression.Expression.RightName, notExpression.Expression.RightLabels, variables);
          if (value == null)
            return false;
          variables[notExpression.Expression.LeftName] = value;
          if (TryRecursively(index + 1, notExpressions, variables))
            return true;
          variables.Remove(notExpression.Expression.LeftName);
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

    private static StoryStatus CastStory(
      List<NotExpression> notExpressions)
    {
      var storyStatus = new StoryStatus();
      if (TryRecursively(0, notExpressions, storyStatus.Variables))
        return storyStatus;
      return null;
    }

    public static void LoadSource()
    {
      // A chance to do some unit testing on the compiler.
      var sequenceObject = CompileSourceText("[tag isOtherSide]");
      /*
    @"[when
    Door.isDoor,
    not Door.isLockable,
    Destination=Door.arrow,
    Destination.isDoorTarget,
    hero.location.isBank]");
    */

      // Get the source directory.
      var arguments = Environment.GetCommandLineArgs();
      if (arguments.Length < 2)
      {
        Log.Fail("usage: game.exe source-directory");
      }

      // Get all the graphml files in the source directory.
      var sourcePaths = Directory.GetFiles(arguments[1], "*.graphml");
      if (sourcePaths.Length < 1)
      {
        Log.Fail(String.Format("no .graphml files in directory {0}", arguments[1]));
      }

      foreach (var sourcePath in sourcePaths)
      {
        var sourceName = Path.GetFileName(sourcePath);

        bool isMap;
        if (sourceName.StartsWith("map.", StringComparison.CurrentCultureIgnoreCase))
        {
          isMap = true;
        }
        else if (sourceName.StartsWith("story.", StringComparison.CurrentCultureIgnoreCase))
        {
          isMap = false;
        }
        else
          continue;

        Log.SetSourceName(sourceName);

        string graphml = System.IO.File.ReadAllText(sourcePath);

        // Translate the graphml boxes and arrows to tags.
        var fileBaseTags = Static.GraphmlToTags(graphml, Path.GetFileNameWithoutExtension(sourceName));

        // Compile the directives embedded in the source text of each box and arrow to create a list of object 'text' tags and a SequenceObjects table that relates them to the actual object code.
        AddToSequenceObjects(fileBaseTags);

        Tags fileNewTags;
        if (isMap)
        {
          fileNewTags = ProcessMap(fileBaseTags);
          MapTags.Merge(fileBaseTags);
          MapTags.Merge(fileNewTags);
        }
        else
        {
          ProcessStory(fileBaseTags);
          StoryTags.Merge(fileBaseTags);
        }
      }

      MapTags.Merge(GetStageTagsForNodes());

      foreach (var arrowName in ActiveArrows)
      {
        SequenceObjects[arrowName].Traverse((@object) =>
        {
          if (!(@object is WhenObject whenObject))
            return true;
          var story = CastStory(whenObject.NotExpressions);
          return true;
        });
      }

      CurrentStage = MapTags.LookupFirst(GetInternalName("hero"), "stage");
      if (CurrentStage == null)
      {
        Log.Fail("Hero is not on any stage");
      }
    }

    public static IEnumerable<string> MapArrowsFor(
      string nodeName)
    {
      return MapTags.LookupAll(nodeName, "arrow");
    }

    public static string EvaluateItemText(
      string itemName)
    {
      itemName = GetInternalName(itemName);

      // The item is a node or arrow.
      string accumulator = "";
      SequenceObjects[itemName].Traverse((@object) =>
      {
        switch (@object)
        {
          case TextObject textObject:
            accumulator += textObject.Text;
            break;
          case SubstitutionObject substitutionObject:
            accumulator += EvaluateLabelList(substitutionObject.Expression.LeftName, substitutionObject.Expression.LeftLabels, new Dictionary<string, string>());
            break;
          case IfObject ifObject:
            return TryRecursively(0, ifObject.NotExpressions, new Dictionary<string, string>());
          case SpecialObject specialObject:
            if (specialObject.Id == "p")
            {
              accumulator += "\r\n";
            }
            else if (specialObject.Id == "First")
            {
              accumulator += HeroFirstName;
            }
            else if (specialObject.Id == "Last")
            {
              accumulator += HeroLastName;
            }
            else if (specialObject.Id == "he")
            {
              accumulator += HeroIsMale ? "he" : "she";
            }
            else if (specialObject.Id == "He")
            {
              accumulator += HeroIsMale ? "He" : "She";
            }
            else if (specialObject.Id == "him")
            {
              accumulator += HeroIsMale ? "him" : "her";
            }
            else if (specialObject.Id == "Him")
            {
              accumulator += HeroIsMale ? "Him" : "Her";
            }
            else if (specialObject.Id == "his")
            {
              accumulator += HeroIsMale ? "his" : "her";
            }
            else if (specialObject.Id == "His")
            {
              accumulator += HeroIsMale ? "His" : "Her";
            }
            else if (specialObject.Id == "himself")
            {
              accumulator += HeroIsMale ? "himself" : "herself";
            }
            else if (specialObject.Id == "Himself")
            {
              accumulator += HeroIsMale ? "Himself" : "Herself";
            }
            else if (specialObject.Id == "man")
            {
              accumulator += HeroIsMale ? "man" : "woman";
            }
            else if (specialObject.Id == "Man")
            {
              accumulator += HeroIsMale ? "Man" : "Woman";
            }
            else if (specialObject.Id == "boy")
            {
              accumulator += HeroIsMale ? "boy" : "girl";
            }
            else if (specialObject.Id == "Boy")
            {
              accumulator += HeroIsMale ? "Boy" : "Girl";
            }
            else if (specialObject.Id == "Mr")
            {
              accumulator += HeroIsMale ? "Mr." : "Ms";
            }
            break;
        }
        return true;
      });
      return accumulator;
    }
  }
}