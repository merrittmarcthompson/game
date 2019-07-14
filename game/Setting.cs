namespace Gamebook
{
   public abstract class Setting
   {
      // Settings have a generic value, which is one of these:
      //    null: False
      //    non-null string: True if you want a boolean, or the value of the string if you want a string.
      public abstract string GenericValue();
      public abstract bool IsBoolean();

      protected static string ConvertTruthValueToGeneric(
        bool truthValue) => truthValue ? "" : null;
   }

   public class BooleanSetting: Setting
   {
      bool Value = false;
      
      public override string GenericValue() => ConvertTruthValueToGeneric(Value);

      public override bool IsBoolean() => true;

      private BooleanSetting() { }
      public BooleanSetting(
         bool value)
      {
         Value = value;
      }
   }

   public class StringSetting: Setting
   {
      string Value = "";

      public override string GenericValue() => Value;

      public override bool IsBoolean() => false;

      private StringSetting() { }
      public StringSetting(
         string value)
      {
         Value = value;
      }
   }

   public class ScoreSetting: Setting
   {
      private int ChosenCount = 0;
      public int GetChosenCount() => ChosenCount;

      private int OpportunityCount = 0;
      public int GetOpportunityCount() => OpportunityCount;

      public void RaiseChosenCount()
      {
         ++ChosenCount;
      }

      public void RaiseOpportunityCount()
      {
         ++OpportunityCount;
      }

      public override string GenericValue() => ConvertTruthValueToGeneric(Value() >= 0.5);

      public override bool IsBoolean() => true;

      public double Value() => OpportunityCount == 0? 0: (double)ChosenCount / OpportunityCount;
   }
}
