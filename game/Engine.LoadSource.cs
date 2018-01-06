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
            return Transform.TokensToObjects(tokens);
         }

         void AddToSequenceObjects(
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

         void RenameBaseTags(
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

         Tags TagItems(
            Tags fileBaseTags)
         {
            // Execute the object text for maps.
            var fileNewTags = new Tags();
            foreach ((var itemName, var itemValue) in fileBaseTags.AllWithLabel("sourceText"))
            {
               var sequenceObject = SequenceObjects[itemName];

               // Execute all the tag directives. This is more limited than the full tagging done in story nodes.
               sequenceObject.Traverse((@object) =>
               {
                  if (!(@object is TagObject tagObject))
                     return true;
               // [untag label] is just a comment in a map file. There's nothing in the tags to start with, so everything is untagged.
               if (tagObject.Untag)
                     return true;
               // Only do tags with no explicit name, i.e. ones that tag the object itself.
               if (tagObject.Expression.LeftName != "")
                     return true;
                  if (tagObject.Expression.LeftLabels.Count != 1)
                  {
                     Log.Fail("expected only one label in a item tag specification");
                  }
                  if (tagObject.Expression.RightLabels.Count > 0)
                  {
                     fileNewTags.Add(itemName, tagObject.Expression.LeftLabels[0], tagObject.Expression.RightLabels[0]);
                  }
                  else
                  {
                     fileNewTags.Add(itemName, tagObject.Expression.LeftLabels[0], "");
                  }
                  return true;
               });

               // Make all the implicit tag names (ex. [if isLarge]) explicit (ex. [if map_test_n1.isLarge]).
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
               Continuations.Add(continuation);
            }
         }

         Tags GetStageTagsForNodes()
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

         // A chance to do some unit testing on the compiler.
         var testSequenceObject = CompileSourceText("[Door.text]");
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
            AddToSequenceObjects(fileBaseTags);

            Tags fileNewTags;
            if (isMap)
            {
               RenameBaseTags(fileBaseTags);
               fileNewTags = TagItems(fileBaseTags);
               MapTags.Merge(fileBaseTags);
               MapTags.Merge(fileNewTags);
            }
            else
            {
               fileNewTags = TagItems(fileBaseTags);
               CreateStartingContinuations(fileNewTags);
               StoryTags.Merge(fileBaseTags);
               StoryTags.Merge(fileNewTags);
            }
         }

         MapTags.Merge(GetStageTagsForNodes());
      }
   }
}
