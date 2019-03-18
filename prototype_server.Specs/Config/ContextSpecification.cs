using System;
using TestStack.BDDfy;

namespace prototype_server.Specs
{
    public class ContextSpecification : MethodNameStepScanner
    {
        public ContextSpecification() : base(
            CleanupTheStepText,
            new[]
            {
                new MethodNameMatcher(s => s.StartsWith("EstablishContext", StringComparison.OrdinalIgnoreCase), false, ExecutionOrder.SetupState, false),
                new MethodNameMatcher(s => s.StartsWith("BecauseOf", StringComparison.OrdinalIgnoreCase), false, ExecutionOrder.Transition, false),
                new MethodNameMatcher(s => s.StartsWith("It", StringComparison.OrdinalIgnoreCase), true, ExecutionOrder.Assertion, true),
                new MethodNameMatcher(s => s.StartsWith("AndIt", StringComparison.OrdinalIgnoreCase), true, ExecutionOrder.ConsecutiveAssertion, true)
            })
        {}

        static string CleanupTheStepText(string stepText)
        {
            if (stepText.StartsWith("EstablishContext", StringComparison.OrdinalIgnoreCase))
            {
                return "Establish context ";
            }

            if (stepText.StartsWith("BecauseOf", StringComparison.OrdinalIgnoreCase))
            {
                return "Because of ";
            }

            if (stepText.StartsWith("AndIt ", StringComparison.OrdinalIgnoreCase))
            {
                return stepText.Remove("and".Length, "It".Length);
            }

            return stepText;
        }
    }
}