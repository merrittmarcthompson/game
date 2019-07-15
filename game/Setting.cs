namespace Gamebook
{
   public abstract class Setting
   {
   }

   public class StringSetting: Setting
   {
      public string Value { get; private set; } = "";

      private StringSetting() { }
      public StringSetting(
         string value)
      {
         Value = value;
      }
   }

   public abstract class AbstractBooleanSetting: Setting
   {
      public abstract bool Value { get; protected set; }
   }

   public class BooleanSetting: AbstractBooleanSetting
   {
      public override bool Value { get; protected set; } = false;

      private BooleanSetting() { }
      public BooleanSetting(
         bool value)
      {
         Value = value;
      }
   }

   public class ScoreSetting: AbstractBooleanSetting
   {
      public override bool Value
      {
         get => ScoreValue >= 0.5;
         protected set { }
      }

      public double ScoreValue
      {
         get => OpportunityCount == 0 ? 0.0 : (double)ChosenCount / OpportunityCount;
         protected set { }
      }

      private int ChosenCount = 0;
      private int OpportunityCount = 0;

      public int GetChosenCount() => ChosenCount;
      public int GetOpportunityCount() => OpportunityCount;

      public void RaiseChosenCount()
      {
         ++ChosenCount;
      }

      public void RaiseOpportunityCount()
      {
         ++OpportunityCount;
      }

   }
}
