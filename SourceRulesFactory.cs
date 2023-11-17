using System.Reflection;

static class SourceRulesFactory {

    public static List<ConditionBuilder<Source>> GetRules() {
        
        return new List<ConditionBuilder<Source>>() {

		
			ConditionBuilderHelper
                .Name<Source>("myDataSourceSample never executed")
				.And(p => p.Action is null)
				.And(p => p.Key == "myDataSourceSample" )
				.And(p => p.FirstAttempt is null)
				.Then("BaseRuleHandlers.Execute")
		,

		
			ConditionBuilderHelper
                .Name<Source>("myDataSourceSample is over 30 seconds")
				.And(p => p.Key == "myDataSourceSample" )
				.And(p => p.Status == "Error")
                .And(p => p.FirstAttempt is not null)
				.And(p => (DateTime.Now - p.FirstAttempt) > TimeSpan.FromSeconds(30))
				.Then(BaseRuleHandlers.Dismiss)
		,

        
			ConditionBuilderHelper
                .Name<Source>("myDataSourceSample first execution less than 30 seconds and last execution was less than 5 seconds")
				.And(p => p.Key == "myDataSourceSample" )
				.And(p => p.Status == "Error")
                .And(p => (DateTime.Now - p.FirstAttempt) < TimeSpan.FromSeconds(30))
                .And(p => p.LastAttempt is not null)
                .And(p => (DateTime.Now - p.LastAttempt) < TimeSpan.FromSeconds(5))
				.Then(BaseRuleHandlers.Enqueue)
		,
		
		
			ConditionBuilderHelper
                .Name<Source>("myDataSourceSample is less than 30 seconds but last execution was more than 5 seconds")
				.And(p => p.Key == "myDataSourceSample" )
				.And(p => p.Status == "Error")
                .And(p => (DateTime.Now - p.FirstAttempt) < TimeSpan.FromSeconds(30))
                .And(p => p.LastAttempt is null || (DateTime.Now - p.LastAttempt) > TimeSpan.FromSeconds(5))
                .Then(BaseRuleHandlers.Execute)

        };
    }

}