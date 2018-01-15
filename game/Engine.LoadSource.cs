using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Game
{
   public static partial class Engine
   {
      public static void LoadSource()
      {
         SequenceObject CompileSourceText(
           string sourceText)
         {
            // Compile the text to an object sequence.
            var tokens = Transform.SourceTextToTokens(sourceText);
            if (tokens == null)
               return null;
            return Transform.TokensToObjects(tokens, sourceText);
         }

         void AddTextsForSourceTexts(
           Tags fileBaseTags)
         {
            var newTextTags = new Tags();
            foreach ((var itemName, var itemValue) in fileBaseTags.AllWithLabel("sourceText"))
            {
               Log.SetSourceText(itemValue as string);
               var sequenceObject = CompileSourceText(itemValue as string);
               Log.SetSourceText(null);
               if (sequenceObject == null)
                  continue;
               newTextTags.Add(itemName, "text", sequenceObject);
            }
            fileBaseTags.Merge(newTextTags);
         }

         void RenameBaseTags(
            Tags fileBaseTags)
         {
            // First make a list of all the tags that have to be renamed.
            var oldToNewList = new Dictionary<string, string>();
            foreach ((var oldItemName, var text) in fileBaseTags.AllWithLabel("text"))
            {
               (text as SequenceObject).Traverse((@object) =>
               {
                  if (!(@object is NameObject nameObject))
                     return true;
                  oldToNewList[oldItemName] = nameObject.Name;
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
               if (oldToNewList.ContainsKey(itemValue as string))
               {
                  tagsToRevalue.Add(itemName, "arrow", itemValue);
               }
            }
            foreach ((var itemName, var itemValue) in fileBaseTags.AllWithLabel("target"))
            {
               if (oldToNewList.ContainsKey(itemValue as string))
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
               fileBaseTags.Add(oldName, oldLabel, oldToNewList[oldValue as string]);
            }
         }

         void AddExplicitMapTagObjectNames(
            Tags fileBaseTags)
         {
            // Make all the implicit tag names (ex. [if isLarge] or [parent.onText]) explicit (ex. [if map_test_n1.isLarge] or [map_test_e2.parent.onText]).
            /* There are two situations:
               onText => .onText => explicitName.onText
               parent.onText => parent.onText => explicitName.parent.onText
            */
            foreach ((var itemName, var itemValue) in fileBaseTags.AllWithLabel("text"))
            {
               (itemValue as SequenceObject).Traverse((@object) =>
               {
                  switch (@object)
                  {
                     case SubstitutionObject substitutionObject:
                        if (substitutionObject.Expression.LeftName != "")
                        {
                           substitutionObject.Expression.LeftLabels.Insert(0, substitutionObject.Expression.LeftName);
                        }
                        substitutionObject.Expression.LeftName = itemName;
                        break;
                     case TagObject tagObject:
                        if (tagObject.Expression.LeftName != "")
                        {
                           tagObject.Expression.LeftLabels.Insert(0, tagObject.Expression.LeftName);
                        }
                        tagObject.Expression.LeftName = itemName;
                        // We don't want to go into sub-texts of tag objects. They execute in the context of stories and always have explicit tags.
                        /*
                        if (RightText != null)
                        {
                           RightText.Traverse(examine);
                        }
                        */
                        break;
                     case IfObject ifObject:
                        foreach (var notExpression in ifObject.NotExpressions)
                        {
                           if (notExpression.Expression.LeftName != "")
                           {
                              notExpression.Expression.LeftLabels.Insert(0, notExpression.Expression.LeftName);
                           }
                           notExpression.Expression.LeftName = itemName;
                           if (notExpression.Expression.RightName != "")
                           {
                              notExpression.Expression.RightLabels.Insert(0, notExpression.Expression.RightName);
                           }
                           notExpression.Expression.RightName = itemName;
                        }
                        break;
                  }
                  return true;
               });
            }
         }

         Tags TagItems(
            Tags fileBaseTags)
         {
            // Execute the object text.
            var fileNewTags = new Tags();
            foreach ((var itemName, var itemValue) in fileBaseTags.AllWithLabel("text"))
            {
               // Execute the top-level tag directives. This is more limited than the full tagging done in story nodes. We don't do embedded tags, ex. we do tag noKnockDoorResponse but don't tag husband.isAnnoyed:
               /*
                  [tag noKnockDoorResponse as]
                     [husband.first] says, "Ever think of knocking before you come in?"
                     [tag husband.isAnnoyed]
                  [end] */
               (itemValue as SequenceObject).Scan(@object =>
               {
                  if (!(@object is TagObject tagObject))
                     return true;
                  // [untag label] is just a comment in a map file. There's nothing in the tags to start with, so everything is untagged.
                  if (tagObject.IsUntag)
                     return true;
                  if (tagObject.Expression.LeftLabels.Count != 1)
                  {
                     Log.Fail("expected only one label in a item tag specification");
                  }
                  if (tagObject.RightText != null)
                  {
                     fileNewTags.Add(itemName, tagObject.Expression.LeftLabels[0], tagObject.RightText);
                  }
                  else if (tagObject.Expression.RightLabels.Count > 0)
                  {
                     fileNewTags.Add(itemName, tagObject.Expression.LeftLabels[0], tagObject.Expression.RightLabels[0]);
                  }
                  else
                  {
                     fileNewTags.Add(itemName, tagObject.Expression.LeftLabels[0], "");
                  }
                  return true;
               });
            }
            return fileNewTags;
         }

         void CreateStartingContinuations(
           Tags storyFileTags)
         {
            // Find all the story nodes where stories can start for this source file. Make a continuation for each one. That means that the player can "continue" from the beginning of that story. Later, as they play through a story, we will add more continuation nodes to represent their position in the story.
            foreach ((var nodeName, _) in storyFileTags.AllWithLabel("start"))
            {
               // Add continuations for nodes tagged with start.
               var continuation = new Continuation();
               continuation.NodeName = nodeName;
               continuation.IsStart = true;
               continuation.Name = GenerateContinuationName(nodeName);
               Continuations.Add(continuation);
            }
         }

         void ChangeNamesInText(
            object listText,
            string arrowName,
            string subordinateNode)
         {
            // We've taken text from the arrow and we're going to move it to the arrow's target node. Change any object code that reference the arrow name to reference the target node.
            (listText as SequenceObject).Traverse(@object =>
            {
               switch (@object)
               {
                  case SubstitutionObject substitutionObject:
                     if (substitutionObject.Expression.LeftName == arrowName)
                     {
                        substitutionObject.Expression.LeftName = subordinateNode;
                     }
                     break;
                  case TagObject tagObject:
                     if (tagObject.Expression.LeftName == arrowName)
                     {
                        tagObject.Expression.LeftName = subordinateNode;
                     }
                     break;
                  case IfObject ifObject:
                     foreach (var notExpression in ifObject.NotExpressions)
                     {
                        if (notExpression.Expression.LeftName == arrowName)
                        {
                           notExpression.Expression.LeftName = subordinateNode;
                        }
                        if (notExpression.Expression.RightName == arrowName)
                        {
                           notExpression.Expression.RightName = subordinateNode;
                        }
                     }
                     break;
               }
               return true;
            });
         }

         Tags BuildContainingTagsForNodes()
         {
            Tags newTags = new Tags();
            // Mark items for what stage they are on. Only things that are directly pointed to by the stage are on the stage. If a person is on the stage and there's change in his pocket, the change isn't on the stage. But if he takes the money out of his pocket and drops it, the money is now on the stage (that would be done by the 'drop' story).
            foreach ((var stageName, var nodeValue) in Tags.AllWithLabel("isStage"))
            {
               foreach (var arrowName in Tags.AllWithNameAndLabel(stageName, "arrow"))
               {
                  var subordinateNode = Tags.FirstWithNameAndLabel(arrowName as string, "target");
                  var listText = Tags.FirstWithNameAndLabel(arrowName as string, "text");
                  newTags.Add(subordinateNode as string, "stage", stageName);
                  newTags.Add(subordinateNode as string, "parent", stageName);
                  ChangeNamesInText(listText, arrowName as string, subordinateNode as string);
                  newTags.Add(subordinateNode as string, "listText", listText);
                  if (Tags.FirstWithNameAndLabel(subordinateNode as string, "isCast") != null)
                  {
                     newTags.Add(stageName, "cast", subordinateNode);
                  }
               }
            }
            foreach ((var storageName, var nodeValue) in Tags.AllWithLabel("isStorage"))
            {
               foreach (var arrowName in Tags.AllWithNameAndLabel(storageName, "arrow"))
               {
                  var subordinateNode = Tags.FirstWithNameAndLabel(arrowName as string, "target");
                  var listText = Tags.FirstWithNameAndLabel(arrowName as string, "text");
                  newTags.Add(subordinateNode as string, "storage", storageName);
                  newTags.Add(subordinateNode as string, "parent", storageName);
                  ChangeNamesInText(listText, arrowName as string, subordinateNode as string);
                  newTags.Add(subordinateNode as string, "listText", listText);
               }
            }
            return newTags;
         }

         // A chance to do some unit testing on the compiler.
         var testSequenceObject = CompileSourceText("[tag hero.stage=Stage]");
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
            var fileBaseTags = Transform.GraphmlToTags(graphml, Path.GetFileNameWithoutExtension(sourceName));

            // Compile the directives embedded in the source text of each box and arrow to create a list of object 'text' tags and a SequenceObjects table that relates them to the actual object code.
            AddTextsForSourceTexts(fileBaseTags);

            Tags fileNewTags;
            if (isMap)
            {
               RenameBaseTags(fileBaseTags);
               AddExplicitMapTagObjectNames(fileBaseTags);
               fileNewTags = TagItems(fileBaseTags);
            }
            else
            {
               fileNewTags = TagItems(fileBaseTags);
               CreateStartingContinuations(fileNewTags);
            }
            Tags.Merge(fileBaseTags);
            Tags.Merge(fileNewTags);
         }
         Log.SetSourceName(null);
         Tags.Merge(BuildContainingTagsForNodes());
      }
   }
}
