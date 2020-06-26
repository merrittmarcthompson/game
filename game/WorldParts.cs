#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamebook
{
   //  All of the graphml source code gets converted into this data structure.

   public class WithId
   {
      protected static string BuildUniqueId(
         string sourceName,
         string sourceId)
      {
         if (sourceName == null) throw new ArgumentNullException(nameof(sourceName));
         return sourceName.Replace(' ', '-') + ":" + sourceId;
      }
   }

   public class Arrow: WithId
   {
      public Node TargetNode { get; protected set; }
      public CodeTree Code { get; protected set;  }

      protected Arrow(
         Node targetNode,
         CodeTree code)
      {
         TargetNode = targetNode;
         Code = code;
      }
   }

   public class MergeArrow: Arrow
   {
      public string? DebugSceneId { get; }
      public string DebugSourceName { get; }

      // This lets the Load function make arrows. 
      public MergeArrow(
         Node targetNode,
         CodeTree code,
         string? debugSceneId,
         string debugSourceName): base (targetNode, code)
      {
         DebugSceneId = debugSceneId;
         DebugSourceName = debugSourceName;
      }
      // This lets the Load function add the target scene in a second pass after construction.
      public Node? TargetSceneNode { get; set; }
   }

   public class ReturnArrow: Arrow
   {
      // This lets the Load function make arrows. 
      public ReturnArrow(
         Node targetNode,
         CodeTree code) : base(targetNode, code)
      {
      }
   }

   public class ReactionArrow: Arrow
   {
      private string SourceName;
      private string SourceId;

      public string UniqueId
      {
         get => BuildUniqueId(SourceName, SourceId);
      }

      // This lets the Load function make arrows. 
      public ReactionArrow(
         Node targetNode,
         CodeTree code,
         string sourceName,
         string sourceId): base (targetNode, code)
      {
         SourceName = sourceName;
         SourceId = sourceId;
      }
   }

   public class Node: WithId
   {
      // When we save the game state, we don't save the nodes, reactions, code, etc. That is already coming from the .graphml files. Instead, when there is a reference to a unit, we just save the file name and internal ID of the unit. We hook up to the actual units after deserialization based on the file and ID.
      private string SourceName;
      private string SourceId;
      public string UniqueId
      {
         get => BuildUniqueId(SourceName, SourceId);
      }

      // Each Node has two parts:
      // a. The text that describes the opposing turn (the "action"), ex. "@Black Bart said, "I'm gonna burn this town to the ground!"
      // b. The list of texts that describes the options for your turn, ex. "Try to reason with him.", "Shoot him.", etc.
      // Is it really that simple? No.
      public CodeTree ActionCode { get; }
   
      public List<Arrow> Arrows { get; }

      public Node(
         string sourceName,
         string sourceId,
         CodeTree actionCode)
      {
         SourceName = sourceName;
         SourceId = sourceId;
         ActionCode = actionCode;
         Arrows = new List<Arrow>();
      }
   }
}
