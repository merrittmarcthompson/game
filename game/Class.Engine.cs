using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Game
{
   public static class Engine
   {
      // It's all about these global variables.
      private static Tags MapTags = new Tags();
      private static Tags StoryTags = new Tags();
      private static Dictionary<string, SequenceObject> SequenceObjects = new Dictionary<string, SequenceObject>();

      public static List<Continuation> Continuations = new List<Continuation>();

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
         foreach ((var itemName, var itemValue) in fileBaseTags.AllWithLabel("sourceText"))
         {
            Log.SetSourceText(itemValue);
            var sequenceObject = CompileSourceText(itemValue);
            Log.SetSourceText(null);
            if (sequenceObject == null)
               continue;
            SequenceObjects[itemName] = sequenceObject;
         }
      }

      private static List<Reaction> BuildReactionsFor(
         string nodeName,
         Tags tags)
      {
         var newReactions = new List<Reaction>();
         // Find all the reactions for the shifted continuation's node where all the when expressions evaluate. Build a reaction object for each.
         foreach (var arrowName in tags.AllWithNameAndLabel(nodeName, "arrow"))
         {
            var variables = new Dictionary<string, string>();
            // If there are no when directives, it always succeeds.
            bool allSucceeded = true;
            bool isPlayerOption = false;
            SequenceObjects[arrowName].Traverse((@object) =>
            {
               switch (@object)
               {
                  case WhenObject whenObject:
                     // We allow multiple when directives. Just do them in order. Accumulate all the variables together.
                     if (!TryRecursively(0, whenObject.NotExpressions, variables))
                     {
                        allSucceeded = false;
                     }
                     break;
                  // If there's anything to build a reaction option string, we're going to display it in the reaction list.
                  case IfObject ifObject:
                  case SubstitutionObject substitutionObject:
                  case SpecialObject specialObject:
                     isPlayerOption = true;
                     break;
                  case TextObject textObject:
                     if (!String.IsNullOrWhiteSpace(textObject.Text))
                     {
                        isPlayerOption = true;
                     }
                     break;
               }
               return true;
            });
            if (allSucceeded)
            {
               var newReaction = new Reaction();
               newReaction.ArrowName = arrowName;
               newReaction.isSelected = false;
               newReaction.isPlayerOption = isPlayerOption;
               newReactions.Add(newReaction);
            }
         }
         return newReactions;
      }

      private static void PreprocessStory(
        Tags fileBaseTags)
      {
         // Find all the story nodes where stories can start for this source file. Make a continuation for each one. That means that the player can "continue" from the beginning of that story. Later, as they play through a story, we will add more continuation nodes to represent their position in the story.
         foreach ((var nodeName, _) in fileBaseTags.AllWithLabel("isNode"))
         {
            var sequenceObject = SequenceObjects[nodeName];
            sequenceObject.Traverse((@object) =>
            {
               // Add continuations for nodes with [start].
               if (!(@object is StartObject startObject))
                  return true;
               var continuation = new Continuation();
               continuation.NodeName = nodeName;
               continuation.IsStart = true;
               continuation.Reactions = BuildReactionsFor(nodeName, fileBaseTags);
               Continuations.Add(continuation);
               return true;
            });

         }
      }

      private static void RenameBaseTags(
      Tags fileBaseTags)
      {
         // First make a list of all the tags that have to be renamed.
         var oldToNewList = new Dictionary<string, string>();
         foreach ((var oldItemName, _) in fileBaseTags.AllWithLabel("sourceText"))
         {
            // If it has a sourceText tag, we made an object for it earlier.
            var sequenceObject = SequenceObjects[oldItemName];
            sequenceObject.Traverse((@object) =>
            {
               if (!(@object is NameObject nameObject))
                  return true;
               oldToNewList[oldItemName] = nameObject.Name;

               // Fix the SequenceObjects table right now while we're at it.
               SequenceObjects[nameObject.Name] = SequenceObjects[oldItemName];
               SequenceObjects.Remove(oldItemName);
               return true;
            });
         }

         // Make a list of all the tags that need new names.
         Tags tagsToRename = new Tags();
         foreach (var oldNew in oldToNewList)
         {
            foreach ((var itemLabel, var itemValue) in fileBaseTags.AllWithName(oldNew.Key))
            {
               tagsToRename.Add(oldNew.Key, itemLabel, itemValue);
            }
         }

         // Make a list of all the tags that need new values. At this point, only arrows and targets contain item names.
         Tags tagsToRevalue = new Tags();
         foreach ((var itemName, var itemValue) in fileBaseTags.AllWithLabel("arrow"))
         {
            if (oldToNewList.ContainsKey(itemValue))
            {
               tagsToRevalue.Add(itemName, "arrow", itemValue);
            }
         }
         foreach ((var itemName, var itemValue) in fileBaseTags.AllWithLabel("target"))
         {
            if (oldToNewList.ContainsKey(itemValue))
            {
               tagsToRevalue.Add(itemName, "target", itemValue);
            }
         }

         // Remove the old tags and add the new tags.
         foreach ((var oldName, var oldLabel, var oldValue) in tagsToRename.All())
         {
            fileBaseTags.Remove(oldName, oldLabel, oldValue);
            fileBaseTags.Add(oldToNewList[oldName], oldLabel, oldValue);
         }
         foreach ((var oldName, var oldLabel, var oldValue) in tagsToRevalue.All())
         {
            fileBaseTags.Remove(oldName, oldLabel, oldValue);
            fileBaseTags.Add(oldName, oldLabel, oldToNewList[oldValue]);
         }
      }

      private static Tags PreprocessMap(
         Tags fileBaseTags)
      {
         // Execute the object text for maps.
         var fileNewTags = new Tags();
         foreach ((var itemName, var itemValue) in fileBaseTags.AllWithLabel("sourceText"))
         {
            var sequenceObject = SequenceObjects[itemName];

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

            // Next execute all the tag directives. This is more limited than the full tagging done in story nodes.
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
               if (tagObject.Expression.RightLabels.Count > 0)
               {
                  fileNewTags.Add(tagObject.Expression.LeftName, tagObject.Expression.LeftLabels[0], tagObject.Expression.RightLabels[0]);
               }
               else
               {
                  fileNewTags.Add(tagObject.Expression.LeftName, tagObject.Expression.LeftLabels[0], "");
               }
               return true;
            });
         }
         return fileNewTags;
      }

      private static Tags GetStageTagsForNodes()
      {
         Tags newTags = new Tags();
         // Mark items for what stage they are on.
         foreach ((var nodeName, var nodeValue) in MapTags.AllWithLabel("isStage"))
         {
            foreach (var arrowName in MapTags.AllWithNameAndLabel(nodeName, "arrow"))
            {
               var subordinateNode = MapTags.FirstWithNameAndLabel(arrowName, "target");
               if (MapTags.FirstWithNameAndLabel(subordinateNode, "isStage") == null)
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

      private static (string, string) EvaluateLabelListGetLastNameAndLabel(
         string name,
         List<string> labels,
         Dictionary<string, string> variables)
      {
         // For example, 'OtherSide.target.isOpen'. This function will return whatever OtherSide.target is as the name and 'isOpen' as the label.
         if (IsVariable(name))
         {
            if (!variables.ContainsKey(name))
            {
               Log.Fail(String.Format("undefined variable {0}", name));
            }
            name = variables[name];
         }
         string lastValue = null;
         string lastLabel = null;
         string lastName = null;
         foreach (var label in labels)
         {
            lastName = name;
            lastLabel = label;
            if (label == labels[labels.Count() - 1])
               break;
            // Otherwise, get the value make it the next name.
            lastValue = MapTags.FirstWithNameAndLabel(name, label);
            // If we can't find any part along the way, fail. 'null' means that it was never tagged, which is different from being tagged with no value (ex. [tag hero.isShort]), which has the value "".
            if (lastValue == null)
               return (null, null);
            name = lastValue;
         }
         // If we reached the last one, return the last name and label. Don't worry about whether you can find the last one. The caller will take care of that to suit their own purposes.
         return (lastName, lastLabel);
      }

      private static IEnumerable<string> EvaluateLabelListAll(
        string name,
        List<string> labels,
        Dictionary<string, string> variables)
      {
         (var lastName, var lastLabel) = EvaluateLabelListGetLastNameAndLabel(name, labels, variables);
         if (lastName != null)
         {
            return MapTags.AllWithNameAndLabel(lastName, lastLabel);
         }
         return Enumerable.Empty<string>();
      }

      private static string EvaluateLabelListFirst(
        string name,
        List<string> labels,
        Dictionary<string, string> variables)
      {
         (var lastName, var lastLabel) = EvaluateLabelListGetLastNameAndLabel(name, labels, variables);
         if (lastName == null)
            return null;
         // Special case! 'someName.text' means evaluate the object that was generated by 'someName.sourceText' and return that.
         if (lastLabel == "text")
         {
            return EvaluateItemText(lastName, variables, false);
         }
         return MapTags.FirstWithNameAndLabel(lastName, lastLabel);
      }

      private static bool EvaluateExpression(
        NotExpression notExpression,
        Dictionary<string, string> variables)
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
         return leftValue == rightValue != notExpression.Not;
      }

      private static bool TryRecursively(
        int index,
        List<NotExpression> notExpressions,
        Dictionary<string, string> variables)
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
               foreach ((var candidateName, var value) in MapTags.AllWithLabel("isNode"))
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

      public static void LoadSource()
      {
         // A chance to do some unit testing on the compiler.
         var sequenceObject = CompileSourceText("[Door.text]");
         //"[tag isOtherSide]");
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
               RenameBaseTags(fileBaseTags);
               fileNewTags = PreprocessMap(fileBaseTags);
               MapTags.Merge(fileBaseTags);
               MapTags.Merge(fileNewTags);
            }
            else
            {
               PreprocessStory(fileBaseTags);
               StoryTags.Merge(fileBaseTags);
            }
         }

         MapTags.Merge(GetStageTagsForNodes());
      }
      /* Let's imagine there is no player, but there is this:
            - A hungry guy sitting next to a counter on which there is a plate of food.
            - A guy behind the counter.
         There are also two stories:
            #1 When a guy is next to a plate of food and he's hungry, he should eat the food and make the plate empty.
            #2 When a guy is behind a counter and there's an empty plate on it, he should pick it up and put it in the dishwasher.
         The program should:
            - Scan all the stories.
            - It should find that the first story is satisfied, so it should make the plate empty. 
            - It should then scan all the stories again.
            - This time it finds that the second story is satisfied, so it should have the counter man pick up the plate and clean it.
         This should happen over and over. That's the whole program. 
         There some questions, though:
            - How does the player see these things happen?
            - How does this keep from running on and on quickly, without any breaks for the player to do things?
            - How does player reaction selection relate to this?
         Seems like after every scan, the program should present the new situation, ex.:
            - Scan all the stories.
            - It should find that the first story is satisfied, so it should make the plate empty. 
          >>- It should stop and show the text for the "man eats food" node.
            - It should then scan all the stories again.
            - This time it finds that the second story is satisfied, so it should have the counter man pick up the plate and clean it.
          >>- It should stop and show the text for the "counter man picks up plate and cleans it" node.
         Seems like there should be a player "do nothing" option that allows you to let a cycle of these things happen without doing anything.
         That answers the first two questions.
         Let's imagine that the player is the hungry man. Let's say he walks away.
         That makes the 'move' story run and then it stops and shows you where you are now. That fits fine in our loop scheme.
         Let's imagine that the player is the hungry man. He can eat the food if he chooses. Or maybe he's on a diet. It's up to him.
         In any case, he has one option: "Eat the food". It's implicit that he can walk away via the stage UI.
         This is the same story as #1 above, except that there's a player option attached.
         Maybe it's like this:
            - Scan all the stories.
            - It should find that the first story COULD BE satisfied IF THE PLAYER CHOSE THE OPTION.
            - Instead of showing the "man eats food" text, it stops and shows the option and its parent node.
            - Let's say you don't pick it. You pick "do nothing".
            - It should then scan all the stories again.
            - It again finds that the first story could be satisfied if you choose.
            - It stops and shows the option again.
            - Finally you pick the option.
            - Now the first story is satisfied and it makes the plate empty.
            - It stops and shows the text for "man eats food".
         So the loop is like this:
            Scan all the stories:
               If the story has no user option and the 'when' can be satisfied:
                  Execute the target node and move to the target node.
               Else if it has a user option and the 'when' can be satisfied:
                  Show the current node.
                  Show the reactions.
            Show all the target nodes and wait for the user to select something.
            If there are reactions and the user picks one:
               Execute the target node and move to the target node.
            Go back to the start.
         So we've got a subroutine that does this:
            For every continuation point:
               if it has no user option and the 'when' can be satisfied:
                  Execute the target node and move to the target node.
               Else if it has a user option and the 'when' can be satisfied:
                  Add it to a list of nodes to display and their reactions.
            Return the list of nodes to display and their reactions.
         We call that from the UI loop. It then displays the nodes and reactions.
         If the UI detects the choice of a reaction it calls this subroutine, which is also called from the one above:
            Execute the target node and move to the target node.
         We don't keep reactions in the continuations. There are separate "stuff to display" objects for that.
         */

      /* We want to have "continuation points". This is a point where a story can be continued.
         To start out, we make a continuation point for every starting node of every story.
         Whenever we move to a different story node, we destroy the old continuation point and add a new one for the different node (unless it's a starting node, in which case we keep the old continuation point).
         Whenever we set up the screen, we scan all the continuation points and present all the ones which can be cast.
         A continuation point can be cast when at least one of its arrows can be cast.

         The player has picked a reaction from a continuation.
         The when directive isn't important any more. We already looked at that when we built the continuation to decide whether the reaction should go into the continuation. He's already picked it; we can't back out now.
         Seems like the picked reaction plus any unconditional reactions all have to be "executed" at this point.
         This occurs for all the pending continuations, not just the one the player is looking at. This allows characters far away from the hero to do things when he isn't looking.
         So essentially, we are giving everybody a "turn" at this point.
         This should occur not just when you pick a reaction. It also has to happen when you select a location.
         The hero has multiple options to pick from, but other characters must have only one option.
         The act of picking a reaction turns the hero into an ordinary character--there is now one option where before there were many.
         So the main loop is like this:

            0. Initialization.

               Before we go into the loop, we have to prime the pump.
               Add all the starting continuations to the list.
               Our next step will be to shift some of them to their target nodes.
               This gets the starting continuations off of the start nodes, which have no descriptions and can't be displayed.

            1. Shift to new continuations.

               This means to move the active continuations to new positions based on their when expressions.
               Go over the list of active continuations we made in initialization or on the previous iteration. If there was a previous iteration, there is now a different state:
                  - The tags were executed.
                  - The player has selected either a stage item or a reaction list item.
               Based on the new state, if any of the continuation's arrows evaluate:
                  Create a new continuation the evaluated arrow.
                  Destroy old continuation (unless it's a starting continuation).
               We now have a new set of continuations.
               Find all the currently active continuations, that is, ones that are or can be cast.
                  This includes generating the current reactions for them.
               While creating the description, execute all the tags in the description. This starts changing the state for the next round. This lets "everybody take a turn".

            2. Show how it is now.

               Display the description and the reactions to choose from.
                  Some of these will have a description, but no reactions, for example, for actions that other characters take.
               Also display the stage.

            3. Let the player change one of the selection states.

               Wait for the player to choose from either the stage or the reaction list.
               Set isSelected on the stage item (if chosen).
               Set isSelected on the player reaction (if chosen):
                  Every reaction that has a player choice essentially has an additional, invisible expression:
                     when ..., "this arrow was selected by the player"
                  So this happens normally as part of the when evaluation process.
                  So arrows like this become "blocked" until the player selects an option.
                  If the arrow could be executed, except for the blockage, you add the option to the reaction list for the player.
                  Though blocked continuations are different. Even though their condition fails, they are still active.
               Go back to step 1.
      */

      public static void ShiftContinuations()
      {
         // ShiftContinuations means to move the active continuations to new positions based on their when expressions.
         // Go over the list of active continuations we made in initialization or on the previous iteration. If there was a previous iteration, there is now a different state.
         var removedContinuations = new List<Continuation>();
         var addedContinuations = new List<Continuation>();
         foreach (var continuation in Continuations)
         {
            // At this point, there are four cases:
            if (!StoryTags.AllWithNameAndLabel(continuation.NodeName, "arrow").Any())
            {
               // a. This continuation's node has no arrows. In that case, we've reached the end of the story pass, so destroy this continuation.
               removedContinuations.Add(continuation);
               continue;
            }
            Reaction reaction;
            if (continuation.Reactions.Count() == 1 && !continuation.Reactions[0].isPlayerOption)
            {
               // b. This continuation's node has one arrow which has no player option. This is for non-player actions. In that case, automatically move to the node the arrow points to. The when expressions were already evaluated when we added this reaction to the list.
               reaction = continuation.Reactions[0];
            }
            else
            {
               reaction = continuation.Reactions.Where((aReaction) => aReaction.isSelected).FirstOrDefault();
               if (reaction == null)
               {
                  // c. This continuation's node has one or more player option arrows, but none has been marked selected by the player. In that case, ignore it.
                  continue;
               }
               // d. This continuation's node has one or more player option arrows, and one of them was marked selected by the player. In that case, move to the node the arrow points to. The expressions were already evaluated earlier when we decided whether we should allow the player to choose it.
            }
            // Move to the reaction target node.
            var newContinuation = new Continuation();
            newContinuation.IsStart = false;
            newContinuation.NodeName = StoryTags.FirstWithNameAndLabel(reaction.ArrowName, "target");
            // It executes in the same context as the old continuation.
            newContinuation.Variables = continuation.Variables;
            newContinuation.Reactions = BuildReactionsFor(newContinuation.NodeName, StoryTags);
            if (!continuation.IsStart)
            {
               removedContinuations.Add(continuation);
            }
            addedContinuations.Add(newContinuation);
         }
         Continuations.RemoveAll((continuation) => removedContinuations.Contains(continuation));
         Continuations.AddRange(addedContinuations);
      }

      public static void RebuildContinuations()
      {
         foreach (var continuation in Continuations)
         {
            continuation.Reactions = BuildReactionsFor(continuation.NodeName, StoryTags);
         }
      }

      public static IEnumerable<string> TagsFor(
           string name,
           string label)
      {
         return MapTags.AllWithNameAndLabel(name, label);
      }

      public static string GetTag(
         string name,
         string label)
      {
         return MapTags.FirstWithNameAndLabel(name, label);
      }

      public static void SetTag(
         string itemName,
         string itemLabel,
         string itemValue)
      {
         MapTags.Remove(itemName, itemLabel);
         MapTags.Add(itemName, itemLabel, itemValue);
      }

      public static string EvaluateItemText(
        string itemName,
        Dictionary<string, string> variables,
        bool executeTags)
      {
         // The item is a node or arrow.
         if (variables == null)
         {
            variables = new Dictionary<string, string>();
         }
         string accumulator = "";
         SequenceObjects[itemName].Traverse((@object) =>
         {
            switch (@object)
            {
               case TextObject textObject:
                  accumulator += textObject.Text;
                  break;
               case SubstitutionObject substitutionObject:
                  accumulator += EvaluateLabelListFirst(substitutionObject.Expression.LeftName, substitutionObject.Expression.LeftLabels, variables);
                  break;
               case IfObject ifObject:
                  return TryRecursively(0, ifObject.NotExpressions, variables);
               case TagObject tagObject:
                  if (!executeTags)
                     break;
                  if (tagObject.Untag)
                  {
                     (var leftName, var leftLabel) = EvaluateLabelListGetLastNameAndLabel(tagObject.Expression.LeftName, tagObject.Expression.LeftLabels, variables);
                     if (leftName == null)
                        break;
                     (var rightName, var rightLabel) = EvaluateLabelListGetLastNameAndLabel(tagObject.Expression.RightName, tagObject.Expression.RightLabels, variables);
                     if (rightName == null)
                     {
                        MapTags.Remove(leftName, leftLabel);
                        break;
                     }
                     var rightValue = MapTags.FirstWithNameAndLabel(rightName, rightLabel);
                     MapTags.Remove(leftName, leftLabel, rightValue);
                  }
                  else
                  {
                     (var leftName, var leftLabel) = EvaluateLabelListGetLastNameAndLabel(tagObject.Expression.LeftName, tagObject.Expression.LeftLabels, variables);
                     if (leftName == null)
                        break;
                     (var rightName, var rightLabel) = EvaluateLabelListGetLastNameAndLabel(tagObject.Expression.RightName, tagObject.Expression.RightLabels, variables);
                     if (rightName == null)
                     {
                        MapTags.Add(leftName, leftLabel, "");
                        break;
                     }
                     var rightValue = MapTags.FirstWithNameAndLabel(rightName, rightLabel);
                     MapTags.Add(leftName, leftLabel, rightValue);
                  }
                  break;
               case SpecialObject specialObject:
                  if (specialObject.Id == "p")
                  {
                     accumulator += "\r\n";
                  }
                  else if (specialObject.Id == "First")
                  {
                     accumulator += MapTags.FirstWithNameAndLabel("hero", "first");
                  }
                  else if (specialObject.Id == "Last")
                  {
                     accumulator += MapTags.FirstWithNameAndLabel("hero", "last");
                  }
                  else
                  {
                     bool heroIsMale = MapTags.FirstWithNameAndLabel("hero", "isMale") != null;
                     if (specialObject.Id == "he")
                     {
                        accumulator += heroIsMale ? "he" : "she";
                     }
                     else if (specialObject.Id == "He")
                     {
                        accumulator += heroIsMale ? "He" : "She";
                     }
                     else if (specialObject.Id == "him")
                     {
                        accumulator += heroIsMale ? "him" : "her";
                     }
                     else if (specialObject.Id == "Him")
                     {
                        accumulator += heroIsMale ? "Him" : "Her";
                     }
                     else if (specialObject.Id == "his")
                     {
                        accumulator += heroIsMale ? "his" : "her";
                     }
                     else if (specialObject.Id == "His")
                     {
                        accumulator += heroIsMale ? "His" : "Her";
                     }
                     else if (specialObject.Id == "himself")
                     {
                        accumulator += heroIsMale ? "himself" : "herself";
                     }
                     else if (specialObject.Id == "Himself")
                     {
                        accumulator += heroIsMale ? "Himself" : "Herself";
                     }
                     else if (specialObject.Id == "man")
                     {
                        accumulator += heroIsMale ? "man" : "woman";
                     }
                     else if (specialObject.Id == "Man")
                     {
                        accumulator += heroIsMale ? "Man" : "Woman";
                     }
                     else if (specialObject.Id == "boy")
                     {
                        accumulator += heroIsMale ? "boy" : "girl";
                     }
                     else if (specialObject.Id == "Boy")
                     {
                        accumulator += heroIsMale ? "Boy" : "Girl";
                     }
                     else if (specialObject.Id == "Mr")
                     {
                        accumulator += heroIsMale ? "Mr." : "Ms";
                     }
                  }
                  break;
            }
            return true;
         });
         return accumulator;
      }
   }
}