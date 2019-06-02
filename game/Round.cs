using System;
using System.Collections.Generic;
using System.IO;

namespace Gamebook
{
   //  All of the graphml source code gets converted into this data structure.

   public class Arrow
   {
      public Round TargetRound { get; protected set; }
      public Code Code { get; protected set;  }

      protected Arrow() { }
      protected Arrow(
         Round targetRound,
         Code code)
      {
         TargetRound = targetRound;
         Code = code;
      }
   }

   public class MergeArrow: Arrow
   {
      public string DebugSceneId { get; private set; }
      public string DebugSourceName { get; private set; }

      private MergeArrow() { }
      // This lets the Load function make arrows. 
      public MergeArrow(
         Round targetRound,
         Code code,
         string debugSceneId,
         string debugSourceName): base (targetRound, code)
      {
         DebugSceneId = debugSceneId;
         DebugSourceName = debugSourceName;
      }
      // This lets the Load function add the target scene in a second pass after construction.
      public Round TargetSceneRound { get; set; }
   }

   public class ReactionArrow: Arrow
   {
      private ReactionArrow() { }
      // This lets the Load function make arrows. 
      public ReactionArrow(
         Round targetRound,
         Code code): base (targetRound, code)
      {
      }
   }

   public class Round
   {
      public Code ActionCode { get; private set; }
   
      private List<Arrow> Arrows = new List<Arrow>();
      public IEnumerable<Arrow> GetArrows ()
      {
         return Arrows;
      }

      // The only way to make a Round is through the Load function, which creates all of them.
      private Round() { }

      public static Round Load()
      {
         // Get the source directory.
         var arguments = Environment.GetCommandLineArgs();
         if (arguments.Length < 2)
            Log.Fail("usage: gamebook.exe source-directory");

         // Load all the graphml files in the source directory.
         var sourcePaths = Directory.GetFiles(arguments[1], "*.graphml");
         if (sourcePaths.Length < 1)
            Log.Fail(String.Format($"no .graphml files in directory {arguments[1]}"));

         Round startRound = null;

         // Create a temporary list of actions from the nodes in the graphml that have scene IDs, so we can link merges to them in this routine later.
         var roundsBySceneId = new Dictionary<string, Round>();
         // Create a temporary list of merges that need to be linked to actions by scene ID.
         var mergeFixups = new List<MergeArrow>();

         foreach (var sourcePath in sourcePaths)
         {
            var sourceName = Path.GetFileName(sourcePath);

            Log.SetSourceName(sourceName);

            // Create a temporary list of actions from the nodes in the graphml, so we can link arrows to them in this routine later.
            var roundsByNodeId = new Dictionary<string, Round>();

            var graphml = new Graphml(File.ReadAllText(sourcePath));
            foreach (var (nodeId, label) in graphml.Nodes())
            {
               Round round = new Round();
               round.ActionCode = Code.Compile(label);
               roundsByNodeId.Add(nodeId, round);

               // Check if there's a [scene ID] declaration.
               var declaredSceneId = EvaluateScene(round.ActionCode);
               if (declaredSceneId != null)
               {
                  roundsBySceneId.Add(declaredSceneId, round);
                  if (declaredSceneId == "start")
                  {
                     if (startRound != null)
                        Log.Fail("More than one start scene.");
                     startRound = round;
                  }
               }
            }

            // Create and attach merges and reactions to the actions we just created.
            foreach (var (sourceNodeId, targetNodeId, label) in graphml.Edges())
            {
               // Point the arrow to its target action.
               if (!roundsByNodeId.TryGetValue(targetNodeId, out var targetRound))
                  Log.Fail($"Internal error: no node declaration for referenced target node '{targetNodeId}'");

               Code code = Code.Compile(label);
               var (isMerge, referencedSceneId) = EvaluateMerge(code);
               Arrow arrow;
               if (isMerge)
               {
                  arrow = new MergeArrow(targetRound, code, referencedSceneId, sourceName);
                  if (referencedSceneId != null)
                     mergeFixups.Add(arrow as MergeArrow);
               }
               else
                  arrow = new ReactionArrow(targetRound, code);

               // Add the arrow to the source action's arrows.
               if (!roundsByNodeId.TryGetValue(sourceNodeId, out var sourceRound))
                  Log.Fail($"Internal error: no node declaration for referenced source node '{sourceNodeId}'");
               sourceRound.Arrows.Add(arrow);
            }
         }

         Log.SetSourceCode(null);

         // Make a second pass to point merges that reference scenes to the scene's first action.
         foreach (var mergeArrow in mergeFixups)
         {
            Log.SetSourceName(mergeArrow.DebugSourceName);
            if (!roundsBySceneId.TryGetValue(mergeArrow.DebugSceneId, out var targetSceneRound))
               Log.Fail($"No scene declaration for referenced scene ID '{mergeArrow.DebugSceneId}'");
            mergeArrow.TargetSceneRound = targetSceneRound;
         }

         Log.SetSourceName(null);

         if (startRound == null)
            Log.Fail("No start scene found.");

         return startRound;

         // Some helper functions.

         string EvaluateScene(
            Code topCode)
         {
            string result = null;
            topCode.Traverse((code) =>
            {
               switch (code)
               {
                  case SceneCode sceneCode:
                     result = sceneCode.SceneId;
                     return true;
               }
               return false;
            });
            return result;
         }

         (bool, string) EvaluateMerge(
            Code topCode)
         {
            bool isMerge = false;
            string referencedSceneId = null;
            topCode.Traverse((code) =>
            {
               switch (code)
               {
                  case MergeCode mergeCode:
                     isMerge = true;
                     referencedSceneId = mergeCode.SceneId;
                     return true;
               }
               return false;
            });
            return (isMerge, referencedSceneId);
         }
      }
   }
}
