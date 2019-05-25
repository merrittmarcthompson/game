using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamebook
{
   public static partial class Transform
   {
      public static Action LoadActions()
      {
         // Get the source directory.
         var arguments = Environment.GetCommandLineArgs();
         if (arguments.Length < 2)
            Log.Fail("usage: gamebook.exe source-directory");

         // Load all the graphml files in the source directory.
         var sourcePaths = Directory.GetFiles(arguments[1], "*.graphml");
         if (sourcePaths.Length < 1)
            Log.Fail(String.Format("no .graphml files in directory {0}", arguments[1]));

         Action startAction = null;

         // Create a temporary list of actions from the nodes in the graphml that have scene IDs, so we can link merges to them in this routine later.
         var actionsBySceneId = new Dictionary<string, Action>();
         // Create a temporary list of merges that need to be linked to actions by scene ID.
         var mergeFixups = new List<MergeArrow>();

         foreach (var sourcePath in sourcePaths)
         {
            var sourceName = Path.GetFileName(sourcePath);

            Log.SetSourceName(sourceName);

            // Create a temporary list of actions from the nodes in the graphml, so we can link arrows to them in this routine later.
            var actionsByNodeId = new Dictionary<string, Action>();

            var graphml = new Graphml(File.ReadAllText(sourcePath));
            foreach (var (nodeId, label) in graphml.Nodes())
            {
               Action action = new Action();
               action.Sequence = CompileSourceCode(label);
               actionsByNodeId.Add(nodeId, action);

               // Check if there's a [scene ID] declaration.
               var declaredSceneId = EvaluateScene(action.Sequence);
               if (declaredSceneId != null)
               {
                  actionsBySceneId.Add(declaredSceneId, action);
                  if (declaredSceneId == "start")
                  {
                     if (startAction != null)
                        Log.Fail("More than one start scene.");
                     startAction = action;
                  }
               }
            }

            // Create and attach merges and reactions to the actions we just created.
            foreach (var (sourceNodeId, targetNodeId, label) in graphml.Edges())
            {
               SequenceOperation sequence = CompileSourceCode(label);
               var (isMerge, referencedSceneId) = EvaluateMerge(sequence);
               Arrow arrow;
               if (isMerge)
                  arrow = new MergeArrow();
               else
                  arrow = new ReactionArrow();
               arrow.Sequence = sequence;

               // Add the arrow to the source action's arrows.
               if (!actionsByNodeId.TryGetValue(sourceNodeId, out var sourceAction))
                  Log.Fail($"Internal error: no node declaration for referenced source node '{sourceNodeId}'");
               sourceAction.Arrows.Add(arrow);

               // Point the arrow to its target action.
               if (!actionsByNodeId.TryGetValue(targetNodeId, out var targetAction))
                  Log.Fail($"Internal error: no node declaration for referenced target node '{targetNodeId}'");
               arrow.TargetAction = targetAction;

               if (arrow is MergeArrow mergeArrow)
               {
                  mergeArrow.DebugSceneId = referencedSceneId;
                  mergeArrow.DebugSourceName = sourceName;
                  if (referencedSceneId != null)
                     mergeFixups.Add(mergeArrow);
               }
            }
         }

         Log.SetSourceCode(null);

         // Make a second pass to point merges that reference scenes to the scene's first action.
         foreach (var mergeArrow in mergeFixups)
         {
            Log.SetSourceName(mergeArrow.DebugSourceName);
            if (!actionsBySceneId.TryGetValue(mergeArrow.DebugSceneId, out var targetSceneAction))
               Log.Fail($"No scene declaration for referenced scene ID '{mergeArrow.DebugSceneId}'");
            mergeArrow.TargetSceneAction = targetSceneAction;
         }

         Log.SetSourceName(null);

         if (startAction == null)
            Log.Fail("No start scene found.");

         return startAction;

         // Some helper functions.

         string EvaluateScene(
            SequenceOperation sequence)
         {
            string result = null;
            sequence.Traverse((operation) =>
            {
               switch (operation)
               {
                  case SceneOperation sceneOperation:
                     result = sceneOperation.SceneId;
                     return true;
               }
               return false;
            });
            return result;
         }

         (bool, string) EvaluateMerge(
            SequenceOperation sequence)
         {
            bool isMerge = false;
            string referencedSceneId = null;
            sequence.Traverse((operation) =>
            {
               switch (operation)
               {
                  case MergeOperation mergeOperation:
                     isMerge = true;
                     referencedSceneId = mergeOperation.SceneId;
                     return true;
               }
               return false;
            });
            return (isMerge, referencedSceneId);
         }

         SequenceOperation CompileSourceCode(
           string sourceCode)
         {
            // Compile the text to an object sequence.
            Log.SetSourceCode(sourceCode);
            var tokens = Transform.SourceTextToTokens(sourceCode);
            if (tokens == null)
               return null;
            return Transform.TokensToOperations(tokens, sourceCode);
         }
      }
   }
}
