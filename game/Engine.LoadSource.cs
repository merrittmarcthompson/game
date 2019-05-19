﻿using System;
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

         // START HERE

         // Get the source directory.
         var arguments = Environment.GetCommandLineArgs();
         if (arguments.Length < 2)
         {
            Log.Fail("usage: gamebook.exe source-directory");
         }

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

            Tags.Merge(fileBaseTags);
         }
         // Get the root story nodes and the merge nodes.
         Current.ActionId = null;
         foreach (var nodeName in Tags.AllWithLabelAndValue("isNode", ""))
         {
            if (!Tags.AllWithLabelAndValue("target", nodeName).Any())
            {
               // Nothing points to it. This is a root node where a scene starts.
               var objectText = Tags.FirstWithNameAndLabel(nodeName, "text");
               string sceneId = EvaluateScene(objectText);
               if (sceneId == null)
               {
                  Log.Fail("Root action of scene without name");
               }
               RootSceneActionIds.Add(sceneId, nodeName);
               if (sceneId == "start")
               {
                  Current.ActionId = nodeName;
               }
            }
         }
         Log.SetSourceName(null);
         if (Current.ActionId == null)
         {
            Log.Fail("No start scene found.");
         }
      }
   }
}
