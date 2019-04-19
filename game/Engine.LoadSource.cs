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
            // Compile all the sourceText tags we got from the graphml file into text tags.
            // Make a new set of tags so we don't modify the fileBaseTags while we iterate over them.
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
            // Now merge the new tags in.
            fileBaseTags.Merge(newTextTags);
         }

         void MergeNodes(
            Tags fileBaseTags)
         {
            Tags removedTags = new Tags();
            Tags addedTags = new Tags();

            // TO DO: repeat the following until there are no more merges to be done.

            // Search all nodes.
            foreach ((var nodeName, var _) in fileBaseTags.AllWithLabel("isNode"))
            {
               // Search all arrows coming from the node
               foreach (var arrowName in fileBaseTags.AllWithNameAndLabel(nodeName, "arrow"))
               {
                  var objectText = fileBaseTags.FirstWithNameAndLabel(arrowName as string, "text");
                  // If the arrow text contains [merge]
                  if (EvaluateMerge(objectText))
                  {
                     // Get the node the arrow points to.
                     var targetName = fileBaseTags.FirstWithNameAndLabel(arrowName as string, "target");
                     if (targetName != null)
                     {
                        // Concatenate the second node text to the first node text.
                        var targetText = fileBaseTags.FirstWithNameAndLabel(targetName as string, "text");
                        var oldText = fileBaseTags.FirstWithNameAndLabel(nodeName, "text");
                        removedTags.Add(nodeName, "sourceText", oldText);
                        addedTags.Add(nodeName, "sourceText", (oldText as SequenceObject).Append(targetText as SequenceObject));

                        // Disconnect the first node from the arrow. We don't need it anymore. We have merged it's text into the node itself.
                        removedTags.Add(nodeName, "arrow", arrowName);

                        // Attach all the target node's arrows to the node.
                        foreach (var targetArrowName in fileBaseTags.AllWithNameAndLabel(targetName as string, "arrow"))
                        {
                           addedTags.Add(nodeName, "arrow", targetArrowName);
                        }
                     }
                  }
               }
            }
            fileBaseTags.Unmerge(removedTags);
            fileBaseTags.Merge(addedTags);
         }

         // START HERE

         // Get the source directory.
         var arguments = Environment.GetCommandLineArgs();
         if (arguments.Length < 2)
         {
            Log.Fail("usage: game.exe source-directory");
         }

         // Load the start file that has all the intial tag settings.
         var startFile = Path.Combine(arguments[1], "start.txt");
         Log.SetSourceName(startFile);
         string startText = File.ReadAllText(startFile);
         if (startText == null)
         {
            Log.Fail(String.Format("no {0} file", startFile));
         }
         var text = CompileSourceText(startText);
         EvaluateTags(text, new Dictionary<string, object>());

         // Load all the graphml files in the source directory.
         var sourcePaths = Directory.GetFiles(arguments[1], "*.graphml");
         if (sourcePaths.Length < 1)
         {
            Log.Fail(String.Format("no .graphml files in directory {0}", arguments[1]));
         }

         foreach (var sourcePath in sourcePaths)
         {
            var sourceName = Path.GetFileName(sourcePath);

            Log.SetSourceName(sourceName);

            string graphml = File.ReadAllText(sourcePath);

            // Translate the graphml boxes and arrows to tags. The file name is just used to create unique tags.
            var fileBaseTags = Transform.GraphmlToTags(graphml, Path.GetFileNameWithoutExtension(sourceName));

            // Compile the directives embedded in the source text of each box and arrow to create a list of object 'text' tags and a SequenceObjects table that relates them to the actual object code.
            AddTextsForSourceTexts(fileBaseTags);

            // If an arrow has [merge], that means concatenate its target node to its source node and disconnect the source node from the arrow.
            MergeNodes(fileBaseTags);

            // Get the root story nodes. They have no arrows pointing at them.
            RootNodeNames = (from nodeName in fileBaseTags.AllWithLabelAndValue("isNode", "")
                             where !fileBaseTags.AllWithLabelAndValue("target", nodeName).Any()
                             select nodeName).ToList<string>();

            Current.Tags.Merge(fileBaseTags);
         }
         Log.SetSourceName(null);
      }
   }
}
